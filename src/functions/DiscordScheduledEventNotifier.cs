using System;
using System.Threading.Tasks;
using Markekraus.TwitchStreamNotifications.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Markekraus.TwitchStreamNotifications
{
    public static class DiscordScheduledEventNotifier
    {
        private readonly static string DiscordMessageTemplate = Environment.GetEnvironmentVariable("DiscordScheduledEventMessageTemplate");

        [FunctionName("DiscordScheduledEventNotifier")]
        public static async Task Run(
            [QueueTrigger("%DiscordEventNotificationsQueue%", Connection = "TwitchStreamStorage")]TwitchScheduledChannelEvent ScheduledEvent,
            ILogger log)
        {
            log.LogInformation($"DiscordScheduledEventNotifier function processed: TwitchName {ScheduledEvent.EventItem.Subscription.TwitchName} DiscordName {ScheduledEvent.EventItem.Subscription.DiscordName} EventID {ScheduledEvent.EventItem.Event.Id} NotificationType {ScheduledEvent.Type}");

            var subscription = ScheduledEvent.EventItem.Subscription;
            var channelEvent = ScheduledEvent.EventItem.Event;
            var eventType = ScheduledEvent.Type;

            if (eventType == TwitchScheduledChannelEventType.Unknown)
            { 
                log.LogInformation($"DiscordScheduledEventNotifier Processing event skipped. TwitchName {ScheduledEvent.EventItem.Subscription.TwitchName} DiscordName {ScheduledEvent.EventItem.Subscription.DiscordName} EventID {ScheduledEvent.EventItem.Event.Id} NotificationType {ScheduledEvent.Type}");
                return;
            }

            string username;
            if(string.IsNullOrWhiteSpace(subscription.DiscordName) || subscription.DiscordName == Utility.NameNullString)
            {
                username = subscription.TwitchName;
                log.LogInformation($"DiscordScheduledEventNotifier Stream username {username} will be used");
            }
            else
            {
                username = $"<@{subscription.DiscordName}>";
                log.LogInformation($"DiscordScheduledEventNotifier Discord username {username} will be used");
            }

            string eventUri = $"https://www.twitch.tv/events/{channelEvent.Id}";
            log.LogInformation($"DiscordScheduledEventNotifier Event Uri: {eventUri}");

            var myDiscordMessage = new DiscordMessage(){
                Content = string.Format(
                    DiscordMessageTemplate,
                    eventUri,
                    username,
                    Utility.TypeStringLookup[eventType],
                    channelEvent.StartTime.ToString("u"),
                    channelEvent.Title)
            };

            await DiscordClient.SendDiscordMessageAsync(myDiscordMessage, log);
        }
    }
}
