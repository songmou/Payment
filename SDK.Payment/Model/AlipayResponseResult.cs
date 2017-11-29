using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDK.Payment.Model
{
    /// <summary>
    /// 支付宝退款返回的model
    /// </summary>
    public class alipay_trade_refund_response
    {
        /// <summary>
        /// 10000 接口调用成功
        /// </summary>
        public string code { get; set; }
        public string msg { get; set; }
        public string trade_no { get; set; }
        public string out_trade_no { get; set; }
        public string buyer_logon_id { get; set; }
        public string fund_change { get; set; }
        public decimal refund_fee { get; set; }
        public string gmt_refund_pay { get; set; }

        public Dictionary<string, object> refund_detail_item_list { get; set; }
        public string store_name { get; set; }
        public string buyer_user_id { get; set; }
    }
}
