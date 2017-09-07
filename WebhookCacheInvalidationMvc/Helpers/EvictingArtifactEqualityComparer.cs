using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using WebhookCacheInvalidationMvc.Models;

namespace WebhookCacheInvalidationMvc.Helpers
{
    public class EvictingArtifactEqualityComparer : IEqualityComparer<EvictingArtifact>
    {
        public bool Equals(EvictingArtifact x, EvictingArtifact y)
        {
            return x.Type.Equals(y.Type) && x.Codename.Equals(y.Codename);
        }

        public int GetHashCode(EvictingArtifact obj)
        {
            return $"{obj.Type}{obj.Codename}".GetHashCode();
        }
    }
}
