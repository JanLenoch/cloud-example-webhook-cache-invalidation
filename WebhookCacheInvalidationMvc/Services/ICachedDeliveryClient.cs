using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using KenticoCloud.Delivery;
using WebhookCacheInvalidationMvc.Models;

namespace WebhookCacheInvalidationMvc.Services
{
    public interface ICachedDeliveryClient : IDeliveryClient
    {
        /// <summary>
        /// Gets one strongly typed content item by its codename.
        /// </summary>
        /// <typeparam name="T">Type of the code-first model that implements <see cref="IContentItemBase"/></typeparam>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">Query parameters, for example for projection or depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemResponse{T}"/> instance that contains the content item with the specified codename.</returns>
        new Task<DeliveryItemResponse<T>> GetItemAsync<T>(string codename, params IQueryParameter[] parameters)
            where T : IContentItemBase;

        /// <summary>
        /// Gets one strongly typed content item by its codename.
        /// </summary>
        /// <typeparam name="T">Type of the code-first model that implements <see cref="IContentItemBase"/></typeparam>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">A collection of query parameters, for example for projection or depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemResponse{T}"/> instance that contains the content item with the specified codename.</returns>
        new Task<DeliveryItemResponse<T>> GetItemAsync<T>(string codename, IEnumerable<IQueryParameter> parameters = null)
            where T : IContentItemBase;

        /// <summary>
        /// Gets one strongly typed content item by its codename.
        /// </summary>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">Query parameters, for example for projection or depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemResponse{object}"/> instance that contains the content item with the specified codename.</returns>
        Task<DeliveryItemResponse<object>> GetRuntimeTypedItemAsync(string codename, params IQueryParameter[] parameters);

        /// <summary>
        /// Gets one strongly typed content item by its codename.
        /// </summary>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">A collection of query parameters, for example for projection or depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemResponse{object}"/> instance that contains the content item with the specified codename.</returns>
        Task<DeliveryItemResponse<object>> GetRuntimeTypedItemAsync(string codename, IEnumerable<IQueryParameter> parameters = null);

        /// <summary>
        /// Searches the content repository for items that match the filter criteria.
        /// Returns strongly typed content items.
        /// </summary>
        /// <typeparam name="T">Type of the code-first model that implements <see cref="IContentItemBase"/></typeparam>
        /// <param name="parameters">Query parameters, for example for filtering, ordering or depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemListingResponse{T}"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        new Task<DeliveryItemListingResponse<T>> GetItemsAsync<T>(params IQueryParameter[] parameters)
            where T : IContentItemBase;

        /// <summary>
        /// Searches the content repository for items that match the filter criteria.
        /// Returns strongly typed content items.
        /// </summary>
        /// <typeparam name="T">Type of the code-first model that implements <see cref="IContentItemBase"/></typeparam>
        /// <param name="parameters">A collection of query parameters, for example for filtering, ordering or depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemListingResponse{T}"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        new Task<DeliveryItemListingResponse<T>> GetItemsAsync<T>(IEnumerable<IQueryParameter> parameters = null)
            where T : IContentItemBase;

        /// <summary>
        /// Searches the content repository for items that match the filter criteria.
        /// Returns content items, each with its proper strong type determined at runtime.
        /// </summary>
        /// <param name="parameters">Query parameters, for example for filtering, ordering or depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemListingResponse{object}"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        Task<DeliveryItemListingResponse<object>> GetRuntimeTypedItemListingAsync(params IQueryParameter[] parameters);

        /// <summary>
        /// Searches the content repository for items that match the filter criteria.
        /// Returns content items, each with its proper strong type determined at runtime.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example for filtering, ordering or depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemListingResponse{object}"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        Task<DeliveryItemListingResponse<object>> GetRuntimeTypedItemListingAsync(IEnumerable<IQueryParameter> parameters);
    }
}
