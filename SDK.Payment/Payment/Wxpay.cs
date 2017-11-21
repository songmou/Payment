using SDK.Payment.Enum;
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

        /// <summary>
        /// 统一下单
        /// </summary>
        /// <param name="param">额外的参数</param>
        /// <returns></returns>
        public string UnifiedOrder(Dictionary<string, string> param)
        {
            string requestXml = this.BuildRequest(param);
            string resultXml = HTTPHelper.Post("https://api.mch.weixin.qq.com/pay/unifiedorder", requestXml);

            var dic = FromXml(resultXml);

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
                    string appid = "";
                    dic.TryGetValue("appid", out appid);
                    string prepay_id = "";
                    dic.TryGetValue("prepay_id", out prepay_id);

                    string preString = CreateURLParamString(dic) + "&key=" + PAY_KEY;
                    string sign = MD5Helper.Sign(preString, CHARTSET).ToUpper();

                    var JsapiParam = new
                    {
                        appId = appid,
                        timeStamp = GenerateTimeStamp(),
                        nonceStr = Guid.NewGuid().ToString().Replace("-", ""),
                        package = "prepay_id=" + prepay_id,
                        signType = "MD5",
                        paySign = sign
                    };
                    return JsapiParam.ToJson();
                }
                else
                    throw new Exception(" WAP 未实现");
            }
            else
                throw new Exception("后台统一下单失败");

            return "error";
        }


        private string BuildRequest(Dictionary<string, string> dic)
        {
            SortedDictionary<string, string> dicParam = CreateParameter(dic);

            string preString = CreateURLParamString(dicParam) + "&key=" + PAY_KEY;
            string sign = MD5Helper.Sign(preString, CHARTSET).ToUpper();
            dicParam.Add("sign", sign);

            return BuildForm(dicParam);
        }

        private SortedDictionary<string, string> CreateParameter(Dictionary<string, string> dic)
        {
            SortedDictionary<string, string> param = new SortedDictionary<string, string>();
            param.Add("appid", APP_ID);//账号ID
            param.Add("mch_id", MCH_ID);//商户号
            param.Add("nonce_str", Guid.NewGuid().ToString().Replace("-", ""));//随机字符串

            if (!string.IsNullOrWhiteSpace(NOTIFY_URL) && !dic.ContainsKey("notify_url"))
                param.Add("notify_url", NOTIFY_URL);//通知地址

            param.Add("trade_type", TradeType.ToString());//交易类型
            foreach (var d in dic)
            {
                if (!param.ContainsKey(d.Key))
                    param.Add(d.Key, d.Value);
            }

            return param;
        }




        private static string CreateURLParamString(SortedDictionary<string, string> dicArray)
        {
            StringBuilder prestr = new StringBuilder();
            foreach (KeyValuePair<string, string> temp in dicArray.OrderBy(o => o.Key))
            {
                prestr.Append(temp.Key + "=" + temp.Value + "&");
            }

            int nLen = prestr.Length;
            prestr.Remove(nLen - 1, 1);
            return prestr.ToString();
        }

        private static string BuildForm(SortedDictionary<string, string> dicParam)
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
        private static SortedDictionary<string, string> FromXml(string xml)
        {
            SortedDictionary<string, string> sortDic = new SortedDictionary<string, string>();
            if (string.IsNullOrEmpty(xml))
            {
                throw new PayException("将空的xml串转换为WxPayData不合法!");
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
    }
}
