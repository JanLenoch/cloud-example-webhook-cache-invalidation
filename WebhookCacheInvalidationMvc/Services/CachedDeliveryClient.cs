using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KenticoCloud.Delivery;
using KenticoCloud.Delivery.InlineContentItems;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;

using WebhookCacheInvalidationMvc.Helpers;
using WebhookCacheInvalidationMvc.Models;

namespace WebhookCacheInvalidationMvc.Services
{
    public class CachedDeliveryClient : IDeliveryClient, IDisposable
    {
        #region "Fields"

        private bool _disposed = false;
        protected readonly IMemoryCache _cache;
        protected readonly DeliveryClient _deliveryClient;
        protected readonly ICacheManager _cacheManager;

        #endregion

        #region "Properties"

        public int CacheExpirySeconds
        {
            get;
            set;
        }

        public IContentLinkUrlResolver ContentLinkUrlResolver { get => _deliveryClient.ContentLinkUrlResolver; set => _deliveryClient.ContentLinkUrlResolver = value; }
        public ICodeFirstModelProvider CodeFirstModelProvider { get => _deliveryClient.CodeFirstModelProvider; set => _deliveryClient.CodeFirstModelProvider = value; }
        public InlineContentItemsProcessor InlineContentItemsProcessor => _deliveryClient.InlineContentItemsProcessor;

        #endregion

        #region "Constructors"

        public CachedDeliveryClient(IOptions<ProjectOptions> projectOptions, ICacheManager cacheManager)
        {
            if (string.IsNullOrEmpty(projectOptions.Value.KenticoCloudPreviewApiKey))
            {
                _deliveryClient = new DeliveryClient(projectOptions.Value.KenticoCloudProjectId);
            }
            else
            {
                _deliveryClient = new DeliveryClient(
                    projectOptions.Value.KenticoCloudProjectId,
                    projectOptions.Value.KenticoCloudPreviewApiKey
                );
            }

            CacheExpirySeconds = projectOptions.Value.CacheTimeoutSeconds;
            _cacheManager = cacheManager;
        }

        #endregion

        #region "Public methods"

        /// <summary>
        /// Returns a content item as JSON data.
        /// </summary>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">An array that contains zero or more query parameters, for example for projection or depth of modular content.</param>
        /// <returns>The <see cref="JObject"/> instance that represents the content item with the specified codename.</returns>
        public async Task<JObject> GetItemJsonAsync(string codename, params string[] parameters)
        {
            string cacheKey = $"{nameof(GetItemJsonAsync)}|{codename}|{Join(parameters)}";

            return await GetOrCreateAsync(cacheKey, () => _deliveryClient.GetItemJsonAsync(codename, parameters));
        }

        /// <summary>
        /// Returns content items as JSON data.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example for filtering, ordering or depth of modular content.</param>
        /// <returns>The <see cref="JObject"/> instance that represents the content items. If no query parameters are specified, all content items are returned.</returns>
        public async Task<JObject> GetItemsJsonAsync(params string[] parameters)
        {
            string cacheKey = $"{nameof(GetItemsJsonAsync)}|{Join(parameters)}";

            return await GetOrCreateAsync(cacheKey, () => _deliveryClient.GetItemsJsonAsync(parameters));
        }

