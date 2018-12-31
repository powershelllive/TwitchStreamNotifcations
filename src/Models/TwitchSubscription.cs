using System;
using Newtonsoft.Json;

namespace Markekraus.TwitchStreamNotifications.Models
{
    public class TwitchSubscription
    {

        [JsonProperty("twitchname")]
        public string TwitchName { get; set; }

        [JsonProperty("twittername")]
        public string TwitterName { get; set; }

        [JsonProperty("discordname")]
        public string DiscordName { get; set; }
    }
}
