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
    public interface IPayResult
    {
        /// <summary>
        /// 商户订单号
        /// </summary>
        string OutTradeNo { get; set; }

        /// <summary>
        /// 支付流水号
        /// </summary>
        string Trade_No { get; set; }

        /// <summary>
        /// 交易总金额
        /// </summary>
        decimal TotalFee { get; set; }

        /// <summary>
        /// 交易状态(只有Success才成功，否则失败)
        /// </summary>
        string TradeStatus { get; set; }

        /// <summary>
        /// 提示信息
        /// </summary>
        string Message { get; set; }

        /// <summary>
        /// 其他额外参数
        /// </summary>
        IDictionary<string, object> Parameter { get; set; }
    }
    public class PayResult : IPayResult
    {
        public string OutTradeNo { get; set; }
        public string Trade_No { get; set; }
        public decimal TotalFee { get; set; }
        public string TradeStatus { get; set; }
        public string Message { get; set; }
        public IDictionary<string, object> Parameter { get; set; }
    }
}
