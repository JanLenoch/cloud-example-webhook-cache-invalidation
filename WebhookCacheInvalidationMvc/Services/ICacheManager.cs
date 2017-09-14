using KenticoCloud.Delivery;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using WebhookCacheInvalidationMvc.Models;

namespace WebhookCacheInvalidationMvc.Services
{
    public interface ICacheManager : IDisposable
    {
        /// <summary>
        /// Either fixed or floating period of time required for an entry to expire.
        /// </summary>
        int CacheExpirySeconds { get; set; }

        /// <summary>
        /// Gets an existing cache entry or creates one using the supplied <paramref name="valueFactory"/>.
        /// </summary>
        /// <typeparam name="T">Type of the cache entry value that implements <see cref="IContentItemBase"/></typeparam>
        /// <param name="valueFactory">Method to create the entry</param>
        /// <param name="dependencyListFactory">Method to get a collection of identifiers of entries that the current entry depends upon</param>
        /// <param name="identifierTokens">String tokens that form a unique identifier of the entry</param>
        /// <returns>The value, either cached or obtained through the <paramref name="valueFactory"/>.</returns>
        Task<T> GetOrCreateAsync<T>(Func<Task<T>> valueFactory, Func<T, IEnumerable<IdentifierSet>> dependencyListFactory, IEnumerable<string> identifierTokens);

        /// <summary>
        /// Gets an existing cache entry or creates one using the supplied <paramref name="valueFactory"/>.
        /// </summary>
        /// <param name="valueFactory">Method to create the entry</param>
        /// <param name="dependencyListFactory">Method to get a collection of identifiers of entries that the current entry depends upon</param>
        /// <param name="identifierTokens">String tokens that form a unique identifier of the entry</param>
        /// <returns>The <see cref="DeliveryItemResponse{object}"/> value, either cached or obtained through the <paramref name="valueFactory"/>.</returns>
        Task<DeliveryItemResponse<object>> GetOrCreateObjectAsync(Func<Task<DeliveryItemResponse<object>>> valueFactory, Func<DeliveryItemResponse<object>, IEnumerable<IdentifierSet>> dependencyListFactory, IEnumerable<string> identifierTokens);

        /// <summary>
        /// Gets an existing cache entry or creates one using the supplied <paramref name="valueFactory"/>.
        /// </summary>
        /// <param name="valueFactory">Method to create the entry</param>
        /// <param name="dependencyListFactory">Method to get a collection of identifiers of entries that the current entry depends upon</param>
        /// <param name="identifierTokens">String tokens that form a unique identifier of the entry</param>
        /// <returns>The <see cref="DeliveryItemListingResponse{object}"/> value, either cached or obtained through the <paramref name="valueFactory"/>.</returns>
        Task<DeliveryItemListingResponse<object>> GetOrCreateObjectCollectionAsync(Func<Task<DeliveryItemListingResponse<object>>> valueFactory, Func<DeliveryItemListingResponse<object>, IEnumerable<IdentifierSet>> dependencyListFactory, IEnumerable<string> identifierTokens);

        /// <summary>
        /// Invalidates (clears) an entry.
        /// </summary>
        /// <param name="identifiers">Identifiers of the entry</param>
        void InvalidateEntry(IdentifierSet identifiers);
    }
}