using SDK.Payment.Enum;
using SDK.Payment.Payment;
using SDK.Payment.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SDK.Demo.Controllers
{
    public class HomeController : Controller
    {
        //临时域名
        static string domain = "http://25770073.ngrok.io";

        public ActionResult Index()
        {
            return View();
        }

        string productname = "支付 SDK1.0 ";
        decimal actualAmount = 0.01M;


        #region 支付宝支付

        #region 支付宝测试账号
        string ALI_APP_ID = "201608040016****";
        //RSA私钥 路径
        string rsa_private_key = @"D:\alipaydev\rsa_private_key.pem";
        //RSA支付宝公钥 路径
        string alipay_rsa_public_key = @"D:\alipaydev\alipay_rsa_public_key.pem";


        string ALI_NOTITY_URL = domain + "/AlipayNotity";
        string ALI_RETURN_URL = domain + "/PaySuccess";
        #endregion

        public ActionResult AlipayDemo()
        {
            var orderNo = DateTime.Now.ToString("yyMMddfff");
            //构造请求参数
            object biz_content = new
            {
                out_trade_no = orderNo,
                total_amount = actualAmount,
                subject = productname + orderNo,
                body = productname,
                product_code = "FAST_INSTANT_TRADE_PAY",//固定值：PC电脑端支付为 FAST_INSTANT_TRADE_PAY，手机支付为 product_code，APP支付为 QUICK_MSECURITY_PAY
                passback_params = "KaungPaySDK"
            };

            Alipay alipay = new Alipay(AlipayTradeTypeEnum.Website, ALI_APP_ID, rsa_private_key);
            alipay.Notify_url = ALI_NOTITY_URL;
            alipay.Return_url = ALI_RETURN_URL;
            ViewBag.html = alipay.BuildFormHtml(biz_content.ToJson());

            return View();
        }



        public ActionResult AlipayNotity()
        {
            Alipay alipay = new Alipay(alipay_rsa_public_key);

            var result = alipay.GetPayNotityResult();
            if (result != null && !string.IsNullOrWhiteSpace(result.OutTradeNo))
            {
                if (result.TradeStatus == "TRADE_SUCCESS")
                {
                    //交易支付成功 todo
                }
                else if (result.TradeStatus == "TRADE_CLOSED")
                {
                    //未付款交易超时关闭，或支付完成后全额退款
                }
                return Content("success");
            }

            return Content("fail");
        }



        /// <summary>
        /// 通过商户号发起退款
        /// </summary>
        /// <param name="orderNo">商户订单号</param>
        /// <returns></returns>
        public ActionResult AlipayRefund(string orderNo)
        {
            Alipay alipay = new Alipay(AlipayTradeTypeEnum.Refund, ALI_APP_ID, rsa_private_key);

            //构造请求参数
            object biz_content = new
            {
                out_trade_no = orderNo,
                refund_amount = actualAmount
            };
            var result = alipay.Refund(biz_content.ToJson());
            return Content(result.alipay_trade_refund_response.msg);
        }
        #endregion


        public ActionResult PaySuccess()
        {
            return View();
        }
        public ActionResult PayFail()
        {
            return View();
        }

        #region 微信支付

        string WX_APP_ID = "wx*****";
        string WX_MCH_ID = "14********";
        string WX_PAY_KEY = "c9********";
        string WX_NOTITY_URL = domain + "/WxpayNotity";

        public ActionResult WxpayDemo()
        {
            return View();
        }
        public ActionResult WxpayJsapi()
        {
            var orderNo = DateTime.Now.ToString("yyMMddfff");

            Wxpay wxpay = new Wxpay(WxpayTradeTypeEnum.JSAPI, WX_APP_ID, WX_MCH_ID, WX_PAY_KEY, WX_NOTITY_URL);
            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("openid", "o1UgGxEvCZlAKsBlG8dY9E3ysKG0");
            param.Add("body", productname);
            param.Add("out_trade_no", orderNo);
            param.Add("total_fee", ((int)(actualAmount * 100)).ToString());
            param.Add("spbill_create_ip", HTTPHelper.GetIP());

            var jsApiParam = wxpay.UnifiedOrder(param);

            return Content(jsApiParam);
        }

        public ActionResult WxpayNative()
        {
            var orderNo = DateTime.Now.ToString("yyMMddfff");

            Wxpay wxpay = new Wxpay(WxpayTradeTypeEnum.NATIVE, WX_APP_ID, WX_MCH_ID, WX_PAY_KEY, WX_NOTITY_URL);
            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("openid", "o1UgGxEvCZlAKsBlG8dY9E3ysKG0");
            param.Add("body", productname);
            param.Add("out_trade_no", orderNo);
            param.Add("total_fee", ((int)(actualAmount * 100)).ToString());
            param.Add("spbill_create_ip", HTTPHelper.GetIP());

            var ImgSrc = wxpay.UnifiedOrder(param);

            return Content(ImgSrc);
        }

        public ActionResult WxpayNotity()
        {
            Wxpay wxpay = new Wxpay(WX_APP_ID, WX_MCH_ID, WX_PAY_KEY);
            var result = wxpay.GetPayNotityResult();
            if (result != null)
            {
                //支付成功(会出现多次) todo
                return Content(ReturnXmlContent(true,"支付成功"));
            }

            return Content(ReturnXmlContent(false));
        }

        public string ReturnXmlContent(bool r, string message = "")
        {
            return @"<xml><return_code><![CDATA[" + (r ? "SUCCESS" : "FAIL") + "]]></return_code><return_msg><![CDATA[" + message + "]]></return_msg></xml>";
        }
        #endregion
    }
}