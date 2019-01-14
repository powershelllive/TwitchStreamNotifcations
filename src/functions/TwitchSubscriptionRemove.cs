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

            log.LogInformation($"Process TwitchName {Subscription.TwitchName} TwitterName {Subscription.TwitterName}");

            log.LogInformation($"Unsubscribing TwitchName {Subscription.TwitchName} TwitterName {Subscription.TwitterName}");
            try
            {
                await TwitchClient.UnsubscribeTwitchStreamWebhook(Subscription, log);
                log.LogInformation($"Unsubscribed TwitchName {Subscription.TwitchName} TwitterName {Subscription.TwitterName}");
            } catch (System.Exception e)
            {
                log.LogError(e, "exception unsubscribing");
            }

            log.LogInformation("TwitchSubscriptionRemove End");
        }
    }
}
