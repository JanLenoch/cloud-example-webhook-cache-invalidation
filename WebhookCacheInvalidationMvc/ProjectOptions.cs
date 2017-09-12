namespace WebhookCacheInvalidationMvc
{
    public class ProjectOptions
    {
        public string KenticoCloudProjectId { get; set; }
        public string KenticoCloudPreviewApiKey { get; set; }
        public int CacheTimeoutSeconds { get; set; }
        public string KenticoCloudWebhookSecret { get; set; }
    }
}
