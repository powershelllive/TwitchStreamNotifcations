using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Markekraus.TwitchStreamNotifications.Models;
using System.Threading.Tasks;

namespace Markekraus.TwitchStreamNotifications
{
    public static class TwitchSubscriptionRemove
    {
        [FunctionName("TwitchSubscriptionRemove")]
        public static async Task Run(
            [QueueTrigger("%TwitchUnsubscribeQueue%", Connection = "TwitchStreamStorage")]
            TwitchSubscription Subscription,
            ILogger log)
        {
            log.LogInformation("TwitchSubscriptionRemove Begin");

            log.LogInformation($"TwitchSubscriptionRemove Process TwitchName {Subscription.TwitchName} TwitterName {Subscription.TwitterName}");

            log.LogInformation($"TwitchSubscriptionRemove Unsubscribing TwitchName {Subscription.TwitchName} TwitterName {Subscription.TwitterName}");
            try
            {
                await TwitchClient.UnsubscribeTwitchStreamWebhook(Subscription, log);
                log.LogInformation($"TwitchSubscriptionRemove Unsubscribed TwitchName {Subscription.TwitchName} TwitterName {Subscription.TwitterName}");
            }
            catch (System.Exception e)
            {
                log.LogError(e, "TwitchSubscriptionRemove exception unsubscribing");
            }

            log.LogInformation("TwitchSubscriptionRemove End");
        }
    }
}
