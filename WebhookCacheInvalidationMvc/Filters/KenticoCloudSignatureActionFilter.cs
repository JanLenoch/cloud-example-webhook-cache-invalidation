using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace WebhookCacheInvalidationMvc.Filters
{
    public class KenticoCloudSignatureActionFilter : ActionFilterAttribute
    {
        private readonly string _secret;

        public KenticoCloudSignatureActionFilter(IOptions<ProjectOptions> projectOptions) => _secret = projectOptions.Value.KenticoCloudWebhookSecret;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var signature = context.HttpContext.Request.Headers["X-KC-Signature"].FirstOrDefault();
            var request = context.HttpContext.Request;
            string content = null;

            using (var reader = new StreamReader(request.Body, Encoding.UTF8, true, 1024, true))
            {
                request.Body.Position = 0;
                content = reader.ReadToEnd();
                request.Body.Position = 0;
                var generatedSignature = GenerateHash(content, _secret);

                if (generatedSignature != signature)
                {
                    context.Result = new UnauthorizedResult();
                }
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