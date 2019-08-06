using System;
using System.Threading.Tasks;
using Markekraus.TwitchStreamNotifications.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Markekraus.TwitchStreamNotifications
{
    public static class TwitchChannelEventLookup
    {
        [FunctionName("TwitchChannelEventLookup")]
        public static async Task Run(
            [QueueTrigger("%TwitchChannelEventLookupQueue%", Connection = "TwitchStreamStorage")]TwitchSubscription Subscription,
            [Queue("%TwitchChannelEventProcessQueue%", Connection = "TwitchStreamStorage")] IAsyncCollector<TwitchChannelEventItem> EventProccessQueue,
            ILogger log)
        {
            log.LogInformation($"TwitchChannelEventLookup function processed: {Subscription.TwitchName}");

            var response = await TwitchClient.GetTwitchSubscriptionEvents(Subscription, log);

            foreach (var channelEvent in response.Events)
            {
                var hasMatchingTitle = Regex.Match(channelEvent.Title, Utility.TwitchStreamRegexPattern, RegexOptions.IgnoreCase, Utility.RegexTimeout).Success;
                if (hasMatchingTitle)
                {
                  log.LogInformation($"TwitchChannelEventLookup Queing event {channelEvent.Id} for channel {Subscription.TwitchName}. hasMatchingTitle {hasMatchingTitle}");
                  await EventProccessQueue.AddAsync(new TwitchChannelEventItem(){
                      Event = channelEvent,
                      Subscription = Subscription
                  });
                }
                else
                {
                  log.LogInformation($"TwitchChannelEventLookup Skip Queing event {channelEvent.Id} for channel {Subscription.TwitchName}. hasMatchingTitle {hasMatchingTitle}");
                }
            }

            log.LogInformation("TwitchChannelEventLookup end");
        }
    }
}
