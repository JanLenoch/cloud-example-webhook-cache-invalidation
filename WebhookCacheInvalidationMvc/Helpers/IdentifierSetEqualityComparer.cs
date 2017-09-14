using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using WebhookCacheInvalidationMvc.Models;

namespace WebhookCacheInvalidationMvc.Helpers
{
    public class IdentifierSetEqualityComparer : IEqualityComparer<IdentifierSet>
    {
        public bool Equals(IdentifierSet x, IdentifierSet y)
        {
            return x.Type.Equals(y.Type) && x.Codename.Equals(y.Codename);
        }

        public int GetHashCode(IdentifierSet obj)
        {
            return $"{obj.Type}{obj.Codename}".GetHashCode();
        }
    }
}
