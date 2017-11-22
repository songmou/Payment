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
        private WxpayTradeTypeEnum TradeType = 0;
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
            SortedDictionary<string, string> dicParam = CreateParameter(param);

            string resultXml = HTTPHelper.Post("https://api.mch.weixin.qq.com/pay/unifiedorder", BuildXmlDocument(dicParam));

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
                        throw new Exception("未找到对应的二维码链接");
                }
                else if (TradeType == WxpayTradeTypeEnum.JSAPI)
                {
                    var jsApiParam = new SortedDictionary<string, string>();
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
                    throw new Exception(" WAP 未实现");
            }
            else
                throw new Exception("后台统一下单失败");

            return "error";
        }

        public WxpayResult GetPayNotityResult()
        {
            string xmlRequest = GetPostString();
            var param = FromXmlToList(xmlRequest);

            var price = param["total_fee"];
            decimal TotalFee = 0M; decimal.TryParse(price, out TotalFee);

            WxpayResult result = null;

            string returnCode = "";
            param.TryGetValue("return_code", out returnCode);

            if (returnCode == "SUCCESS")
            {
                var out_trade_no = param["out_trade_no"];
                var queryParam = QueryOrderRecord(out_trade_no);
                if (queryParam.ContainsKey("return_code") && queryParam["return_code"] == "SUCCESS"
                    && queryParam.ContainsKey("result_code") && queryParam["result_code"] == "SUCCESS"
                    && queryParam.ContainsKey("trade_state") && queryParam["trade_state"] == "SUCCESS")
                {
                    //签名校验
                    if (GetSignString(param) == param["sign"])
                    {
                        result = new WxpayResult();
                        result.OutTradeNo = out_trade_no;
                        result.TradeStatus = param["return_code"];
                        result.Trade_No = param["transaction_id"];
                        result.TotalFee = TotalFee / 100;
                        result.Parameter = param;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 查询微信支付交易的订单（自行判断返回参数）
        /// </summary>
        /// <param name="out_trade_no"></param>
        /// <returns></returns>
        public IDictionary<string, string> QueryOrderRecord(string out_trade_no)
        {
            SortedDictionary<string, string> param = new SortedDictionary<string, string>();
            param.Add("appid", APP_ID);
            param.Add("mch_id", MCH_ID);

            param.Add("out_trade_no", out_trade_no);

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
        public string GetSignString(SortedDictionary<string, string> dic)
        {
            string preString = CreateURLParamString(dic) + "&key=" + PAY_KEY;
            string signValue = MD5Helper.Sign(preString, CHARTSET).ToUpper();
            return signValue;
        }

        private SortedDictionary<string, string> CreateParameter(Dictionary<string, string> dic)
        {
            SortedDictionary<string, string> param = new SortedDictionary<string, string>();
            param.Add("appid", APP_ID);//账号ID
            param.Add("mch_id", MCH_ID);//商户号
            param.Add("nonce_str", GetNonceStr());//随机字符串

            if (!string.IsNullOrWhiteSpace(NOTIFY_URL) && !dic.ContainsKey("notify_url"))
                param.Add("notify_url", NOTIFY_URL);//通知地址

            param.Add("trade_type", TradeType.ToString());//交易类型
            foreach (var d in dic)
            {
                if (!param.ContainsKey(d.Key))
                    param.Add(d.Key, d.Value);
            }

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


        private static string CreateURLParamString(SortedDictionary<string, string> dicArray)
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

        private static SortedDictionary<string, string> FromXmlToList(string xml)
        {
            SortedDictionary<string, string> sortDic = new SortedDictionary<string, string>();
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
