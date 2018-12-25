using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TwitchLib.Webhook.Models;

namespace Markekraus.TwitchStreamNotifications
{
    [StorageAccount("TwitchStreamNotifications")]
    public static class TwitchWebhookIngestion
    {
        [FunctionName("TwitchWebhookIngestion")]
        [return: Queue("StreamNotifications")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "TwitchWebhookIngestion/{StreamName}")] StreamData WebHook,
            HttpRequest req,
            string StreamName,
            ICollector<Stream> queue,
            ILogger log)
        {
            log.LogInformation("TwitchWebhookIngestion function processed a request.");
            log.LogInformation($"Request contains {WebHook.Data.Count} objects.");

            if(!req.Headers.TryGetValue("X-Hub-Signature", out var signature))
            {
                log.LogError("Missing X-Hub-Signature header");
                return new BadRequestResult();
            }

            var fields = signature.ToString().Split("=");
            if (fields.Length != 2)
            {
                log.LogError("Malformed X-Hub-Signature header. Missing '='?");
                return new BadRequestObjectResult(signature);
            }

            var header = fields[1];
            if (string.IsNullOrEmpty(header))
            {
                log.LogError("Malformed X-Hub-Signature header. Signature is null or empty");
                return new BadRequestObjectResult(fields);
            }

            var expectedHash = Utility.FromHex(header);
            if (expectedHash == null)
            {
                log.LogError("Malformed X-Hub-Signature header. Invalid hex signature");
                return new BadRequestObjectResult("X-Hub-Signature");
            }

            var actualHash = await Utility.ComputeRequestBodySha256HashAsync(req, "supersecretpassword");

            if(!Utility.SecretEqual(expectedHash, actualHash))
            {
                log.LogError("Signature mismatch. actaulHash did not match expectedHash");
                return new BadRequestObjectResult(signature);
            }

            foreach (var item in WebHook.Data)
            {
                queue.Add(item);
            }

            return new OkResult();
        }
    }
}
