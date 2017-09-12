using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KenticoCloud.Delivery;
using KenticoCloud.Delivery.InlineContentItems;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;

using System.Reflection;
using System.Threading;
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
            var identifierTokens = new List<string>();
            identifierTokens.Add(CacheHelper.CONTENT_ITEM_TYPE_CODENAME);
            identifierTokens.Add(codename);
            identifierTokens.AddRange(parameters?.Select(p => p.GetQueryStringParameter()));

            return await _cacheManager.GetOrCreateAsync(() => _deliveryClient.GetItemAsync(codename, parameters), GetContentItemOrListingResponseDependencies, identifierTokens);
        }

        /// <summary>
        /// Gets one strongly typed content item by its codename.
        /// </summary>
        /// <typeparam name="T">Type of the code-first model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="codename">The codename of a content item.</param>
        /// <param name="parameters">A collection of query parameters, for example for projection or depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemResponse{T}"/> instance that contains the content item with the specified codename.</returns>
        public async Task<DeliveryItemResponse<T>> GetItemAsync<T>(string codename, IEnumerable<IQueryParameter> parameters = null)
        {
            string cacheKey = $"{nameof(GetItemAsync)}-{typeof(T).FullName}|{codename}|{Join(parameters?.Select(p => p.GetQueryStringParameter()).ToList())}";

            return await GetOrCreateAsync(cacheKey, () => _deliveryClient.GetItemAsync<T>(codename, parameters));
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
            var identifierTokens = new List<string>();
            identifierTokens.Add(CacheHelper.CONTENT_ITEM_LISTING_IDENTIFIER);
            identifierTokens.AddRange(parameters?.Select(p => p.GetQueryStringParameter()));

            return await _cacheManager.GetOrCreateAsync(() => _deliveryClient.GetItemsAsync(parameters), GetContentItemOrListingResponseDependencies, identifierTokens);
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

        /// <summary>
        /// Searches the content repository for items that match the filter criteria.
        /// Returns strongly typed content items.
        /// </summary>
        /// <typeparam name="T">Type of the code-first model. (Or <see cref="object"/> if the return type is not yet known.)</typeparam>
        /// <param name="parameters">A collection of query parameters, for example for filtering, ordering or depth of modular content.</param>
        /// <returns>The <see cref="DeliveryItemListingResponse{T}"/> instance that contains the content items. If no query parameters are specified, all content items are returned.</returns>
        public async Task<DeliveryItemListingResponse<T>> GetItemsAsync<T>(IEnumerable<IQueryParameter> parameters)
        {
            string cacheKey = $"{nameof(GetItemsAsync)}-{typeof(T).FullName}|{Join(parameters?.Select(p => p.GetQueryStringParameter()).ToList())}";

            return await GetOrCreateAsync(cacheKey, () => _deliveryClient.GetItemsAsync<T>(parameters));
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

        public DependencyGroup CreateItemOrListingDependencyGroup<T>(T response)
        {
            if (response is DeliveryItemResponse || response is DeliveryItemListingResponse)
            {
                IEnumerable<string> modularContentCodenames = null;

                if (response is DeliveryItemResponse)
                {
                    modularContentCodenames = GetCodenamesFromResponse(response).modularContentCodenames;
                }

                if (response is DeliveryItemListingResponse)
                {
                    modularContentCodenames = GetCodenamesFromListingResponse(response).modularContentCodenames;
                }

                return new DependencyGroup
                {
                    EvictingArtifacts = modularContentCodenames.Select(mc => new Dependency
                    {
                        Type = CacheHelper.CONTENT_ITEM_TYPE_CODENAME,
                        Codename = mc
                    }).ToList(),
                    CancellationTokenSource = new CancellationTokenSource()
                };
            }
            else
            {
                // HACK Make the SDK classes implement an interface and use the "where" clause here.
                throw new ArgumentOutOfRangeException(nameof(response), $"The {nameof(response)} parameter must be of either the DeliveryItemResponse or the DeliveryItemListingResponse type.");
            }
        }

        public static (string itemCodename, IEnumerable<string> modularContentCodenames) GetCodenamesFromResponse<T>(T response)
        {
            var item = response.GetType().GetTypeInfo().GetProperty("Item", typeof(ContentItem)).GetValue(response) as ContentItem;

            return (item.System.Codename, GetModularContentCodenames(response));
        }

        public static (IEnumerable<string> itemCodenames, IEnumerable<string> modularContentCodenames) GetCodenamesFromListingResponse(object response)
        {
            var items = response.GetType().GetTypeInfo().GetProperty("Items", typeof(IReadOnlyList<ContentItem>)).GetValue(response) as IReadOnlyList<ContentItem>;

            return (items.Select(i => i.System.Codename), GetModularContentCodenames(response));
        }

        public static IEnumerable<string> GetModularContentCodenames(dynamic response)
        {
            var codenames = new List<string>();

            foreach (var mc in response.ModularContent)
            {
                codenames.Add(mc.Path);
            }

            return codenames;
        }

        public static IEnumerable<Dependency> GetContentItemOrListingResponseDependencies<T>(T response)
        {
            if (response is DeliveryItemResponse || response is DeliveryItemListingResponse)
            {
                var dependencies = new List<Dependency>();

                AddModularContentDependencies(response, dependencies);

                if (response is DeliveryItemListingResponse)
                {
                    foreach (var codename in GetContentItemCodenamesFromListingResponse(response))
                    {
                        dependencies.Add(new Dependency
                        {
                            Type = CacheHelper.CONTENT_ITEM_TYPE_CODENAME,
                            Codename = codename
                        });
                    }
                }

                return dependencies;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(response));
            }
        }

        private static void AddModularContentDependencies<T>(T response, List<Dependency> dependencies)
        {
            // TODO Refactor foreach to LINQ
            foreach (var codename in GetModularContentCodenames(response))
            {
                dependencies.Add(new Dependency
                {
                    Type = CacheHelper.CONTENT_ITEM_TYPE_CODENAME,
                    Codename = codename
                });
            }
        }

        public static IEnumerable<Dependency> GetContentItemTypedResponseDependencies<T>(DeliveryItemResponse<T> response)
            where T : ContentItemBase
        {
            var dependencies = new List<Dependency>();
            AddModularContentDependencies(response, dependencies);

            return dependencies;
        }

        public static IEnumerable<Dependency> GetContentItemListingTypedResponseDependencies<T>(DeliveryItemListingResponse<T> response)
            where T : ContentItemBase
        {
            var dependencies = new List<Dependency>();
            AddModularContentDependencies(response, dependencies);

            foreach (var item in response.Items)
            {
                dependencies.Add(new Dependency
                {
                    Type = CacheHelper.CONTENT_ITEM_TYPE_CODENAME,
                    Codename = item.System.Codename
                });
            }

            return dependencies;
        }

        public static IEnumerable<string> GetContentItemCodenamesFromListingResponse<T>(T response)
        {
            var items = response.GetType().GetTypeInfo().GetProperty("Items", typeof(IReadOnlyList<ContentItem>)).GetValue(response) as IReadOnlyList<ContentItem>;

            return items.Select(i => i.System.Codename);
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
            var result = _cache.GetOrCreateAsync<T>(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(CacheExpirySeconds);
                return factory.Invoke();
            });

            return await result;
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
