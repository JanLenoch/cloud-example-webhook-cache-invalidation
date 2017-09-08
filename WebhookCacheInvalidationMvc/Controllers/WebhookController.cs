using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using KenticoCloud.Delivery;
using Microsoft.AspNetCore.Mvc;
using WebhookCacheInvalidationMvc.Filters;
using WebhookCacheInvalidationMvc.Helpers;
using WebhookCacheInvalidationMvc.Models;
using WebhookCacheInvalidationMvc.Services;

namespace WebhookCacheInvalidationMvc.Controllers
{
    public class WebhookController : BaseController
    {
        protected readonly ICacheManager _cacheManager;

        public WebhookController(IDeliveryClient deliveryClient, ICacheManager cacheManager) : base(deliveryClient) => _cacheManager = cacheManager;

        [ServiceFilter(typeof(KenticoCloudSignatureActionFilter))]
        public IActionResult Index([FromBody] KenticoCloudWebhookModel model)
        {
            switch (model.Message.Type)
            {
                case CacheHelper.CONTENT_ITEM_TYPE_CODENAME:
                    switch (model.Message.Operation)
                    {
                        case "archive":
                        case "unpublish":
                        case "upsert":
                            foreach (var item in model.Data.Items)
                            {
                                _cacheManager.InvalidateEntry(new EvictingArtifact
                                {
                                    Type = item.Type,
                                    Codename = item.Codename
                                });
                            }

                            break;
                        default:
                            break;
                    }

                    break;
                default:
                    break;
            }

            return Ok();
        }
    }
}
