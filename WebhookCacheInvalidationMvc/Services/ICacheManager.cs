using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebhookCacheInvalidationMvc.Models;

namespace WebhookCacheInvalidationMvc.Services
{
    public interface ICacheManager
    {
        int CacheExpirySeconds { get; set; }
        List<string> CacheKeys { get; set; }

        void Dispose();
        Task<T> GetOrCreateAsync<T>(Func<Task<T>> contentFactory, Func<T, DependencyGroup> dependencyGroupFactory, IEnumerable<string> identifierTokens);
    }
}