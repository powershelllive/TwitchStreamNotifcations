using System.Collections.Generic;
using Newtonsoft.Json;

namespace Markekraus.TwitchStreamNotifications.Models
{
    public class DiscordMessage
    {

        [JsonProperty("content")]
        public string Content { get; set; }
    }
}
