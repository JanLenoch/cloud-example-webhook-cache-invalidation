using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using KenticoCloud.Delivery;

namespace WebhookCacheInvalidationMvc.Models
{
    public class ContentItemBase : IContentItemBase
    {
        public ContentItemSystemAttributes System { get; set; }
    }
}
