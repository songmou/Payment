using SDK.Payment.Enum;
using SDK.Payment.Model;
using SDK.Payment.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace SDK.Payment.Payment
{
    public enum WxpayTradeEnum
    {
        /// <summary>
        /// 公众号支付 小程序支付
        /// </summary>
        JSAPI = 0,
        /// <summary>
        /// 原生扫码支付
        /// </summary>
        NATIVE,
        /// <summary>
        /// app支付
        /// </summary>
        APP,
        /// <summary>
        /// wap支付（浏览器调用微信app，H5支付）
        /// </summary>
        MWEB,
        /// <summary>
        /// 刷卡支付
        /// </summary>
        MICROPAY
    }

    public class Wxpay
    {
        //private WxpayTradeEnum? TradeType;
        private WxpayConfig WxpayConfig = null;

        public Wxpay(WxpayConfig _WxpayConfig)
        {
            WxpayConfig = _WxpayConfig;
            //TradeType = _TraceEnum;
        }

        #region 统一下单
        public string UnifiedOrder(WxpayTradeEnum? _TraceEnum,object param)
        {
            var inputParam = ReflectionHelper.GetObjectValues(param);
            #region 必填信息验证
            if (!inputParam.ContainsKey("out_trade_no"))
            {
                throw new PayException("缺少订单号");
            }
            if (!inputParam.ContainsKey("total_fee"))
            {
                throw new PayException("缺少订单金额");
            }
            #endregion

            string gateway = "https://api.mch.weixin.qq.com/pay/unifiedorder";
            string resultXml = HTTPHelper.Post(gateway, BuildXmlDocument(CreateParameter(inputParam, _TraceEnum)));

            var dic = FromXmlToList(resultXml);

            string returnCode = dic.TryGetString("return_code");

            if (returnCode == "SUCCESS")
            {
                if (_TraceEnum == WxpayTradeEnum.JSAPI)
                {
                    var jsApiParam = new Dictionary<string, object>();
                    jsApiParam.Add("appId", dic["appid"]);
                    jsApiParam.Add("timeStamp", GenerateTimeStamp());
                    jsApiParam.Add("nonceStr", GetNonceStr());
                    jsApiParam.Add("package", "prepay_id=" + dic["prepay_id"]);
                    jsApiParam.Add("signType", "MD5");
                    string preString = CreateURLParamString(jsApiParam) + "&key=" + WxpayConfig.PAY_KEY;
                    string signValue = HashHelper.MD5(preString, WxpayConfig.CHARTSET).ToUpper();
                    jsApiParam.Add("paySign", signValue);
                    return JsonHelper.ToJson(jsApiParam);
                }
                else if (_TraceEnum == WxpayTradeEnum.NATIVE)
                {
                    string codeUrl = dic.TryGetString("code_url");
                    if (!string.IsNullOrEmpty(codeUrl))
                        return codeUrl;
                    else
                        throw new PayException("未找到对应的二维码链接");
                }
                else if (_TraceEnum == WxpayTradeEnum.MWEB)
                {
                }
                else if (_TraceEnum == WxpayTradeEnum.APP)
                {
                    //var prepay_id = GetValueFromDic<string>(dic, "prepay_id");
                    //if (!string.IsNullOrEmpty(prepay_id))
                    //    return BuildAppPay(prepay_id);
                }
            }

            return GetMessage(dic);
        }
        #endregion

        #region 刷卡支付
        /// <summary>
        /// 提交刷卡支付获取返回支付结果
        /// </summary>
        /// <param name="param"></param>
        /// <param name="waitforPaying">出现付款中状态，是否等待用户付款</param>
        /// <returns></returns>
        public IPayResult GetMicropayResult(object param, bool waitforPaying = true)
        {
            var inputParam = ReflectionHelper.GetObjectValues(param);
            //注意：如果当前交易返回的支付状态是明确的错误原因造成的支付失败（支付确认失败），请重新下单支付；
            //如果当前交易返回的支付状态是不明错误（支付结果未知），请调用查询订单接口确认状态，如果长时间（建议30秒）都得不到明确状态请调用撤销订单接口。
            string gateway = "https://api.mch.weixin.qq.com/pay/micropay";

            if (!inputParam.ContainsKey("auth_code"))
            {
                throw new PayException("授权码 auth_code 不能为空");
            }

            IPayResult result = new PayResult();

            SortedDictionary<string, object> dicParam = CreateParameter(inputParam,WxpayTradeEnum.MICROPAY);
            string resultXml = HTTPHelper.Post(gateway, BuildXmlDocument(dicParam));
            var dic = FromXmlToList(resultXml);

            result.Message = GetMessage(dic);
            //签名校验通过
            if (GetMd5SignString(dic) == dic.TryGetString("sign"))
            {
                result.Parameter = dic;

                //扣款成功
                if (dic.TryGetString("return_code") == "SUCCESS"
                    && dic.TryGetString("result_code") == "SUCCESS")
                {
                    var price = dic.TryGetString("total_fee");
                    decimal TotalFee = 0M; decimal.TryParse(price, out TotalFee);

                    var out_trade_no = dic.TryGetString("out_trade_no");

                    result.OutTradeNo = out_trade_no;
                    result.TradeStatus = dic.TryGetString("return_code");
                    result.Trade_No = dic.TryGetString("transaction_id");
                    result.TotalFee = TotalFee / 100;
                    return result;
                }

                if (dic.TryGetString("err_code") == "USERPAYING")
                {
                    string out_trade_no = dicParam.TryGetString("out_trade_no");

                    if (waitforPaying)
                    {
                        //等待用户输入密码，循环5次查询订单的支付状态
                        IPayResult queryParam = new PayResult();
                        int loop = 1;
                        while (loop < 5 && queryParam.TradeStatus != "SUCCESS")
                        {
                            System.Threading.Thread.Sleep(5 * 1000);
                            loop++;

                            //查询订单
                            queryParam = GetQueryOrder(out_trade_no);
                        };

                        //如果还是未支付成功状态,则撤销订单
                        if (queryParam.TradeStatus != "SUCCESS")
                        {
                            //交易已撤销
                            var ro = ReverseOrder(out_trade_no);
                        }
                        return queryParam;
                    }
                    var r = ReverseOrder(out_trade_no);
                }


            }
            return result;
        }
        #endregion

        #region 撤销订单
        /// <summary>
        /// 撤销订单
        /// 支付交易返回失败或支付系统超时，调用该接口撤销交易。
        /// 如果此订单用户支付失败，微信支付系统会将此订单关闭；如果用户支付成功，微信支付系统会将此订单资金退还给用户。
        /// </summary>
        /// <param name="out_trade_no"></param>
        /// <param name="transaction_id"></param>
        /// <returns></returns>
        public bool ReverseOrder(string out_trade_no)
        {
            string gateway = "https://api.mch.weixin.qq.com/secapi/pay/reverse";
            SortedDictionary<string, object> param = new SortedDictionary<string, object>();

            if (string.IsNullOrWhiteSpace(out_trade_no))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(out_trade_no))
            {
                param.Add("out_trade_no", out_trade_no);
            }

            IPayResult result = new PayResult();

            param.Add("appid", WxpayConfig.APP_ID);
            param.Add("mch_id", WxpayConfig.MCH_ID);

            param.Add("nonce_str", GetNonceStr());
            param.Add("sign_type", "MD5");
            param.Add("sign", GetMd5SignString(param));

            string resultXml = HTTPHelper.Post(gateway, BuildXmlDocument(param), WxpayConfig.SSLCERT_PATH, WxpayConfig.SSLCERT_PASSWORD);
            var dic = FromXmlToList(resultXml);

            //签名校验
            if (GetMd5SignString(dic) == dic.TryGetString("sign"))
            {
                result.Parameter = dic;

                string returnCode = dic.TryGetString("return_code");
                if (dic.TryGetString("return_code") == "SUCCESS" && dic.TryGetString("result_code") == "SUCCESS")
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region 交易查询
        /// <summary>
        /// 查询微信订单 
        /// （商户订单号和微信交易号二选一）
        /// </summary>
        /// <param name="out_trade_no">商户号</param>
        /// <param name="transaction_id">交易号</param>
        /// <returns></returns>
        public IPayResult GetQueryOrder(string out_trade_no = "", string transaction_id = "")
        {
            var param = new SortedDictionary<string, object>();

            if (string.IsNullOrWhiteSpace(out_trade_no) && string.IsNullOrWhiteSpace(transaction_id))
            {
                throw new PayException("交易号和商户订单号至少需要一个");
            }

            if (!string.IsNullOrWhiteSpace(out_trade_no))
            {
                param.Add("out_trade_no", out_trade_no);
            }
            if (!string.IsNullOrWhiteSpace(transaction_id))
            {
                param.Add("transaction_id", transaction_id);
            }

            IPayResult result = new PayResult();
            var queryParam = QueryOrderRecord(param);

            result.Message = GetMessage(queryParam);
            //签名校验
            if (GetMd5SignString(queryParam) == queryParam.TryGetString("sign"))
            {
                result.Parameter = queryParam;

                if (queryParam.TryGetString("return_code") == "SUCCESS"
                    && queryParam.TryGetString("result_code") == "SUCCESS"
                    && queryParam.TryGetString("trade_state") == "SUCCESS")
                {
                    var price = queryParam.TryGetString("total_fee");
                    decimal TotalFee = 0M; decimal.TryParse(price, out TotalFee);

                    result.OutTradeNo = out_trade_no;
                    result.TradeStatus = queryParam.TryGetString("return_code");
                    result.Trade_No = queryParam.TryGetString("transaction_id");
                    result.TotalFee = TotalFee / 100;
                }
            }
            return result;
        }
        /// <summary>
        /// 查询微信支付交易的订单（自行判断返回参数）
        /// </summary>
        /// <param name="out_trade_no"></param>
        /// <returns></returns>
        private IDictionary<string, object> QueryOrderRecord(SortedDictionary<string, object> param)
        {
            string gateway = "https://api.mch.weixin.qq.com/pay/orderquery";
            param.Add("appid", WxpayConfig.APP_ID);
            param.Add("mch_id", WxpayConfig.MCH_ID);

            param.Add("nonce_str", GetNonceStr());
            param.Add("sign_type", "MD5");
            param.Add("sign", GetMd5SignString(param));

            string resultXml = HTTPHelper.Post(gateway, BuildXmlDocument(param));
            return FromXmlToList(resultXml);
        }
        #endregion

        #region 交易退款
        /// <summary>
        /// 退款调用方法
        /// </summary>
        /// <param name="param">参数里需要包含 订单号|退款订单号|总金额|退款金额</param>
        /// <returns></returns>
        public IPayResult RefundOrder(object inputParam)
        {
            var param = ReflectionHelper.GetObjectValues(inputParam);
            #region 必填信息验证
            if (!param.ContainsKey("transaction_id") && !param.ContainsKey("out_trade_no"))
            {
                throw new PayException("缺少订单号");
            }
            if (!param.ContainsKey("out_refund_no"))
            {
                throw new PayException("缺少退款订单号");
            }
            if (!param.ContainsKey("total_fee"))
            {
                throw new PayException("缺少订单总金额");
            }
            if (!param.ContainsKey("refund_fee"))
            {
                throw new PayException("缺少退款总金额");
            }
            #endregion

            string gateway = "https://api.mch.weixin.qq.com/secapi/pay/refund";

            IPayResult result = new PayResult();

            param.Add("appid", WxpayConfig.APP_ID);
            param.Add("mch_id", WxpayConfig.MCH_ID);
            param.Add("nonce_str", GetNonceStr());
            param.Add("sign_type", "MD5");
            param.Add("sign", GetMd5SignString(param));

            string resultXml = HTTPHelper.Post(gateway, BuildXmlDocument(param), WxpayConfig.SSLCERT_PATH, WxpayConfig.SSLCERT_PASSWORD);
            var dic = FromXmlToList(resultXml);

            result.Message = GetMessage(dic);
            //签名校验
            if (dic.TryGetString("sign") == GetMd5SignString(dic))
            {
                result.Parameter = dic;

                if (dic.TryGetString("return_code") == "SUCCESS"
                    && dic.TryGetString("result_code") == "SUCCESS")
                {
                    var out_trade_no = dic.TryGetString("out_trade_no");

                    var price = dic.TryGetString("total_fee");
                    decimal TotalFee = 0M;
                    decimal.TryParse(price, out TotalFee);

                    result.OutTradeNo = out_trade_no;
                    result.TradeStatus = dic.TryGetString("return_code");
                    result.Trade_No = dic.TryGetString("transaction_id");
                    result.TotalFee = TotalFee / 100;
                }
            }
            return result;
        }
        #endregion

        #region Private Method

        /// <summary>
        /// 显示详细的文本信息
        /// </summary>
        /// <param name="dic"></param>
        /// <returns></returns>
        private string GetMessage(IDictionary<string, object> dic)
        {
            var result = "";

            if (dic.ContainsKey("return_code"))
            {
                string return_code = dic.TryGetString("return_code");
                string return_msg = dic.TryGetString("return_msg");
                result = string.Format("{0}:{1}", return_code, return_msg);
            }

            if (dic.ContainsKey("err_code"))
            {
                string err_code = dic.TryGetString("err_code");
                string err_code_des = dic.TryGetString("err_code_des");
                result = string.Format("{0}:{1}", err_code, err_code_des);
            }

            if (dic.ContainsKey("trade_state"))
            {
                string trade_state = dic.TryGetString("trade_state");
                string trade_state_desc = dic.TryGetString("trade_state_desc");
                result = string.Format("{0}:{1}", trade_state, trade_state_desc);
            }
            return result;
        }

        /// <summary>
        /// 获取Md5 签名
        /// </summary>
        /// <param name="dic"></param>
        /// <returns></returns>
        private string GetMd5SignString(IDictionary<string, object> dic)
        {
            string preString = CreateURLParamString(dic) + "&key=" + WxpayConfig.PAY_KEY;
            string signValue = HashHelper.MD5(preString, WxpayConfig.CHARTSET).ToUpper();
            return signValue;
        }


        private SortedDictionary<string, object> CreateParameter(IDictionary<string, object> dic, WxpayTradeEnum? _TraceEnum)
        {
            if (string.IsNullOrWhiteSpace(WxpayConfig.APP_ID) || string.IsNullOrWhiteSpace(WxpayConfig.MCH_ID))
            {
                throw new PayException("缺少必要参数");
            }

            SortedDictionary<string, object> param = new SortedDictionary<string, object>();
            param.Add("appid", WxpayConfig.APP_ID);//账号ID
            param.Add("mch_id", WxpayConfig.MCH_ID);//商户号
            param.Add("nonce_str", GetNonceStr());//随机字符串


            if (_TraceEnum == WxpayTradeEnum.APP
                || _TraceEnum == WxpayTradeEnum.JSAPI
                || _TraceEnum == WxpayTradeEnum.NATIVE
                || _TraceEnum == WxpayTradeEnum.MWEB)
            {
                if (!string.IsNullOrWhiteSpace(WxpayConfig.NOTIFY_URL) && !dic.ContainsKey("notify_url"))
                    param.Add("notify_url", WxpayConfig.NOTIFY_URL);//通知地址

                param.Add("trade_type", _TraceEnum.ToString());//交易类型
            }

            foreach (var d in dic)
            {
                if (!param.ContainsKey(d.Key))
                    param.Add(d.Key, d.Value);
            }

            param.Add("sign_type", "MD5");
            param.Add("sign", GetMd5SignString(param));

            return param;
        }

        /// <summary>
        /// 最终请求的XML组装
        /// </summary>
        /// <param name="dicParam"></param>
        /// <returns></returns>
        private static string BuildXmlDocument(IDictionary<string, object> dicParam)
        {
            StringBuilder sbXML = new StringBuilder();
            sbXML.Append("<xml>");
            foreach (KeyValuePair<string, object> temp in dicParam)
            {
                sbXML.Append("<" + temp.Key + ">" + temp.Value.ToString() + "</" + temp.Key + ">");
            }

            sbXML.Append("</xml>");
            return sbXML.ToString();
        }

        /// <summary>
        /// 组装加密前的请求参数
        /// </summary>
        /// <param name="dicArray"></param>
        /// <returns></returns>
        private static string CreateURLParamString(IDictionary<string, object> dicArray)
        {
            StringBuilder prestr = new StringBuilder();
            foreach (KeyValuePair<string, object> temp in dicArray.OrderBy(o => o.Key))
            {
                if (temp.Key != "sign" && temp.Value != null && !string.IsNullOrWhiteSpace(temp.Value.ToString()))
                    prestr.Append(temp.Key + "=" + temp.Value + "&");
            }

            int nLen = prestr.Length;
            prestr.Remove(nLen - 1, 1);
            return prestr.ToString();
        }

        private static IDictionary<string, object> FromXmlToList(string xml)
        {
            IDictionary<string, object> sortDic = new SortedDictionary<string, object>();
            if (string.IsNullOrEmpty(xml))
            {
                throw new PayException();
            }

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            XmlNode xmlNode = xmlDoc.FirstChild;//获取到根节点<xml>
            XmlNodeList nodes = xmlNode.ChildNodes;
            foreach (XmlNode xn in nodes)
            {
                XmlElement xe = (XmlElement)xn;

                if (!sortDic.ContainsKey(xe.Name))
                    sortDic.Add(xe.Name, xe.InnerText);
            }
            return sortDic;
        }

        /// <summary>
        /// 获取时间戳
        /// </summary>
        /// <returns></returns>
        public static string GenerateTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }
        /// <summary>
        /// 获取随机字符
        /// </summary>
        /// <returns></returns>
        public static string GetNonceStr()
        {
            return Guid.NewGuid().ToString().Replace("-", "");
        }

        #endregion
    }
}
