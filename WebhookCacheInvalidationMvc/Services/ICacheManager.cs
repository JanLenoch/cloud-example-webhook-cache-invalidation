using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebhookCacheInvalidationMvc.Models;

namespace WebhookCacheInvalidationMvc.Services
{
    public interface ICacheManager : IDisposable
    {
        int CacheExpirySeconds { get; set; }
        List<string> CacheKeys { get; set; }

        //Task<T> GetOrCreateAsync<T>(Func<Task<T>> contentFactory, Func<T, DependencyGroup> dependencyGroupFactory, IEnumerable<string> identifierTokens);
        Task<T> GetOrCreateAsync<T>(Func<Task<T>> contentFactory, Func<T, IEnumerable<EvictingArtifact>> dependencyListFactory, IEnumerable<string> identifierTokens);
        void InvalidateEntry(EvictingArtifact identifiers);
    }
}