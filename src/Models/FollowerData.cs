using Newtonsoft.Json;

namespace TwitchLib.Webhook.Models
{
    public class FollowerData
    {
        [JsonProperty("from_id")]
        public string FromId { get; set; }

        [JsonProperty("to_id")]
        public string ToId { get; set; }
    }
}
