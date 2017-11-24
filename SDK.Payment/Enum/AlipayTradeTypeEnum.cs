using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDK.Payment.Enum
{
    public enum AlipayTradeTypeEnum
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
        /// 条码&二维码支付
        /// </summary>
        Qrcode,
        /// <summary>
        /// 退款
        /// </summary>
        Refund
    }
}
