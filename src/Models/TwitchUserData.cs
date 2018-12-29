using System.Collections.Generic;
using Newtonsoft.Json;

namespace Markekraus.TwitchStreamNotifications.Models
{
    public class TwitchUserData
    {

        [JsonProperty("data")]
        public IList<TwitchUser> Data { get; set; }
    }
}