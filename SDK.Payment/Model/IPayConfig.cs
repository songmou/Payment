using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDK.Payment.Model
{
    public interface IPayConfig
    {
    }

    public class AlipayConfig : IPayConfig
    {
        public string app_id { get; set; }

        //public string method { get; set; }
        //public string biz_content { get; set; }

        public string sign_type = "RSA";
        public string charset = "utf-8";

        public string rsa_private_key { get; set; }
        public string alipay_rsa_public_key { get; set; }

        public string return_url { get; set; }
        public string notify_url { get; set; }
    }


    public class WxpayConfig : IPayConfig
    {
        public string APP_ID { get; set; }
        public string MCH_ID { get; set; }
        public string PAY_KEY { get; set; }
        public string CHARTSET = "utf-8";

        public string NOTIFY_URL { get; set; }

        public string SSLCERT_PATH { get; set; }
        public string SSLCERT_PASSWORD { get; set; }
    }
}
