using KenticoCloud.Delivery;

namespace WebhookCacheInvalidationMvc.Models
{
    public class ContentItemBase : IContentItemBase
    {
        public ContentItemSystemAttributes System { get; set; }
    }
}
