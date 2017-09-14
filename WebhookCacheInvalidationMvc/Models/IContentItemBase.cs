using KenticoCloud.Delivery;

namespace WebhookCacheInvalidationMvc.Models
{
    public interface IContentItemBase
    {
        ContentItemSystemAttributes System { get; set; }
    }
}