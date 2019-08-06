using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TwitchLib.Webhook.Models;
using Newtonsoft.Json;
using Markekraus.TwitchStreamNotifications.Models;
using Stream = TwitchLib.Webhook.Models.Stream;
using System.Text.RegularExpressions;

namespace Markekraus.TwitchStreamNotifications
{

    [StorageAccount("TwitchStreamStorage")]
    public static class TwitchWebhookIngestion
    {
        private const string SignatureHeader = "X-Hub-Signature";
        private static readonly string HashSecret = Environment.GetEnvironmentVariable("TwitchSubscriptionsHashSecret");

        [FunctionName("TwitchWebhookIngestion")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", "get", Route = "TwitchWebhookIngestion/{StreamName}/{TwitterName?}/{DiscordName?}")] HttpRequest Req,
            string StreamName,
            string TwitterName,
            string DiscordName,
            [Queue("%TwitchStreamActivity%")] ICollector<Stream> queue,
            ILogger Log)
        {
            Log.LogInformation($"TwitchWebhookIngestion function processed a request.");
            Log.LogInformation($"StreamName: {StreamName}");
            Log.LogInformation($"TwitterName: {TwitterName}");
            Log.LogInformation($"DiscordName: {DiscordName}");

            var subscription = new TwitchSubscription()
            {
                TwitchName = StreamName,
                TwitterName = TwitterName != Utility.NameNullString ? TwitterName : string.Empty,
                DiscordName = DiscordName != Utility.NameNullString ? DiscordName : string.Empty
            };

            if(Req.Query.TryGetValue("hub.mode", out var hubMode)){
                Log.LogInformation($"Received hub.mode Query string: {Req.QueryString}");
                if (hubMode.ToString().ToLower() == "subscribe" || hubMode.ToString().ToLower() == "unsubscribe")
                {
                    Log.LogInformation($"Returning hub.challenge {Req.Query["hub.challenge"]}");
                    return new OkObjectResult(Req.Query["hub.challenge"].ToString());
                }
                else
                {
                    Log.LogError($"Failed subscription: {Req.QueryString}");
                    // Subscription hub expects 200 result when subscription fails
                    return new OkResult();
                }
            }
            else
            {
                Log.LogInformation("No hub.mode supplied.");
            }

            Log.LogInformation("Processing body stream");
            Log.LogInformation($"CanSeek: {Req.Body.CanSeek}");

            var bodyString = await Req.ReadAsStringAsync();
            Log.LogInformation("Payload:");
            Log.LogInformation(bodyString);

            StreamData webhook;
            try
            {
                webhook = JsonConvert.DeserializeObject<StreamData>(bodyString);
            }
            catch (Exception e)
            {
                Log.LogError($"Invalid JSON. exception {e.Message}. {bodyString}");
                return new BadRequestResult();
            }

            Log.LogInformation($"Request contains {webhook.Data.Count} objects.");

            if(!Req.Headers.TryGetValue(SignatureHeader, out var signature))
            {
                Log.LogError($"Missing {SignatureHeader} header");
                return new BadRequestResult();
            }

            var fields = signature.ToString().Split("=");
            if (fields.Length != 2)
            {
                Log.LogError($"Malformed {SignatureHeader} header. Missing '='?");
                return new BadRequestObjectResult(signature);
            }

            var header = fields[1];
            if (string.IsNullOrEmpty(header))
            {
                Log.LogError($"Malformed {SignatureHeader} header. Signature is null or empty");
                return new BadRequestObjectResult(fields);
            }

            var expectedHash = Utility.FromHex(header);
            if (expectedHash == null)
            {
                Log.LogError($"Malformed {SignatureHeader} header. Invalid hex signature");
                return new BadRequestObjectResult(SignatureHeader);
            }

            var actualHash = await Utility.ComputeRequestBodySha256HashAsync(Req, HashSecret);

            if(!Utility.SecretEqual(expectedHash, actualHash))
            {
                Log.LogError($"Signature mismatch. actaulHash {Convert.ToBase64String(actualHash)} did not match expectedHash {Convert.ToBase64String(expectedHash)}");
                return new BadRequestObjectResult(signature);
            }

            foreach (var item in webhook.Data)
            {
                if (string.IsNullOrWhiteSpace(item.UserName))
                {
                    Log.LogInformation($"Setting missing Username to {StreamName}");
                    item.UserName = StreamName;
                }

                item.Subscription = subscription;

                var hasMatchingTitle = Regex.Match(item.Title, Utility.TwitchStreamRegexPattern, RegexOptions.IgnoreCase, Utility.RegexTimeout).Success;
                if (hasMatchingTitle)
                {
                  Log.LogInformation($"Queing notification for stream {item.UserName} type {item.Type} started at {item.StartedAt}");
                  queue.Add(item);
                }
                else
                {
                  Log.LogInformation($"Skip queing notification for stream {item.UserName} type {item.Type} started at {item.StartedAt}. Does not have matchign title.");
                }
            }

            Log.LogInformation("Processing complete");
            return new OkResult();
        }
    }
}
