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

    public enum AlipayTradeEnum
    {
        /// <summary>
        /// 网站支付
        /// </summary>
        Website = 0,
        /// <summary>
        /// wap支付
        /// </summary>
        Wap,
        /// <summary>
        /// APP支付
        /// </summary>
        App,
        /// <summary>
        /// 付钱码：条码&二维码支付
        /// </summary>
        Qrcode,
        /// <summary>
        /// 用户扫描支付
        /// </summary>
        Scanpay,
        /// <summary>
        /// 退款
        /// </summary>
        Refund
    }

    public class Alipay
    {
        private AlipayConfig AlipayConfig = null;

        private string Gateway = "https://openapi.alipay.com/gateway.do";
        //沙箱环境网关
        //private string Gateway = "https://openapi.alipaydev.com/gateway.do";

        public Alipay(AlipayConfig _AlipayConfig)
        {
            AlipayConfig = _AlipayConfig;
        }


        /// <summary>
        /// 支付的html form表单
        /// 适用于<see cref="AlipayTradeEnum.Website"/>、<see cref="AlipayTradeEnum.Wap"/> 等
        /// </summary>
        /// <param name="biz_content"></param>
        /// <returns></returns>
        public string GetFormHtml(AlipayTradeEnum? _traceEnum, object biz_content)
        {
            var dicParam = GetParameter(_traceEnum, biz_content);

            StringBuilder sbHtml = new StringBuilder();
            sbHtml.Append("<form id='alipaysubmit' name='alipaysubmit' action='" + Gateway + "?_input_charset=" + AlipayConfig.charset + "' method='get'>");

            foreach (KeyValuePair<string, object> temp in dicParam)
            {
                sbHtml.Append("<input type='hidden' name='" + temp.Key + "' value='" + temp.Value.ToString() + "'/>");
            }

            sbHtml.Append("<input type='submit' value='确认' style='display:none;'></form>");
            sbHtml.Append("<script>document.forms['alipaysubmit'].submit();</script>");
            return sbHtml.ToString();
        }



        /// <summary>
        /// 支付回调结果
        /// </summary>
        /// <returns>null失败，否则验证成功</returns>
        public IPayResult GetPayNotityResult(Dictionary<string, object> param)
        {
            IPayResult result = new PayResult();
            result.Parameter = param;

            var flag = AlipaySignature.RSACheckV1(param, AlipayConfig.alipay_rsa_public_key, AlipayConfig.charset);
            if (flag && param.ContainsKey("total_amount"))
            {
                decimal total_amount = 0M;
                decimal.TryParse(param.TryGetString("total_amount") ?? "", out total_amount);
                if (total_amount > 0)
                {
                    result.OutTradeNo = param.TryGetString("out_trade_no");
                    result.Trade_No = param.TryGetString("trade_no");
                    result.TotalFee = total_amount;
                    result.TradeStatus = param.TryGetString("trade_status");
                }
            }
            return result;
        }

        /// <summary>
        /// 通用的交易请求
        /// 适用于<see cref="AlipayTradeEnum.Scanpay"/>等
        /// </summary>
        /// <param name="tradeType"></param>
        /// <param name="biz_content"></param>
        /// <returns></returns>
        public dynamic GetResponseBody(AlipayTradeEnum? tradeType, object biz_content)
        {
            var parameters = GetParameter(tradeType, biz_content);

            byte[] postData = Encoding.GetEncoding(AlipayConfig.charset).GetBytes(HTTPHelper.BuildQuery(parameters, AlipayConfig.charset));
            string body = HTTPHelper.Post(this.Gateway + "?charset=" + AlipayConfig.charset, postData);
            var result = JsonHelper.JsonToObject<IDictionary<string, dynamic>>(body);

            if (result != null && result.Count > 1 && result.ContainsKey("sign"))
            {
                return result.Where(q => q.Key != "sign").FirstOrDefault().Value;
            }

            return null;
        }

        /// <summary>
        /// 支付宝请求参数集合
        /// 适用于<see cref="AlipayTradeEnum.App"/>等获取请求参数
        /// </summary>
        /// <param name="bizContent"></param>
        /// <returns></returns>
        public SortedDictionary<string, object> GetParameter(AlipayTradeEnum? tradeType, object biz_content)
        {
            var dic = new SortedDictionary<string, object>();

            #region BASEPARAM
            dic.Add("app_id", AlipayConfig.app_id);
            dic.Add("method", GetMethod(tradeType));
            dic.Add("format", "JSON");
            dic.Add("charset", AlipayConfig.charset);

            dic.Add("sign_type", AlipayConfig.sign_type);

            dic.Add("timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            dic.Add("version", "1.0");

            //只有手机和PC支付有同步回调地址
            if (tradeType == AlipayTradeEnum.Wap || tradeType == AlipayTradeEnum.Website)
            {
                dic.Add("return_url", AlipayConfig.return_url);
            }

            if (tradeType == AlipayTradeEnum.Wap
                || tradeType == AlipayTradeEnum.Website
                || tradeType == AlipayTradeEnum.App
                || tradeType == AlipayTradeEnum.Qrcode)
                dic.Add("notify_url", AlipayConfig.notify_url);

            #endregion

            #region BIZPARAM
            dic.Add("biz_content", JsonHelper.ToJson(biz_content));
            #endregion

            var sign = AlipaySignature.RSASign(dic, AlipayConfig.rsa_private_key, AlipayConfig.charset, AlipayConfig.sign_type);
            dic.Add("sign", sign);

            return dic;
        }

        #region Private Method
        /// <summary>
        /// 获取支付宝业务对应接口的method
        /// </summary>
        /// <param name="tradeType"></param>
        /// <returns></returns>
        private string GetMethod(AlipayTradeEnum? tradeType)
        {
            string Method = "";
            switch (tradeType)
            {
                case AlipayTradeEnum.Website:
                    Method = "alipay.trade.page.pay";
                    break;
                case AlipayTradeEnum.App:
                    Method = "alipay.trade.app.pay";
                    break;
                case AlipayTradeEnum.Wap:
                    Method = "alipay.trade.wap.pay";
                    break;
                case AlipayTradeEnum.Scanpay:
                    Method = "alipay.trade.precreate";
                    break;
                case AlipayTradeEnum.Refund:
                    Method = "alipay.trade.refund";
                    break;
                default:
                    break;
            }
            return Method;
        }

        #endregion
    }
}
