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
    public class Wxpay
    {
        private WxpayTradeTypeEnum TradeType;
        private string PAY_KEY = "";
        private string APP_ID = "";
        private string MCH_ID = "";
        private string CHARTSET = "";

        private string NOTIFY_URL = "";
        public Wxpay(WxpayTradeTypeEnum Enum, string app_id, string mch_id, string pay_key, string notity_url = "", string charset = "utf-8")
        {
            TradeType = Enum;
            APP_ID = app_id;
            MCH_ID = mch_id;
            PAY_KEY = pay_key;
            NOTIFY_URL = notity_url;
            CHARTSET = charset;
        }

        public Wxpay(string app_id, string mch_id, string pay_key, string charset = "utf-8")
        {
            APP_ID = app_id;
            MCH_ID = mch_id;
            PAY_KEY = pay_key;
            CHARTSET = charset;
        }

        /// <summary>
        /// 统一下单
        /// </summary>
        /// <param name="param">额外的参数</param>
        /// <returns></returns>
        public string UnifiedOrder(Dictionary<string, string> param)
        {
            #region 必填信息验证
            if (!param.ContainsKey("out_trade_no"))
            {
                throw new PayException("缺少订单号");
            }
            if (!param.ContainsKey("total_fee"))
            {
                throw new PayException("缺少订单金额");
            }
            #endregion


            string gateway = "https://api.mch.weixin.qq.com/pay/unifiedorder";

            SortedDictionary<string, string> dicParam = CreateParameter(param);

            string resultXml = HTTPHelper.Post(gateway, BuildXmlDocument(dicParam));

            var dic = FromXmlToList(resultXml);

            string returnCode = "";
            dic.TryGetValue("return_code", out returnCode);

            if (returnCode == "SUCCESS")
            {
                if (TradeType == WxpayTradeTypeEnum.APP)
                {
                    //var prepay_id = GetValueFromDic<string>(dic, "prepay_id");
                    //if (!string.IsNullOrEmpty(prepay_id))
                    //    return BuildAppPay(prepay_id);
                    //else
                    //    throw new Exception("支付错误:" + GetValueFromDic<string>(dic, "err_code_des"));
                }
                else if (TradeType == WxpayTradeTypeEnum.NATIVE)
                {
                    string codeUrl = "";
                    dic.TryGetValue("code_url", out codeUrl);
                    if (!string.IsNullOrEmpty(codeUrl))
                        return codeUrl;
                    else
                        throw new PayException("未找到对应的二维码链接");
                }
                else if (TradeType == WxpayTradeTypeEnum.JSAPI)
                {
                    var jsApiParam = new Dictionary<string, string>();
                    jsApiParam.Add("appId", dic["appid"]);
                    jsApiParam.Add("timeStamp", GenerateTimeStamp());
                    jsApiParam.Add("nonceStr", GetNonceStr());
                    jsApiParam.Add("package", "prepay_id=" + dic["prepay_id"]);
                    jsApiParam.Add("signType", "MD5");
                    string preString = CreateURLParamString(jsApiParam) + "&key=" + PAY_KEY;
                    string signValue = MD5Helper.Sign(preString, CHARTSET).ToUpper();
                    jsApiParam.Add("paySign", signValue);
                    return jsApiParam.ToJson();
                }
                else
                    throw new PayException(" WAP 未实现");
            }
            else
                throw new PayException("后台统一下单失败");

            return "error";
        }

        /// <summary>
        /// 提交刷卡支付获取返回支付结果
        /// 注意：如果当前交易返回的支付状态是明确的错误原因造成的支付失败（支付确认失败），请重新下单支付；
        /// 如果当前交易返回的支付状态是不明错误（支付结果未知），请调用查询订单接口确认状态，如果长时间（建议30秒）都得不到明确状态请调用撤销订单接口。
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public WxpayResult GetMicropayResult(Dictionary<string, string> param)
        {
            string gateway = "https://api.mch.weixin.qq.com/pay/micropay";

            if (!param.ContainsKey("auth_code"))
            {
                throw new PayException("授权码 auth_code 不能为空");
            }

            WxpayResult result = new WxpayResult();

            SortedDictionary<string, string> dicParam = CreateParameter(param);
            string resultXml = HTTPHelper.Post(gateway, BuildXmlDocument(dicParam));
            var dic = FromXmlToList(resultXml);


            //签名校验
            if (dic.ContainsKey("sign") && GetSignString(dic) == dic["sign"])
            {
                result.Parameter = dic;

                if (dic.ContainsKey("return_code") && dic["return_code"] == "SUCCESS"
                    && dic.ContainsKey("result_code") && dic["result_code"] == "SUCCESS")
                {
                    var out_trade_no = dic["out_trade_no"];

                    var price = dic["total_fee"];
                    decimal TotalFee = 0M; decimal.TryParse(price, out TotalFee);

                    if (dic.ContainsKey("trade_state") && dic["trade_state"] == "SUCCESS")
                    {
                        result.OutTradeNo = out_trade_no;
                        result.TradeStatus = dic["return_code"];
                        result.Trade_No = dic["transaction_id"];
                        result.TotalFee = TotalFee / 100;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 处理微信支付结果通知
        /// 返回支付通知结果
        /// </summary>
        /// <returns></returns>
        public WxpayResult GetPayNotityResult()
        {
            WxpayResult result = new WxpayResult();

            string xmlRequest = GetPostString();
            var param = FromXmlToList(xmlRequest);

            result.Parameter = param;

            var price = param["total_fee"];
            decimal TotalFee = 0M; decimal.TryParse(price, out TotalFee);

            string returnCode = "";
            param.TryGetValue("return_code", out returnCode);

            if (returnCode == "SUCCESS")
            {
                var out_trade_no = param["out_trade_no"];
                result = GetQueryOrder(out_trade_no);
            }

            return result;
        }

        /// <summary>
        /// 查询微信订单 
        /// （商户订单号和微信交易号二选一）
        /// </summary>
        /// <param name="out_trade_no">商户号</param>
        /// <param name="transaction_id">交易号</param>
        /// <returns></returns>
        public WxpayResult GetQueryOrder(string out_trade_no = "", string transaction_id = "")
        {
            SortedDictionary<string, string> param = new SortedDictionary<string, string>();

            if (string.IsNullOrWhiteSpace(out_trade_no) && string.IsNullOrWhiteSpace(transaction_id))
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(out_trade_no))
            {
                param.Add("out_trade_no", out_trade_no);
            }
            if (!string.IsNullOrWhiteSpace(transaction_id))
            {
                param.Add("transaction_id", transaction_id);
            }

            WxpayResult result = new WxpayResult();
            var queryParam = QueryOrderRecord(param);

            //签名校验
            if (queryParam.ContainsKey("sign") && GetSignString(queryParam) == queryParam["sign"])
            {
                result.Parameter = queryParam;

                string returnCode = "";
                queryParam.TryGetValue("return_code", out returnCode);
                if (queryParam.ContainsKey("return_code") && queryParam["return_code"] == "SUCCESS"
                                    && queryParam.ContainsKey("result_code") && queryParam["result_code"] == "SUCCESS"
                                    && queryParam.ContainsKey("trade_state") && queryParam["trade_state"] == "SUCCESS")
                {
                    var price = queryParam["total_fee"];
                    decimal TotalFee = 0M; decimal.TryParse(price, out TotalFee);

                    result.OutTradeNo = out_trade_no;
                    result.TradeStatus = queryParam["return_code"];
                    result.Trade_No = queryParam["transaction_id"];
                    result.TotalFee = TotalFee / 100;
                }
            }
            return result;
        }

        /// <summary>
        /// 撤销订单
        /// 支付交易返回失败或支付系统超时，调用该接口撤销交易。
        /// 如果此订单用户支付失败，微信支付系统会将此订单关闭；如果用户支付成功，微信支付系统会将此订单资金退还给用户。
        /// </summary>
        /// <param name="out_trade_no"></param>
        /// <param name="transaction_id"></param>
        /// <returns></returns>
        public bool ReverseOrder(string out_trade_no, string SSLCERT_PATH, string SSLCERT_PASSWORD)
        {
            SortedDictionary<string, string> param = new SortedDictionary<string, string>();

            if (string.IsNullOrWhiteSpace(out_trade_no))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(out_trade_no))
            {
                param.Add("out_trade_no", out_trade_no);
            }
            //if (!string.IsNullOrWhiteSpace(transaction_id))
            //{
            //    param.Add("transaction_id", transaction_id);
            //}

            WxpayResult result = new WxpayResult();

            param.Add("appid", APP_ID);
            param.Add("mch_id", MCH_ID);

            param.Add("nonce_str", GetNonceStr());
            param.Add("sign_type", "MD5");
            param.Add("sign", GetSignString(param));

            string resultXml = HTTPHelper.Post("https://api.mch.weixin.qq.com/secapi/pay/reverse", BuildXmlDocument(param), SSLCERT_PATH, SSLCERT_PASSWORD);
            var dic = FromXmlToList(resultXml);

            //签名校验
            if (dic.ContainsKey("sign") && GetSignString(dic) == dic["sign"])
            {
                result.Parameter = dic;

                string returnCode = "";
                dic.TryGetValue("return_code", out returnCode);
                if (dic.ContainsKey("return_code") && dic["return_code"] == "SUCCESS"
                       && dic.ContainsKey("result_code") && dic["result_code"] == "SUCCESS")
                {
                    return true;
                }
            }
            return false;
        }

        public WxpayResult RefundOrder(Dictionary<string, string> param, string SSLCERT_PATH, string SSLCERT_PASSWORD)
        {
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

            WxpayResult result = new WxpayResult();

            param.Add("appid", APP_ID);
            param.Add("mch_id", MCH_ID);
            param.Add("nonce_str", GetNonceStr());
            param.Add("sign_type", "MD5");
            param.Add("sign", GetSignString(param));

            string resultXml = HTTPHelper.Post(gateway, BuildXmlDocument(param), SSLCERT_PATH, SSLCERT_PASSWORD);
            var dic = FromXmlToList(resultXml);


            //签名校验
            if (dic.ContainsKey("sign") && dic["sign"] == GetSignString(dic))
            {
                result.Parameter = dic;

                if (dic.ContainsKey("return_code") && dic["return_code"] == "SUCCESS"
                    && dic.ContainsKey("result_code") && dic["result_code"] == "SUCCESS")
                {
                    var out_trade_no = dic["out_trade_no"];

                    var price = dic["total_fee"];
                    decimal TotalFee = 0M; decimal.TryParse(price, out TotalFee);

                    result.OutTradeNo = out_trade_no;
                    result.TradeStatus = dic["return_code"];
                    result.Trade_No = dic["transaction_id"];
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
        private IDictionary<string, string> QueryOrderRecord(SortedDictionary<string, string> param)
        {
            param.Add("appid", APP_ID);
            param.Add("mch_id", MCH_ID);

            param.Add("nonce_str", GetNonceStr());
            param.Add("sign_type", "MD5");
            param.Add("sign", GetSignString(param));

            string resultXml = HTTPHelper.Post("https://api.mch.weixin.qq.com/pay/orderquery", BuildXmlDocument(param));
            return FromXmlToList(resultXml);
        }

        /// <summary>
        /// 获取post的流
        /// </summary>
        /// <returns></returns>
        public string GetPostString()
        {
            //接收从微信后台POST过来的数据
            System.IO.Stream stream = HttpContext.Current.Request.InputStream;
            int count = 0;
            byte[] buffer = new byte[1024];
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            while ((count = stream.Read(buffer, 0, 1024)) > 0)
            {
                builder.Append(System.Text.Encoding.UTF8.GetString(buffer, 0, count));
            }
            stream.Flush();
            stream.Close();
            stream.Dispose();

            return builder.ToString();
        }

        /// <summary>
        /// 获取Md5 签名
        /// </summary>
        /// <param name="dic"></param>
        /// <returns></returns>
        public string GetSignString(IDictionary<string, string> dic)
        {
            string preString = CreateURLParamString(dic) + "&key=" + PAY_KEY;
            string signValue = MD5Helper.Sign(preString, CHARTSET).ToUpper();
            return signValue;
        }

        private SortedDictionary<string, string> CreateParameter(IDictionary<string, string> dic)
        {
            if (string.IsNullOrWhiteSpace(APP_ID) || string.IsNullOrWhiteSpace(MCH_ID))
            {
                throw new PayException("缺少必要参数");
            }

            SortedDictionary<string, string> param = new SortedDictionary<string, string>();
            param.Add("appid", APP_ID);//账号ID
            param.Add("mch_id", MCH_ID);//商户号
            param.Add("nonce_str", GetNonceStr());//随机字符串

            if (!string.IsNullOrWhiteSpace(NOTIFY_URL) && !dic.ContainsKey("notify_url"))
                param.Add("notify_url", NOTIFY_URL);//通知地址

            if (TradeType == WxpayTradeTypeEnum.APP
                || TradeType == WxpayTradeTypeEnum.JSAPI
                || TradeType == WxpayTradeTypeEnum.NATIVE)
            {
                param.Add("trade_type", TradeType.ToString());//交易类型
            }

            foreach (var d in dic)
            {
                if (!param.ContainsKey(d.Key))
                    param.Add(d.Key, d.Value);
            }

            param.Add("sign_type", "MD5");
            param.Add("sign", GetSignString(param));

            return param;
        }

        public static string BuildXmlDocument(IDictionary<string, string> dicParam)
        {
            StringBuilder sbXML = new StringBuilder();
            sbXML.Append("<xml>");
            foreach (KeyValuePair<string, string> temp in dicParam)
            {
                sbXML.Append("<" + temp.Key + ">" + temp.Value + "</" + temp.Key + ">");
            }

            sbXML.Append("</xml>");
            return sbXML.ToString();
        }


        private static string CreateURLParamString(IDictionary<string, string> dicArray)
        {
            StringBuilder prestr = new StringBuilder();
            foreach (KeyValuePair<string, string> temp in dicArray.OrderBy(o => o.Key))
            {
                if (temp.Key != "sign" && !string.IsNullOrWhiteSpace(temp.Value))
                    prestr.Append(temp.Key + "=" + temp.Value + "&");
            }

            int nLen = prestr.Length;
            prestr.Remove(nLen - 1, 1);
            return prestr.ToString();
        }

        private static IDictionary<string, string> FromXmlToList(string xml)
        {
            IDictionary<string, string> sortDic = new SortedDictionary<string, string>();
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


        public static string GenerateTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }
        public static string GetNonceStr()
        {
            return Guid.NewGuid().ToString().Replace("-", "");
        }
    }
}
