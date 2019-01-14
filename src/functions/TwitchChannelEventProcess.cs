using System;
using System.Threading.Tasks;
using Markekraus.TwitchStreamNotifications.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Markekraus.TwitchStreamNotifications
{
    public static class TwitchChannelEventProcess
    {
        [FunctionName("TwitchChannelEventProcess")]
        public static async Task Run([QueueTrigger("%TwitchChannelEventProcessQueue%", Connection = "TwitchStreamStorage")]TwitchChannelEventItem ChannelEventItem,
        [Queue("%DiscordEventNotificationsQueue%")] IAsyncCollector<TwitchScheduledChannelEvent> DiscordQueue,
        [Queue("%TwitterEventNotificationsQueue%")] IAsyncCollector<TwitchScheduledChannelEvent> TwitterQueue,
        ILogger log)
        {
            log.LogInformation($"TwitchChannelEventProcess function processed: {ChannelEventItem.Event.Id}");

            var channelEvent = ChannelEventItem.Event;

            var now = DateTime.UtcNow;
            var hourStart = new DateTime(now.Year, now.Month, now.Day, now.Hour+1, 0, 0, DateTimeKind.Utc);
            var hourEnd = new DateTime(now.Year, now.Month, now.Day, now.Hour+1, 59, 59, DateTimeKind.Utc);
            var weekStart = new DateTime(now.Year, now.Month, now.Day+7, now.Hour, 0, 0, DateTimeKind.Utc);
            var weekEnd = new DateTime(now.Year, now.Month, now.Day+7, now.Hour, 59, 59, DateTimeKind.Utc);
            var dayStart = new DateTime(now.Year, now.Month, now.Day+1, now.Hour, 0, 0, DateTimeKind.Utc);
            var dayEnd = new DateTime(now.Year, now.Month, now.Day+1, now.Hour, 59, 59, DateTimeKind.Utc);

            log.LogInformation($"TwitchChannelEventProcess Now {now}");
            log.LogInformation($"TwitchChannelEventProcess HourStart {hourStart}");
            log.LogInformation($"TwitchChannelEventProcess HourEnd {hourEnd}");
            log.LogInformation($"TwitchChannelEventProcess DayStart {dayStart}");
            log.LogInformation($"TwitchChannelEventProcess DayEnd {dayEnd}");
            log.LogInformation($"TwitchChannelEventProcess WeekStart {weekStart}");
            log.LogInformation($"TwitchChannelEventProcess WeekEnd {weekEnd}");

            var scheduledEvent = new TwitchScheduledChannelEvent(ChannelEventItem);

            if(channelEvent.StartTime >= hourStart && channelEvent.StartTime <= hourEnd)
            {
                log.LogInformation($"TwitchChannelEventProcess TwitchName {ChannelEventItem.Subscription.TwitchName} EventId {ChannelEventItem.Event.Id} in an hour");
                scheduledEvent.Type = TwitchScheduledChannelEventType.Hour;
            }
            else if (channelEvent.StartTime >= dayStart && channelEvent.StartTime <= dayEnd)
            {
                log.LogInformation($"TwitchChannelEventProcess TwitchName {ChannelEventItem.Subscription.TwitchName} EventId {ChannelEventItem.Event.Id} in a day");
                scheduledEvent.Type = TwitchScheduledChannelEventType.Day;
            }
            else if (channelEvent.StartTime >= weekStart && channelEvent.StartTime <= weekEnd)
            {
                log.LogInformation($"TwitchChannelEventProcess TwitchName {ChannelEventItem.Subscription.TwitchName} EventId {ChannelEventItem.Event.Id} in a week");
                scheduledEvent.Type = TwitchScheduledChannelEventType.Week;
            }
            else
            {
                log.LogInformation($"TwitchChannelEventProcess TwitchName {ChannelEventItem.Subscription.TwitchName} EventId {ChannelEventItem.Event.Id} Unknown {channelEvent.StartTime}");
                scheduledEvent.Type = TwitchScheduledChannelEventType.Unknown;
            }

            if(scheduledEvent.Type != TwitchScheduledChannelEventType.Unknown)
            {
                log.LogInformation($"TwitchChannelEventProcess Queing TwitchName {scheduledEvent.EventItem.Subscription.TwitchName} EventId {scheduledEvent.EventItem.Event.Id} Type {scheduledEvent.Type}");
                await DiscordQueue.AddAsync(scheduledEvent);
                await TwitterQueue.AddAsync(scheduledEvent);
            }

            log.LogInformation($"TwitchChannelEventProcess End");
        }
    }
}
