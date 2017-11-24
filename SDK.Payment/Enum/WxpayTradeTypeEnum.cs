using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDK.Payment.Enum
{
    public enum WxpayTradeTypeEnum
    {
        /// <summary>
        /// 公众号支付
        /// </summary>
        JSAPI = 0,
        /// <summary>
        /// 原生扫码支付
        /// </summary>
        NATIVE,
        /// <summary>
        /// app支付
        /// </summary>
        APP,
        /// <summary>
        /// wap支付（浏览器调用微信app，H5支付）
        /// </summary>
        WAP,
        /// <summary>
        /// 刷卡支付
        /// </summary>
        MICROPAY
    }
}
