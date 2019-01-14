using System.Collections.Generic;
using Newtonsoft.Json;

namespace Markekraus.TwitchStreamNotifications.Models
{
    public class TwitchChannelEventResponse
    {

        [JsonProperty("_total")]
        public int Total { get; set; }

        [JsonProperty("events")]
        public IList<TwitchChannelEvent> Events { get; set; }
    }
}
