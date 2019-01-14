using System;
using Markekraus.TwitchStreamNotifications.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Markekraus.TwitchStreamNotifications
{
    public static class TwitterScheduledEventNotifier
    {
        private readonly static string TwitterTweetTemplate = Environment.GetEnvironmentVariable("TwitterScheduledEventTweetTemplate");

        [FunctionName("TwitterScheduledEventNotifier")]
        public static void Run(
            [QueueTrigger("%TwitterEventNotificationsQueue%", Connection = "TwitchStreamStorage")]TwitchScheduledChannelEvent ScheduledEvent,
            ILogger log)
        {
            log.LogInformation($"TwitterScheduledEventNotifier function processed: TwitchName {ScheduledEvent.EventItem.Subscription.TwitchName} TwitterName {ScheduledEvent.EventItem.Subscription.TwitterName} EventID {ScheduledEvent.EventItem.Event.Id} NotificationType {ScheduledEvent.Type}");

            var subscription = ScheduledEvent.EventItem.Subscription;
            var channelEvent = ScheduledEvent.EventItem.Event;
            var eventType = ScheduledEvent.Type;

            if (eventType == TwitchScheduledChannelEventType.Unknown)
            { 
                log.LogInformation($"TwitterScheduledEventNotifier Processing event skipped. TwitchName {ScheduledEvent.EventItem.Subscription.TwitchName} TwitterName {ScheduledEvent.EventItem.Subscription.TwitterName} EventID {ScheduledEvent.EventItem.Event.Id} NotificationType {ScheduledEvent.Type}");
                return;
            }

            string username;
            if(string.IsNullOrWhiteSpace(subscription.TwitterName) || subscription.TwitterName == Utility.NameNullString)
            {
                username = subscription.TwitchName;
                log.LogInformation($"TwitterScheduledEventNotifier Stream username {username} will be used");
            }
            else
            {
                username = $"@{subscription.TwitterName}";
                log.LogInformation($"TwitterScheduledEventNotifier Twitter username {username} will be used");
            }

            string eventUri = $"https://www.twitch.tv/events/{channelEvent.Id}";
            log.LogInformation($"Event Uri: {eventUri}");

            string myTweet = string.Format(
                TwitterTweetTemplate,
                eventUri,
                username,
                Utility.TypeStringLookup[eventType],
                channelEvent.StartTime.ToString("u"),
                channelEvent.Title);

            TwitterClient.PublishTweet(myTweet, log);
        }
    }
}
