using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using WebhookCacheInvalidationMvc.Models;

namespace WebhookCacheInvalidationMvc.Helpers
{
    public class EvictingArtifactEqualityComparer : IEqualityComparer<Dependency>
    {
        public bool Equals(Dependency x, Dependency y)
        {
            return x.Type.Equals(y.Type) && x.Codename.Equals(y.Codename);
        }

        public int GetHashCode(Dependency obj)
        {
            return $"{obj.Type}{obj.Codename}".GetHashCode();
        }
    }
}
