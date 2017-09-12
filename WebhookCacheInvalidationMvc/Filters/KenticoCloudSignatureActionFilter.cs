using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;

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

            //if (decryptedSecret != _secret)
            //{
            //    context.Result = new UnauthorizedResult();
            //}
        }

        private static string GenerateHash(string message, string secret)
        {
            secret = secret ?? "";
            var encoding = new System.Text.UTF8Encoding();
            byte[] keyByte = encoding.GetBytes(secret);
            byte[] messageBytes = encoding.GetBytes(message);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                return Convert.ToBase64String(hashmessage);
            }
        }
    }
}
