using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Markekraus.TwitchStreamNotifications.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Markekraus.TwitchStreamNotifications
{
    public static class TwitchScheduledSubscriptionRegistration
    {
        [FunctionName("TwitchScheduledSubscriptionRegistration")]
        public static async Task Run(
            [TimerTrigger("0 0 * * * *")]TimerInfo myTimer,
            [Blob("%TwitchSubscriptionBlob%", Connection = "TwitchStreamStorage")] string SubscriptionJsonContent,
            [Queue("%TwitchSubscribeQueue%", Connection = "TwitchStreamStorage")] IAsyncCollector<TwitchSubscription> SubscribeQueue,
            [Queue("%TwitchUnsubscribeQueue%", Connection = "TwitchStreamStorage")] IAsyncCollector<TwitchSubscription> UnsubscribeQueue,
            ILogger log)
        {
            log.LogInformation($"{nameof(TwitchScheduledSubscriptionRegistration)} function executed at: {DateTime.Now}");
            log.LogInformation($"{nameof(TwitchScheduledSubscriptionRegistration)} SubscriptionJsonContent: {SubscriptionJsonContent}");
            var TwitchSubscriptions = JsonConvert.DeserializeObject<IList<TwitchSubscription>>(SubscriptionJsonContent);
            log.LogInformation($"Count: {TwitchSubscriptions.Count}");
            var result = await TwitchClient.InvokeSubscriptionRegistration(TwitchSubscriptions, SubscribeQueue, UnsubscribeQueue, log, nameof(TwitchScheduledSubscriptionRegistration));
            var resultString = JsonConvert.SerializeObject(result);
            log.LogInformation($"{nameof(TwitchScheduledSubscriptionRegistration)} Result: {resultString}");
            log.LogInformation($"{nameof(TwitchScheduledSubscriptionRegistration)} End");
        }
    }
}
