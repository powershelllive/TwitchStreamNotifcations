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
        public static async Task Run(
            [QueueTrigger("%TwitchChannelEventProcessQueue%", Connection = "TwitchStreamStorage")] TwitchChannelEventItem ChannelEventItem,
            [Queue("%DiscordEventNotificationsQueue%")] IAsyncCollector<TwitchScheduledChannelEvent> DiscordQueue,
            [Queue("%TwitterEventNotificationsQueue%")] IAsyncCollector<TwitchScheduledChannelEvent> TwitterQueue,
            ILogger log)
        {
            log.LogInformation($"TwitchChannelEventProcess function processed: TwitchName {ChannelEventItem.Subscription.TwitchName} EventId {ChannelEventItem.Event.Id} StartTime {ChannelEventItem.Event.StartTime}");

            var channelEvent = ChannelEventItem.Event;

            var now = DateTime.UtcNow;
            var nowHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
            var hourStart = nowHour.AddHours(1);
            var hourEnd = hourStart.AddHours(1).AddSeconds(-1);
            var weekStart = nowHour.AddDays(7);
            var weekEnd = weekStart.AddHours(1).AddSeconds(-1);
            var dayStart = nowHour.AddDays(1);
            var dayEnd = dayStart.AddHours(1).AddSeconds(-1);

            log.LogInformation($"TwitchChannelEventProcess Now {now}");
            log.LogInformation($"TwitchChannelEventProcess HourStart {hourStart}");
            log.LogInformation($"TwitchChannelEventProcess HourEnd {hourEnd}");
            log.LogInformation($"TwitchChannelEventProcess DayStart {dayStart}");
            log.LogInformation($"TwitchChannelEventProcess DayEnd {dayEnd}");
            log.LogInformation($"TwitchChannelEventProcess WeekStart {weekStart}");
            log.LogInformation($"TwitchChannelEventProcess WeekEnd {weekEnd}");

            var scheduledEvent = new TwitchScheduledChannelEvent(ChannelEventItem);

            if (channelEvent.StartTime >= hourStart && channelEvent.StartTime <= hourEnd)
            {
                log.LogInformation($"TwitchChannelEventProcess TwitchName {ChannelEventItem.Subscription.TwitchName} EventId {ChannelEventItem.Event.Id} in an hour {channelEvent.StartTime}");
                scheduledEvent.Type = TwitchScheduledChannelEventType.Hour;
            }
            else if (channelEvent.StartTime >= dayStart && channelEvent.StartTime <= dayEnd)
            {
                log.LogInformation($"TwitchChannelEventProcess TwitchName {ChannelEventItem.Subscription.TwitchName} EventId {ChannelEventItem.Event.Id} in a day {channelEvent.StartTime}");
                scheduledEvent.Type = TwitchScheduledChannelEventType.Day;
            }
            else if (channelEvent.StartTime >= weekStart && channelEvent.StartTime <= weekEnd)
            {
                log.LogInformation($"TwitchChannelEventProcess TwitchName {ChannelEventItem.Subscription.TwitchName} EventId {ChannelEventItem.Event.Id} in a week {channelEvent.StartTime}");
                scheduledEvent.Type = TwitchScheduledChannelEventType.Week;
            }
            else
            {
                log.LogInformation($"TwitchChannelEventProcess TwitchName {ChannelEventItem.Subscription.TwitchName} EventId {ChannelEventItem.Event.Id} Unknown {channelEvent.StartTime}");
                scheduledEvent.Type = TwitchScheduledChannelEventType.Unknown;
            }

            if (scheduledEvent.Type != TwitchScheduledChannelEventType.Unknown)
            {
                log.LogInformation($"TwitchChannelEventProcess Queing TwitchName {scheduledEvent.EventItem.Subscription.TwitchName} EventId {scheduledEvent.EventItem.Event.Id} Type {scheduledEvent.Type}");
                await DiscordQueue.AddAsync(scheduledEvent);
                await TwitterQueue.AddAsync(scheduledEvent);
            }

            log.LogInformation($"TwitchChannelEventProcess End");
        }
    }
}
