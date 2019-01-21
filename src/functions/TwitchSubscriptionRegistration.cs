using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Markekraus.TwitchStreamNotifications.Models;
using System.Collections.Generic;
using System.Linq;

namespace Markekraus.TwitchStreamNotifications
{
    [StorageAccount("TwitchStreamStorage")]
    public static class TwitchSubscriptionRegistration
    {
        [FunctionName("TwitchSubscriptionRegistration")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequest req,
            [Queue("%TwitchSubscribeQueue%", Connection = "TwitchStreamStorage")] IAsyncCollector<TwitchSubscription> SubscribeQueue,
            [Queue("%TwitchUnsubscribeQueue%", Connection = "TwitchStreamStorage")] IAsyncCollector<TwitchSubscription> UnsubscribeQueue,
            ILogger log)
        {
            log.LogInformation($"{nameof(TwitchSubscriptionRegistration)} processed a request.");

            var requestBody = await req.ReadAsStringAsync();
            log.LogInformation($"{nameof(TwitchSubscriptionRegistration)} RequestBody: {requestBody}");
            var TwitchSubscriptions = JsonConvert.DeserializeObject<IList<TwitchSubscription>>(requestBody);
            log.LogInformation($"{nameof(TwitchSubscriptionRegistration)} Count: {TwitchSubscriptions.Count}");

            var result = await TwitchClient.InvokeSubscriptionRegistration(TwitchSubscriptions, SubscribeQueue, UnsubscribeQueue, log, nameof(TwitchSubscriptionRegistration));

            log.LogInformation($"{nameof(TwitchSubscriptionRegistration)} End");
            var responseString = JsonConvert.SerializeObject(result);
            return new OkObjectResult(responseString);
        }
    }
}
