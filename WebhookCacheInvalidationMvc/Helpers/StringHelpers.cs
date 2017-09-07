using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebhookCacheInvalidationMvc.Helpers
{
    public static class StringHelpers
    {
        public static string Join(IEnumerable<string> strings)
        {
            return strings != null ? string.Join("|", strings) : string.Empty;
        }

        public static string Join(params string[] strings)
        {
            return Join((IEnumerable<string>)strings);
        }
    }
}
