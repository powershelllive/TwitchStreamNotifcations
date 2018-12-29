using System;
using Newtonsoft.Json;

namespace Markekraus.TwitchStreamNotifications.Models
{
    public class TwitchHubSubscription
    {

        [JsonProperty("hub.callback")]
        public string HubCallback { get; set; }

        [JsonProperty("hub.topic")]
        public string HubTopic { get; set; }

        [JsonProperty("hub.mode")]
        public string HubMode { get; set; }

        [JsonProperty("hub.lease_seconds")]
        public int HubLeaseSeconds { get; set; }

        [JsonProperty("hub.secret")]
        public string HubSecret { get; set; }
    }
}
