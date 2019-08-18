using System.Collections.Generic;
using Newtonsoft.Json;

namespace Markekraus.TwitchStreamNotifications.Models
{
    public class TwitchGamesData
    {

        [JsonProperty("data")]
        public IList<TwitchGame> Data { get; set; }
    }
}
