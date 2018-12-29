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
            [Queue("%TwitchSubscribeQueue%")] IAsyncCollector<TwitchSubscription> NewSubscriptions,
            [Queue("%TwitchUnsubscribeQueue%")] IAsyncCollector<TwitchSubscription> RemoveSubscriptions,
            ILogger log)
        {
            log.LogInformation("TwitchSubscriptionRegistration processed a request.");

            var requestBody = await req.ReadAsStringAsync();
            log.LogInformation("RequestBody:");
            log.LogInformation(requestBody);
            var TwitchSubscriptions = JsonConvert.DeserializeObject<IList<TwitchSubscription>>(requestBody);
            log.LogInformation($"Count: {TwitchSubscriptions.Count}");

            var response = new TwitchSubscriptionRegistrationResponse();
            response.RequestSubscriptions = TwitchSubscriptions;

            log.LogInformation("Get current subscriptions");
            var currentSubscriptions = await TwitchClient.GetTwitchWebhookSubscriptions(log);
            response.CurrentSubscriptions = currentSubscriptions;

            log.LogInformation("Create currentSubDictionary dictionary");
            var currentSubDictionary = new Dictionary<string,TwitchSubscription>();
            foreach(var subscription in currentSubscriptions)
            {
                log.LogInformation($"Add TwitchName {subscription.Subscription.TwitchName} TwitterName {subscription.Subscription.TwitterName}");
                currentSubDictionary.Add(subscription.Subscription.TwitchName, subscription.Subscription);
            }

            log.LogInformation("Create requestedSubDictionary dictionary");
            var requestedSubDictionary = new Dictionary<string,TwitchSubscription>();
            foreach(var subscription in TwitchSubscriptions)
            {
                log.LogInformation($"Add TwitchName {subscription.TwitchName} TwitterName {subscription.TwitterName}");
                requestedSubDictionary.Add(subscription.TwitchName, subscription);
            }

            log.LogInformation("Find missing subscriptions to add");
            response.AddSubscriptions = new List<TwitchSubscription>();
            foreach(var missing in Enumerable.Except(requestedSubDictionary.Keys, currentSubDictionary.Keys, StringComparer.InvariantCultureIgnoreCase))
            {
                log.LogInformation($"Add Queue TwitchName {requestedSubDictionary[missing].TwitchName} TwitterName {requestedSubDictionary[missing].TwitterName}");
                await NewSubscriptions.AddAsync(requestedSubDictionary[missing]);
                response.AddSubscriptions.Add(requestedSubDictionary[missing]);
            }

            log.LogInformation("Find extra subscriptions to remove");
            response.RemoveSubscriptions = new List<TwitchSubscription>();
            foreach(var extra in Enumerable.Except(currentSubDictionary.Keys, requestedSubDictionary.Keys, StringComparer.InvariantCultureIgnoreCase))
            {
                log.LogInformation($"Remove Queue TwitchName {currentSubDictionary[extra].TwitchName} TwitterName {currentSubDictionary[extra].TwitterName}");
                await RemoveSubscriptions.AddAsync(currentSubDictionary[extra]);
                response.RemoveSubscriptions.Add(currentSubDictionary[extra]);

            }

            log.LogInformation("TwitchSubscriptionRegistration End");
            var responseString = JsonConvert.SerializeObject(response);
            return new OkObjectResult(responseString);
        }
    }
}
