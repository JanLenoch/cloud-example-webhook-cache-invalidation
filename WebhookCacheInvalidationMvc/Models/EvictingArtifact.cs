using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebhookCacheInvalidationMvc.Models
{
    public class EvictingArtifact
    {
        public string Type { get; set; }
        public string Codename { get; set; }
    }
}
