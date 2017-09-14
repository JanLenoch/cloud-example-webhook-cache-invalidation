using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace WebhookCacheInvalidationMvc.Filters
{
    public class KenticoCloudSignatureActionFilter : ActionFilterAttribute
    {
        private readonly string _secret;

        public KenticoCloudSignatureActionFilter(IOptions<ProjectOptions> projectOptions) => _secret = projectOptions.Value.KenticoCloudWebhookSecret;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var signature = context.HttpContext.Request.Headers["X-KC-Signature"].FirstOrDefault();
            var content = context.HttpContext.Request.Body.ToString();
            var decryptedSecret = GenerateHash(content, _secret);

            if (decryptedSecret != _secret)
            {
                context.Result = new UnauthorizedResult();
            }
        }

        private static string GenerateHash(string message, string secret)
        {
            secret = secret ?? "";
            var SafeUTF8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            byte[] keyBytes = SafeUTF8.GetBytes(secret);
            byte[] messageBytes = SafeUTF8.GetBytes(message);

            using (var hmacsha256 = new HMACSHA256(keyBytes))
            {
                byte[] hashMessage = hmacsha256.ComputeHash(messageBytes);
                return Convert.ToBase64String(hashMessage);
            }
        }
    }
}