using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WebhookCacheInvalidationMvc.Models
{
    public class DependencyGroup
    {
        public List<Dependency> EvictingArtifacts { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
    }
}
