using SDK.Payment.Model;
using SDK.Payment.Payment;
using SDK.Payment.Utility;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace SDK.Demo.Controllers
{
    public class HomeController : Controller
    {
        //临时域名
        static string domain = "http://song.ngrok.xiaomiqiu.cn";

        public ActionResult Index()
        {
            return View();
        }

        string productname = "支付 SDK1.0 ";
        decimal actualAmount = 0.01M;


        #region 支付宝支付

        #region 支付宝测试账号
        string ALI_APP_ID = "2016080400161635";
        //RSA私钥 路径
        string rsa_private_key = @"D:\Pay\alipaydev\rsa_private_key.pem";
        //RSA支付宝公钥 路径
        string alipay_rsa_public_key = @"D:\Pay\alipaydev\alipay_rsa_public_key.pem";


        string ALI_NOTITY_URL = domain + "/AlipayNotity";
        string ALI_RETURN_URL = domain + "/PaySuccess";

        Alipay alipay = new Alipay(new AlipayConfig() { });
        #endregion

        public ActionResult AlipayDemo()
        {
            return View();
        }

        /// <summary>
        /// 电脑网站支付
        /// </summary>
        /// <returns></returns>
        public ActionResult AliWebsite()
        {
            var orderNo = DateTime.Now.ToString("yyMMddfff");
            //构造请求参数
            object biz_content = new
            {
                out_trade_no = orderNo,
                total_amount = actualAmount,
                subject = productname + orderNo,
                body = productname,
                product_code = "FAST_INSTANT_TRADE_PAY",//固定值：PC电脑端支付为 FAST_INSTANT_TRADE_PAY，手机支付为 QUICK_WAP_WAY，APP支付为 QUICK_MSECURITY_PAY
                passback_params = "KaungPaySDK"
            };


            return Content(alipay.GetFormHtml(AlipayTradeEnum.Website,biz_content.ToJson()));
        }


        public ActionResult AliWap()
        {
            var orderNo = DateTime.Now.ToString("yyMMddfff");
            //构造请求参数
            object biz_content = new
            {
                out_trade_no = orderNo,
                total_amount = actualAmount,
                subject = productname + orderNo,
                body = productname,
                product_code = "QUICK_WAP_WAY",//固定值：PC电脑端支付为 FAST_INSTANT_TRADE_PAY，手机支付为 QUICK_WAP_WAY，APP支付为 QUICK_MSECURITY_PAY
                passback_params = "KaungPaySDK"
            };
            
            return Content(alipay.GetFormHtml(AlipayTradeEnum.Wap, biz_content.ToJson()));
        }
        public ActionResult AliApp()
        {
            var orderNo = DateTime.Now.ToString("yyMMddfff");
            //构造请求参数
            object biz_content = new
            {
                out_trade_no = orderNo,
                total_amount = actualAmount,
                subject = productname + orderNo,
                body = productname,
                product_code = "QUICK_MSECURITY_PAY",//固定值：PC电脑端支付为 FAST_INSTANT_TRADE_PAY，手机支付为 QUICK_WAP_WAY，APP支付为 QUICK_MSECURITY_PAY
                passback_params = "KaungPaySDK"
            };

            var result = alipay.GetParameter(AlipayTradeEnum.App, biz_content.ToJson());
            return Content(result.ToJson());
        }
        public ActionResult AliScanpay()
        {
            var orderNo = DateTime.Now.ToString("yyMMddfff");
            //构造请求参数
            object biz_content = new
            {
                out_trade_no = orderNo,
                total_amount = actualAmount,
                subject = productname + orderNo,
                body = productname
            };
            
            var body = alipay.GetResponseBody(AlipayTradeEnum.Scanpay, biz_content.ToJson());
            
            return Content("请求出错");
        }


        public ActionResult AlipayNotity()
        {
            Dictionary<string, object> sArray = new Dictionary<string, object>();
            System.Collections.Specialized.NameValueCollection coll = Request.Form;
            String[] requestItem = coll.AllKeys;
            for (var i = 0; i < requestItem.Length; i++)
            {
                sArray.Add(requestItem[i], coll[requestItem[i]]);
            }

            var result = alipay.GetPayNotityResult(sArray);
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



        ///// <summary>
        ///// 通过商户号发起退款
        ///// </summary>
        ///// <param name="orderNo">商户订单号</param>
        ///// <returns></returns>
        //public ActionResult AlipayRefund(string orderNo)
        //{
        //    //构造请求参数
        //    object biz_content = new
        //    {
        //        out_trade_no = orderNo,
        //        refund_amount = actualAmount
        //    };
        //    var result = alipay.GetFormHtml(AlipayTradeEnum.Refund,biz_content.ToJson());

        //    if (result != null && result.code == "10000")
        //        return Content("退款成功");
        //    else
        //        return Content("退款失败：" + result.msg);
        //}
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

        string WX_APP_ID = "wx2f306bd45b100d13";
        string WX_MCH_ID = "1483617922";
        string WX_PAY_KEY = "c98da17154823f9b2adb14c2ce0cac87";
        string WX_NOTITY_URL = domain + "/WxpayNotity";

        string openid = "o1UgGxEvCZlAKsBlG8dY9E3ysKG0";

        string SSLCERT_PATH = "D:\\Pay\\S201703010\\apiclient_cert.p12";
        string SSLCERT_PASSWORD = "1483617922";

        Wxpay wxpay = new Wxpay(new WxpayConfig() { });

        public ActionResult WxpayDemo()
        {
            return View();
        }

        /// <summary>
        /// 公众号支付
        /// </summary>
        /// <returns></returns>
        public ActionResult WxpayJsapi()
        {
            var orderNo = DateTime.Now.ToString("yyMMddfff");
            
            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("openid", openid);
            param.Add("body", productname);
            param.Add("out_trade_no", orderNo);
            param.Add("total_fee", ((int)(actualAmount * 100)).ToString());
            param.Add("spbill_create_ip", HTTPHelper.GetIP());

            var jsApiParam = wxpay.UnifiedOrder(WxpayTradeEnum.JSAPI,param);

            return Content(jsApiParam);
        }

        /// <summary>
        /// 扫码支付
        /// </summary>
        /// <returns></returns>
        public ActionResult WxpayNative()
        {
            var orderNo = DateTime.Now.ToString("yyMMddfff");

            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("body", productname);
            param.Add("out_trade_no", orderNo);
            param.Add("total_fee", ((int)(actualAmount * 100)).ToString());
            param.Add("spbill_create_ip", HTTPHelper.GetIP());

            var ImgSrc = wxpay.UnifiedOrder(WxpayTradeEnum.NATIVE, param);

            return Content(ImgSrc);
        }

        /// <summary>
        /// 刷卡支付
        /// </summary>
        /// <param name="auth_code"></param>
        /// <returns></returns>
        public ActionResult WxpayMicropay(string auth_code)
        {
            var orderNo = DateTime.Now.ToString("yyMMddfff");
            
            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("auth_code", auth_code);
            param.Add("body", productname);
            param.Add("out_trade_no", orderNo);
            param.Add("total_fee", ((int)(actualAmount * 100)).ToString());
            param.Add("spbill_create_ip", HTTPHelper.GetIP());

            var result = wxpay.GetMicropayResult(param);

            string err_code = result.Parameter.TryGetString("err_code");

            #region 等待用户输入密码，循环5次查询订单的支付状态
            int loop = 1;
            IPayResult queryParam = new PayResult();
            while (err_code == "USERPAYING" && loop < 5
                && queryParam.TradeStatus != "SUCCESS")
            {
                System.Threading.Thread.Sleep(5 * 1000);
                loop++;

                queryParam = wxpay.GetQueryOrder(orderNo);
            };
            #endregion

            //如果还是未支付成功状态,则撤销订单
            if (result.TradeStatus != "SUCCESS"
                && err_code == "USERPAYING" && queryParam.TradeStatus != "SUCCESS")
            {
                var r = wxpay.ReverseOrder(orderNo);
                return Content("交易已撤销");
            }


            if (result.TradeStatus == "SUCCESS" || queryParam.TradeStatus == "SUCCESS")
                return Content("支付成功");
            else
                return Content("支付失败，请重新发起支付");
        }

        /// <summary>
        /// 撤销订单
        /// </summary>
        /// <param name="orderNo"></param>
        /// <returns></returns>
        public ActionResult ReverseOrderRecord(string orderNo)
        {
            var r = wxpay.ReverseOrder(orderNo);
            if (r)
            {
                //撤销订单成功
            }
            return Content(r.ToJson());
        }

        /// <summary>
        /// 订单退款
        /// </summary>
        /// <param name="orderNo"></param>
        /// <returns></returns>
        public ActionResult RefundOrderRecord(string orderNo)
        {
            var refundNo = DateTime.Now.ToString("yyMMddfff");

            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("out_trade_no", orderNo);
            param.Add("out_refund_no", refundNo);
            param.Add("total_fee", ((int)(actualAmount * 100)).ToString());
            param.Add("refund_fee", ((int)(actualAmount * 100)).ToString());

            var result = wxpay.RefundOrder(param);
            if (result.TradeStatus == "SUCCESS")
            {
                //退款成功
            }
            return Content(result.ToJson());
        }


        //public ActionResult WxpayNotity()
        //{
        //    //接收从微信后台POST过来的数据
        //    Stream s = Request.InputStream;
        //    int count = 0;
        //    byte[] buffer = new byte[1024];
        //    StringBuilder builder = new StringBuilder();
        //    while ((count = s.Read(buffer, 0, 1024)) > 0)
        //    {
        //        builder.Append(Encoding.UTF8.GetString(buffer, 0, count));
        //    }
        //    s.Close();
        //    s.Dispose();

        //    var result = wxpay.();
        //    if (result != null && result.TradeStatus == "SUCCESS")
        //    {
        //        //支付成功(会出现多次) todo
        //        return Content(ReturnXmlContent(true, "支付成功"));
        //    }

        //    return Content(ReturnXmlContent(false));
        //}

        public string ReturnXmlContent(bool r, string message = "")
        {
            return @"<xml><return_code><![CDATA[" + (r ? "SUCCESS" : "FAIL") + "]]></return_code><return_msg><![CDATA[" + message + "]]></return_msg></xml>";
        }
        #endregion
    }
}