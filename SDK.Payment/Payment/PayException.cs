using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDK.Payment.Payment
{
    public class PayException : Exception
    {
        public PayException()
            : base()
        {
        }

        public PayException(string message)
            : base(message)
        {
        }

        public PayException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
