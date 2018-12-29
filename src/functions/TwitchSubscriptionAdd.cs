using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Markekraus.TwitchStreamNotifications.Models;
using System.Threading.Tasks;

namespace Markekraus.TwitchStreamNotifications
{
    public static class TwitchSubscriptionAdd
    {
        [FunctionName("TwitchSubscriptionAdd")]
        public static async Task Run(
            [QueueTrigger("%TwitchSubscribeQueue%", Connection = "TwitchStreamStorage")]
            TwitchSubscription Subscription, 
            ILogger log)
        {
            log.LogInformation("TwitchSubscriptionAdd Begin");

            log.LogInformation($"Process TwitchName {Subscription.TwitchName} TwitterName {Subscription.TwitterName}");
            await TwitchClient.SubscribeTwitchStreamWebhook(Subscription, log);
            log.LogInformation("TwitchSubscriptionAdd End");
        }
    }
}
