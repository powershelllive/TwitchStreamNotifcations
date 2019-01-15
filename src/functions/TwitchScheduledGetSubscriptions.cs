using System;
using System.Threading.Tasks;
using Markekraus.TwitchStreamNotifications.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Markekraus.TwitchStreamNotifications
{
    public static class TwitchScheduledGetSubscriptions
    {
        [FunctionName("TwitchScheduledGetSubscriptions")]
        public static async Task Run(
            [TimerTrigger("0 0 * * * *")]TimerInfo myTimer,
            [Queue("%TwitchChannelEventLookupQueue%", Connection = "TwitchStreamStorage")] IAsyncCollector<TwitchSubscription> EventLookupQueue,
            ILogger log)
        {
            log.LogInformation($"TwitchScheduledGetSubscriptions function executed at: {DateTime.Now}");
            log.LogInformation("TwitchScheduledGetSubscriptions Get current subscriptions");
            var currentSubscriptions = await TwitchClient.GetTwitchWebhookSubscriptions(log);
            foreach (var currentSubscription in currentSubscriptions)
            {
                log.LogInformation($"TwitchScheduledGetSubscriptions Queuing TwitchName {currentSubscription.Subscription.TwitchName} TwitterName {currentSubscription.Subscription.TwitterName} DiscordName {currentSubscription.Subscription.DiscordName}");
                await EventLookupQueue.AddAsync(currentSubscription.Subscription);
            }

            log.LogInformation("TwitchScheduledGetSubscriptions end");
        }
    }
}
