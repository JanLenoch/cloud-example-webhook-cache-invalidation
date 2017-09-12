using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebhookCacheInvalidationMvc.Models;

namespace WebhookCacheInvalidationMvc.Services
{
    public interface ICacheManager : IDisposable
    {
        int CacheExpirySeconds { get; set; }
        Task<T> GetOrCreateAsync<T>(Func<Task<T>> contentFactory, Func<T, IEnumerable<Dependency>> dependencyListFactory, IEnumerable<string> identifierTokens);
        void InvalidateEntry(Dependency identifiers);
    }
}