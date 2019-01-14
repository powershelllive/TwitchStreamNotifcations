using System;
using Newtonsoft.Json;

namespace Markekraus.TwitchStreamNotifications.Models
{
    public class TwitchScheduledChannelEvent
    {
        [JsonProperty("type")]
        public TwitchScheduledChannelEventType Type { get; set; }

        [JsonProperty("event")]
        public TwitchChannelEventItem EventItem { get; set; }

        public TwitchScheduledChannelEvent() {}

        public TwitchScheduledChannelEvent(TwitchChannelEventItem EventItem)
        {
            this.EventItem = EventItem;
            this.Type = TwitchScheduledChannelEventType.Unknown;
        }
    }
}