using System.Collections.Generic;
using Newtonsoft.Json;

namespace TwitchLib.Webhook.Models
{
    public class DiscordMessage
    {

        [JsonProperty("content")]
        public string Content { get; set; }
    }
}
