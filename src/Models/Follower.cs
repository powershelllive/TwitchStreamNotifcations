using System;
using Newtonsoft.Json;

namespace TwitchLib.Webhook.Models
{
    public class Follower
    {
        [JsonProperty("data")]
        public FollowerData Data { get; set; }
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
        [JsonProperty("topic")]
        public string Topic { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
    }
}
