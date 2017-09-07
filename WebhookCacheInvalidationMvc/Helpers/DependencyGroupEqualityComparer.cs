using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using WebhookCacheInvalidationMvc.Models;

namespace WebhookCacheInvalidationMvc.Helpers
{
    public class DependencyGroupEqualityComparer : IEqualityComparer<DependencyGroup>
    {
        public bool Equals(DependencyGroup x, DependencyGroup y)
        {
            if (x.EvictingArtifacts.Count == y.EvictingArtifacts.Count)
            {
                // Compare sets
                return new HashSet<EvictingArtifact>(x.EvictingArtifacts, new EvictingArtifactEqualityComparer()).SetEquals(y.EvictingArtifacts);
            }
            else
            {
                return false;
            }
        }

        public int GetHashCode(DependencyGroup obj)
        {
            // TODO Verify the independence of order, verify if hashset.gethashcode uses item.gethashcode.
            return new HashSet<EvictingArtifact>(obj.EvictingArtifacts, new EvictingArtifactEqualityComparer()).GetHashCode();
        }
    }
}