        /// <summary>
        /// Returns a content item.
        /// </summary>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">An array that contains zero or more query parameters, for example for projection or depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemResponse"/> instance that contains the content item with the specified codename.</returns>
        public async Task<DeliveryItemResponse> GetItemAsync(string codename, params IQueryParameter[] parameters)
        {
            return await GetItemAsync(codename, (IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Gets one strongly typed content item by its codename.
        /// </summary>
        /// <typeparam name="T">Type of the code-first model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">An array that contains zero or more query parameters, for example for projection or depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemResponse{T}"/> instance that contains the content item with the specified codename.</returns>
        public async Task<DeliveryItemResponse<T>> GetItemAsync<T>(string codename, params IQueryParameter[] parameters)
        {
            return await GetItemAsync<T>(codename, (IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Returns a content item.
        /// </summary>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">A collection of query parameters, for example for projection or depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemResponse"/> instance that contains the content item with the specified codename.</returns>
        public async Task<DeliveryItemResponse> GetItemAsync(string codename, IEnumerable<IQueryParameter> parameters)
        {
            var identifierTokens = new List<string> { CacheHelper.CONTENT_ITEM_TYPE_CODENAME, codename };
            AddIdentifiersFromParameters(parameters, identifierTokens);

            return await _cacheManager.GetOrCreateAsync(() => _deliveryClient.GetItemAsync(codename, parameters), GetDependencies, identifierTokens);
        }

        /// <summary>
        /// Gets one strongly typed content item by its codename.
        /// </summary>
        /// <typeparam name="T">Type of the code-first model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">A collection of query parameters, for example for projection or depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemResponse{T}"/> instance that contains the content item with the specified codename.</returns>
        public async Task<DeliveryItemResponse<T>> GetItemAsync<T>(string codename, IEnumerable<IQueryParameter> parameters)
        {
            var identifierTokens = new List<string> { string.Join(string.Empty, CacheHelper.CONTENT_ITEM_TYPE_CODENAME, "_typed"), codename };
            AddIdentifiersFromParameters(parameters, identifierTokens);

            return await _cacheManager.GetOrCreateAsync(() => _deliveryClient.GetItemAsync<T>(codename, parameters), GetDependencies, identifierTokens);
        }

        /// <summary>
        /// Searches the content repository for items that match the filter criteria.
        /// Returns content items.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example for filtering, ordering or depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemListingResponse"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        public async Task<DeliveryItemListingResponse> GetItemsAsync(params IQueryParameter[] parameters)
        {
            return await GetItemsAsync((IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Returns content items.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example for filtering, ordering or depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemListingResponse"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        public async Task<DeliveryItemListingResponse> GetItemsAsync(IEnumerable<IQueryParameter> parameters)
        {
            var identifierTokens = new List<string> { CacheHelper.CONTENT_ITEM_LISTING_IDENTIFIER };
            AddIdentifiersFromParameters(parameters, identifierTokens);

            return await _cacheManager.GetOrCreateAsync(() => _deliveryClient.GetItemsAsync(parameters), GetDependencies, identifierTokens);
        }

        /// <summary>
        /// Searches the content repository for items that match the filter criteria.
        /// Returns strongly typed content items.
        /// </summary>
        /// <typeparam name="T">Type of the code-first model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="parameters">An array that contains zero or more query parameters, for example for filtering, ordering or depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemListingResponse{T}"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        public async Task<DeliveryItemListingResponse<T>> GetItemsAsync<T>(params IQueryParameter[] parameters)
        {
            return await GetItemsAsync<T>((IEnumerable<IQueryParameter>)parameters);
        }

        public async Task<DeliveryItemListingResponse<T>> GetItemsAsync<T>(IEnumerable<IQueryParameter> parameters)
        {
            var identifierTokens = new List<string> { string.Join(string.Empty, CacheHelper.CONTENT_ITEM_LISTING_IDENTIFIER, "_typed") };
            AddIdentifiersFromParameters(parameters, identifierTokens);

            return await _cacheManager.GetOrCreateAsync(() => _deliveryClient.GetItemsAsync<T>(parameters), GetDependencies, identifierTokens);
        }

        /// <summary>
        /// Returns a content type as JSON data.
        /// </summary>
        /// <param name="codename">The codename of a content type.</param>
        /// <returns>The <see cref="JObject"/> instance that represents the content type with the specified codename.</returns>
        public async Task<JObject> GetTypeJsonAsync(string codename)
        {
            string cacheKey = $"{nameof(GetTypeJsonAsync)}|{codename}";

            return await GetOrCreateAsync(cacheKey, () => _deliveryClient.GetTypeJsonAsync(codename));
        }

        /// <summary>
        /// Returns content types as JSON data.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example for paging.</param>
        /// <returns>The <see cref="JObject"/> instance that represents the content types. If no query parameters are specified, all content types are returned.</returns>
        public async Task<JObject> GetTypesJsonAsync(params string[] parameters)
        {
            string cacheKey = $"{nameof(GetTypesJsonAsync)}|{Join(parameters)}";

            return await GetOrCreateAsync(cacheKey, () => _deliveryClient.GetTypesJsonAsync(parameters));
        }

        /// <summary>
        /// Returns a content type.
        /// </summary>
        /// <param name="codename">The codename of a content type.</param>
        /// <returns>The content type with the specified codename.</returns>
        public async Task<ContentType> GetTypeAsync(string codename)
        {
            string cacheKey = $"{nameof(GetTypeAsync)}|{codename}";

            return await GetOrCreateAsync(cacheKey, () => _deliveryClient.GetTypeAsync(codename));
        }

        /// <summary>
        /// Returns content types.
        /// </summary>
        /// <param name="parameters">An array that contains zero or more query parameters, for example for paging.</param>
        /// <returns>The <see cref="DeliveryTypeListingResponse"/> instance that represents the content types. If no query parameters are specified, all content types are returned.</returns>
        public async Task<DeliveryTypeListingResponse> GetTypesAsync(params IQueryParameter[] parameters)
        {
            return await GetTypesAsync((IEnumerable<IQueryParameter>)parameters);
        }

        /// <summary>
        /// Returns content types.
        /// </summary>
        /// <param name="parameters">A collection of query parameters, for example for paging.</param>
        /// <returns>The <see cref="DeliveryTypeListingResponse"/> instance that represents the content types. If no query parameters are specified, all content types are returned.</returns>
        public async Task<DeliveryTypeListingResponse> GetTypesAsync(IEnumerable<IQueryParameter> parameters)
        {
            string cacheKey = $"{nameof(GetTypesAsync)}|{Join(parameters?.Select(p => p.GetQueryStringParameter()).ToList())}";

            return await GetOrCreateAsync(cacheKey, () => _deliveryClient.GetTypesAsync(parameters));
        }

        /// <summary>
        /// Returns a content element.
        /// </summary>
        /// <param name="contentTypeCodename">The codename of the content type.</param>
        /// <param name="contentElementCodename">The codename of the content element.</param>
        /// <returns>A content element with the specified codename that is a part of a content type with the specified codename.</returns>
        public async Task<ContentElement> GetContentElementAsync(string contentTypeCodename, string contentElementCodename)
        {
            string cacheKey = $"{nameof(GetContentElementAsync)}|{contentTypeCodename}|{contentElementCodename}";

            return await GetOrCreateAsync(cacheKey, () => _deliveryClient.GetContentElementAsync(contentTypeCodename, contentElementCodename));
        }

        public static IEnumerable<IdentifierSet> GetDependencies<T>(T response)
        {
            var dependencies = new List<IdentifierSet>();
            AddModularContentDependencies(response, dependencies);

            if (response is DeliveryItemResponse || response is DeliveryItemResponse<T>)
            {
                var ownDependency = new IdentifierSet
                {
                    Type = CacheHelper.CONTENT_ITEM_TYPE_CODENAME,
                    Codename = GetContentItemCodenameFromResponse(response)
                };

                if (!dependencies.Contains(ownDependency, new IdentifierSetEqualityComparer()))
                {
                    dependencies.Add(ownDependency);
                }
            }
            else if (response is DeliveryItemListingResponse || response is DeliveryItemListingResponse<T>)
            {
                foreach (var codename in GetContentItemCodenamesFromListingResponse(response))
                {
                    var dependency = new IdentifierSet
                    {
                        Type = CacheHelper.CONTENT_ITEM_TYPE_CODENAME,
                        Codename = codename
                    };

                    if (!dependencies.Contains(dependency, new IdentifierSetEqualityComparer()))
                    {
                        dependencies.Add(dependency);
                    }
                }
            }

            return dependencies;
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

        #region "Helper methods"

        protected string Join(IEnumerable<string> parameters)
        {
            return parameters != null ? string.Join("|", parameters) : string.Empty;
        }

        protected async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory)
        {
            var result = _cache.GetOrCreateAsync(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(CacheExpirySeconds);
                return factory.Invoke();
            });

            return await result;
        }

        private static string GetContentItemCodenameFromResponse(dynamic response)
        {
            if (response.Item?.System?.Codename != null)
            {
                try
                {
                    return response.Item.System.Codename;
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        private static IEnumerable<string> GetContentItemCodenamesFromListingResponse(dynamic response)
        {
            if (response?.Items != null)
            {
                var codenames = new List<string>();

                foreach (dynamic item in response.Items)
                {
                    try
                    {
                        codenames.Add(item.System?.Codename);
                    }
                    catch
                    {
                        return null;
                    }
                }

                return codenames;
            }

            return null;
        }

        private static void AddModularContentDependencies<T>(T response, List<IdentifierSet> dependencies)
        {
            foreach (var codename in GetModularContentCodenames(response))
            {
                dependencies.Add(new IdentifierSet
                {
                    Type = CacheHelper.CONTENT_ITEM_TYPE_CODENAME,
                    Codename = codename
                });
            }
        }

        private static IEnumerable<string> GetModularContentCodenames(dynamic response)
        {
            if (response.ModularContent != null)
            {
                foreach (var mc in response.ModularContent)
                {
                    yield return mc.Path;
                }
            }
        }

        private static void AddIdentifiersFromParameters(IEnumerable<IQueryParameter> parameters, List<string> identifierTokens)
        {
            if (parameters != null)
            {
                identifierTokens.AddRange(parameters?.Select(p => p.GetQueryStringParameter()));
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _cache.Dispose();
            }

            _disposed = true;
        }

        #endregion
    }
}
