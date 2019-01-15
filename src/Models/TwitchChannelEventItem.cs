using System;
using Newtonsoft.Json;

namespace Markekraus.TwitchStreamNotifications.Models
{
    public class TwitchChannelEventItem
    {
        [JsonProperty("event")]
        public TwitchChannelEvent Event { get; set; }

        [JsonProperty("subscription")]
        public TwitchSubscription Subscription { get; set; }
    }
}

