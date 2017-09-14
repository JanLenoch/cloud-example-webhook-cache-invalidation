using Microsoft.AspNetCore.Mvc;

using WebhookCacheInvalidationMvc.Services;

namespace WebhookCacheInvalidationMvc.Controllers
{
    public class BaseController : Controller
    {
        public BaseController(ICachedDeliveryClient deliveryClient)
        {
            DeliveryClient = deliveryClient;
        }

        protected ICachedDeliveryClient DeliveryClient { get; }
    }
}
