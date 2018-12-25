using System.Collections.Generic;
using Newtonsoft.Json;

namespace TwitchLib.Webhook.Models
{
    public class StreamData
    {

        [JsonProperty("data")]
        public IList<Stream> Data { get; set; }
    }
}
