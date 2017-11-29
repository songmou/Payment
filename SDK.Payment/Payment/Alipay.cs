using SDK.Payment.Enum;
using SDK.Payment.Model;
using SDK.Payment.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SDK.Payment.Payment
{
    /// <summary>
    /// 支付宝的公共调用SDK
    /// 更新于 2017.11.15
    /// </summary>
    public class Alipay
    {
        private AlipayTradeTypeEnum TradeType;

        //private string Gateway = "https://openapi.alipay.com/gateway.do";

        //沙箱环境网关
        private string Gateway = "https://openapi.alipaydev.com/gateway.do";

        private string App_id;
        private string Method;
        private string Rsa_private_key;
        private string Sign_type;
        private string Charset;
        private string Biz_content;

        private string Alipay_rsa_public_key;

        public string Return_url;
        public string Notify_url;

        /// <summary>
        /// 支付的实例化(手机、PC、App)
        /// </summary>
        /// <param name="tradeType"></param>
        /// <param name="app_id"></param>
        /// <param name="rsa_private_key"></param>
        /// <param name="notify_url"></param>
        /// <param name="return_url"></param>
        /// <param name="sign_type"></param>
        /// <param name="charset"></param>
        public Alipay(AlipayTradeTypeEnum tradeType, string app_id, string rsa_private_key, string sign_type = "RSA", string charset = "utf-8")
        {
            TradeType = tradeType;
            App_id = app_id;
            Rsa_private_key = rsa_private_key;

            switch (tradeType)
            {
                case AlipayTradeTypeEnum.Website:
                    Method = "alipay.trade.page.pay";
                    break;
                case AlipayTradeTypeEnum.App:
                    Method = "alipay.trade.app.pay";
                    break;
                case AlipayTradeTypeEnum.Wap:
                    Method = "alipay.trade.wap.pay";
                    break;
                case AlipayTradeTypeEnum.Scanpay:
                    Method = "alipay.trade.precreate";
                    break;
                case AlipayTradeTypeEnum.Refund:
                    Method = "alipay.trade.refund";
                    break;
                default:
                    break;
            }
            Sign_type = sign_type;
            Charset = charset;
        }


        /// <summary>
        /// 异步通知的实例化
        /// </summary>
        /// <param name="rsa_private_key"></param>
        /// <param name="charset"></param>
        public Alipay(string alipay_rsa_public_key, string charset = "utf-8")
        {
            Alipay_rsa_public_key = alipay_rsa_public_key;
            Charset = charset;
        }

        /// <summary>
        /// 支付的html表单
        /// </summary>
        /// <param name="biz_content"></param>
        /// <returns></returns>
        public string BuildFormHtml(string biz_content)
        {
            Biz_content = biz_content;

            SortedDictionary<string, string> dicParam = GetParameter(Biz_content);

            StringBuilder sbHtml = new StringBuilder();
            sbHtml.Append("<form id='alipaysubmit' name='alipaysubmit' action='" + Gateway + "?_input_charset=" + Charset + "' method='get'>");

            foreach (KeyValuePair<string, string> temp in dicParam)
            {
                sbHtml.Append("<input type='hidden' name='" + temp.Key + "' value='" + temp.Value + "'/>");
            }

            sbHtml.Append("<input type='submit' value='确认' style='display:none;'></form>");
            sbHtml.Append("<script>document.forms['alipaysubmit'].submit();</script>");
            return sbHtml.ToString();
        }

        /// <summary>
        /// 支付回调结果
        /// </summary>
        /// <returns>null失败，否则验证成功</returns>
        public AlipayResult GetPayNotityResult()
        {
            Dictionary<string, string> forms = GetRequestParameter();
            AlipayResult result = new AlipayResult();
            result.Parameter = forms;

            var flag = AlipaySignature.RSACheckV1(forms, Alipay_rsa_public_key, Charset);
            if (flag && forms.ContainsKey("total_amount"))
            {
                decimal total_amount = 0M;
                decimal.TryParse(forms["total_amount"] ?? "", out total_amount);
                if (total_amount > 0)
                {
                    result.OutTradeNo = forms["out_trade_no"];
                    result.Trade_No = forms["trade_no"];
                    result.TotalFee = total_amount;
                    result.TradeStatus = forms["trade_status"];
                }
            }
            return result;
        }

        /// <summary>
        /// 支付宝请求参数集合
        /// </summary>
        /// <param name="bizContent"></param>
        /// <returns></returns>
        public SortedDictionary<string, string> GetParameter(string bizContent)
        {
            SortedDictionary<string, string> dic = new SortedDictionary<string, string>();

            #region BASEPARAM
            dic.Add("app_id", App_id);
            dic.Add("method", Method);
            dic.Add("format", "JSON");
            dic.Add("charset", Charset);

            dic.Add("sign_type", Sign_type);

            dic.Add("timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            dic.Add("version", "1.0");

            //只有手机和PC支付有同步回调地址
            if (TradeType == AlipayTradeTypeEnum.Wap || TradeType == AlipayTradeTypeEnum.Website)
            {
                dic.Add("return_url", Return_url);
            }

            if (TradeType == AlipayTradeTypeEnum.Wap
                || TradeType == AlipayTradeTypeEnum.Website
                || TradeType == AlipayTradeTypeEnum.App
                || TradeType == AlipayTradeTypeEnum.Qrcode)
                dic.Add("notify_url", Notify_url);

            #endregion

            #region BIZPARAM
            dic.Add("biz_content", bizContent);
            #endregion

            var sign = AlipaySignature.RSASign(dic, Rsa_private_key, Charset, Sign_type);
            dic.Add("sign", sign);

            return dic;
        }

        /// <summary>
        /// 获取请求参数集合
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetRequestParameter()
        {
            Dictionary<string, string> sArray = new Dictionary<string, string>();
            System.Collections.Specialized.NameValueCollection coll = HttpContext.Current.Request.Form;
            String[] requestItem = coll.AllKeys;
            for (var i = 0; i < requestItem.Length; i++)
            {
                sArray.Add(requestItem[i], coll[requestItem[i]]);
            }
            return sArray;

        }

        public object GetResponseBody(string biz_content)
        {
            Biz_content = biz_content;

            SortedDictionary<string, string> parameters = GetParameter(Biz_content);

            byte[] postData = Encoding.GetEncoding(this.Charset).GetBytes(HTTPHelper.BuildQuery(parameters, this.Charset));
            string body = HTTPHelper.Post(this.Gateway + "?charset=" + this.Charset, postData);
            Dictionary<string, object> result = JsonHelper.JsonToObject<dynamic>(body);
            if (result != null && result.Count > 1 && result.ContainsKey("sign"))
            {
                return result.Where(q => q.Key != "sign").FirstOrDefault().Value;
            }

            return null;
        }

        /// <summary>
        /// 退款
        /// </summary>
        /// <param name="biz_content"></param>
        /// <returns></returns>
        public alipay_trade_refund_response Refund(string biz_content)
        {
            alipay_trade_refund_response result = GetResponseBody(biz_content) as alipay_trade_refund_response;

            return result;
        }
    }
}
