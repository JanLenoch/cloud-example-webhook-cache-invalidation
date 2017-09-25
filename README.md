# Webhook Cache Invalidation

The example code for the [Clearing Obsolete Cache Entries with Webhooks](https://kenticocloud.com/blog/clearing-obsolete-cache-entries-with-webhooks) article in the [Kentico Cloud blog](https://kenticocloud.com/blog).

The example ASP.NET MVC Core app showcases how webhooks in Kentico Cloud can be used to invalidate content cached in web app's memory cache.

## How To Run the Code

Clone the repo and edit the "KenticoCloudProjectId" and "KenticoCloudWebhookSecret" values in the [appsettings.json](https://github.com/Kentico/cloud-example-webhook-cache-invalidation/blob/master/WebhookCacheInvalidationMvc/appsettings.json) file. The latter one should be generated in Kentico Cloud > main menu > Webhooks > Create new Webhook. In that dialog, also insert the publicly routable URL address of your app with "/webhook" at its end (e.g. http://example.com/webhook).

You may also wish to deploy the example app to your Azure subscription via the "Deploy to Azure" button below.

[![Deploy to Azure](http://azuredeploy.net/deploybutton.png)](https://azuredeploy.net/)

Once deployed, you should add the above two settings into your App Service's [Application settings](https://docs.microsoft.com/en-us/azure/app-service-web/web-sites-configure).
