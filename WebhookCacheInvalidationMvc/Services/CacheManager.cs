using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Threading;
using WebhookCacheInvalidationMvc.Helpers;
using WebhookCacheInvalidationMvc.Models;
using KenticoCloud.Delivery;

namespace WebhookCacheInvalidationMvc.Services
{
    public class CacheManager : ICacheManager, IDisposable
    {
        #region "Fields"

        private bool _disposed = false;
        private readonly IMemoryCache _memoryCache;

        #endregion

        #region "Properties"

        public int CacheExpirySeconds
        {
            get;
            set;
        }

        #endregion

        #region "Constructors"

        public CacheManager(IOptions<ProjectOptions> projectOptions, IMemoryCache memoryCache)
        {
            CacheExpirySeconds = projectOptions.Value.CacheTimeoutSeconds;
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        #endregion

        #region "Public methods"

        public async Task<T> GetOrCreateAsync<T>(Func<Task<T>> valueFactory, Func<T, IEnumerable<IdentifierSet>> dependencyListFactory, IEnumerable<string> identifierTokens)
        {
            if (!_memoryCache.TryGetValue(StringHelpers.Join(identifierTokens), out T entry))
            {
                T response = await valueFactory();
                CreateEntry(response, dependencyListFactory, identifierTokens);

                return response;
            }

            return entry;
        }

        public async Task<DeliveryItemResponse<object>> GetOrCreateObjectAsync(Func<Task<DeliveryItemResponse<object>>> valueFactory, Func<DeliveryItemResponse<object>, IEnumerable<IdentifierSet>> dependencyListFactory, IEnumerable<string> identifierTokens)
        {
            if (!_memoryCache.TryGetValue(StringHelpers.Join(identifierTokens), out DeliveryItemResponse<object> entry))
            {
                DeliveryItemResponse<object> response = await valueFactory();
                CreateEntry(response, dependencyListFactory, identifierTokens);

                return response;
            }

            return entry;
        }

        public async Task<DeliveryItemListingResponse<object>> GetOrCreateObjectCollectionAsync(Func<Task<DeliveryItemListingResponse<object>>> valueFactory, Func<DeliveryItemListingResponse<object>, IEnumerable<IdentifierSet>> dependencyListFactory, IEnumerable<string> identifierTokens)
        {
            if (!_memoryCache.TryGetValue(StringHelpers.Join(identifierTokens), out DeliveryItemListingResponse<object> entry))
            {
                DeliveryItemListingResponse<object> response = await valueFactory();
                CreateEntry(response, dependencyListFactory, identifierTokens);

                return response;
            }

            return entry;
        }

        public void CreateEntry<T>(T value, Func<T, IEnumerable<IdentifierSet>> dependencyListFactory, IEnumerable<string> identifierTokens)
        {
            var dependencies = dependencyListFactory(value);
            var entryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(CacheExpirySeconds));
            var dummyOptions = new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.NeverRemove);

            foreach (var dependency in dependencies)
            {
                var dummyIdentifierTokens = new List<string>();
                dummyIdentifierTokens.Add("dummy");
                dummyIdentifierTokens.Add(dependency.Type);
                dummyIdentifierTokens.Add(dependency.Codename);
                var dummyKey = StringHelpers.Join(dummyIdentifierTokens);
                CancellationTokenSource dummyEntry;

                if (!_memoryCache.TryGetValue(dummyKey, out dummyEntry) || _memoryCache.TryGetValue(dummyKey, out dummyEntry) && dummyEntry.IsCancellationRequested)
                {
                    dummyEntry = _memoryCache.Set(dummyKey, new CancellationTokenSource(), dummyOptions);
                }

                if (dummyEntry != null)
                {
                    entryOptions.AddExpirationToken(new CancellationChangeToken(dummyEntry.Token));
                }
            }

            _memoryCache.Set(StringHelpers.Join(identifierTokens), value, entryOptions);
        }

        public void InvalidateEntry(IdentifierSet identifiers)
        {
            var typeIdentifiers = new List<string>();

            if (identifiers.Type.Equals(CacheHelper.CONTENT_ITEM_TYPE_CODENAME))
            {
                typeIdentifiers.AddRange(new[] { string.Join(string.Empty, CacheHelper.CONTENT_ITEM_TYPE_CODENAME, "_typed"), string.Join(string.Empty, CacheHelper.CONTENT_ITEM_TYPE_CODENAME, "_runtime_typed") });
            }
            else if (identifiers.Type.Equals(CacheHelper.CONTENT_ITEM_LISTING_IDENTIFIER))
            {
                typeIdentifiers.AddRange(new[] { string.Join(string.Empty, CacheHelper.CONTENT_ITEM_LISTING_IDENTIFIER, "_typed"), string.Join(string.Empty, CacheHelper.CONTENT_ITEM_LISTING_IDENTIFIER, "_runtime_typed") });
            }
            else
            {
                typeIdentifiers.Add(identifiers.Type);
            }

            foreach (var typeIdentifier in typeIdentifiers)
            {
                if (_memoryCache.TryGetValue(StringHelpers.Join("dummy", typeIdentifier, identifiers.Codename), out CancellationTokenSource dummyEntry))
                {
                    dummyEntry.Cancel();
                } 
            }
        }

        /// <summary>
        /// The <see cref="IDisposable.Dispose"/> implementation.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region "Non-public methods"

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _memoryCache.Dispose();
            }

            _disposed = true;
        }

        private MemoryCacheEntryOptions CreateOptionsWithSlidingExpiration()
        {
            return new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(CacheExpirySeconds));
        }

        #endregion
    }
}