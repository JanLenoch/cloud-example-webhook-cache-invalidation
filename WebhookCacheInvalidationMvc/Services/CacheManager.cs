using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using KenticoCloud.Delivery;
using Microsoft.Extensions.Caching.Memory;
using System.Threading;
using WebhookCacheInvalidationMvc.Helpers;
using WebhookCacheInvalidationMvc.Models;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Options;

namespace WebhookCacheInvalidationMvc.Services
{
    public class CacheManager : ICacheManager, IDisposable
    {
        private bool _disposed = false;
        private readonly IMemoryCache _memoryCache;

        public int CacheExpirySeconds
        {
            get;
            set;
        }

        public CacheManager(IOptions<ProjectOptions> projectOptions, IMemoryCache memoryCache)
        {
            CacheExpirySeconds = projectOptions.Value.CacheTimeoutSeconds;
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        public async Task<T> GetOrCreateAsync<T>(Func<Task<T>> contentFactory, Func<T, IEnumerable<Dependency>> dependencyListFactory, IEnumerable<string> identifierTokens)
        {
            if (!_memoryCache.TryGetValue(StringHelpers.Join(identifierTokens), out T entry))
            {
                T response = await contentFactory();
                CreateEntry(response, dependencyListFactory, identifierTokens);

                return response;
            }

            return entry;
        }

        public void CreateEntry<T>(T response, Func<T, IEnumerable<Dependency>> dependencyListFactory, IEnumerable<string> identifierTokens)
        {
            var dependencies = dependencyListFactory(response);
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

            var mirrorDummy = _memoryCache.Set(StringHelpers.Join("dummy", identifierTokens.ElementAtOrDefault(0), identifierTokens.ElementAtOrDefault(1)), new CancellationTokenSource(), dummyOptions);
            entryOptions.AddExpirationToken(new CancellationChangeToken(mirrorDummy.Token));
            _memoryCache.Set(StringHelpers.Join(identifierTokens), response, entryOptions);
        }

        public void InvalidateEntry(Dependency identifiers)
        {
            if (_memoryCache.TryGetValue(StringHelpers.Join("dummy", identifiers.Type, identifiers.Codename), out CancellationTokenSource dummyEntry))
            {
                dummyEntry.Cancel();
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
    }
}
