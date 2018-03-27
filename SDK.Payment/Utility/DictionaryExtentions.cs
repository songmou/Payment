using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDK.Payment.Utility
{
    public static class DictionaryExtentions
    {
        public static string TryGetString(this IDictionary<string, object> r, string key)
        {
            if (r == null) return null;

            object v = null;
            r.TryGetValue(key, out v);

            return v != null ? v.ToString() : null;
        }
    }
}
