using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDK.Payment.Model
{
    /// <summary>
    /// 支付结果基类
    /// </summary>
    public class PayResultBase
    {
        /// <summary>
        /// 商户订单号
        /// </summary>
        public string OutTradeNo { get; set; }

        /// <summary>
        /// 支付交易号
        /// </summary>
        public string Trade_No { get; set; }

        /// <summary>
        /// 交易总金额
        /// </summary>
        public decimal TotalFee { get; set; }

        /// <summary>
        /// 交易状态
        /// </summary>
        public string TradeStatus { get; set; }

        /// <summary>
        /// 其他额外参数
        /// </summary>
        public Dictionary<string, string> Parameter { get; set; }
    }
}
