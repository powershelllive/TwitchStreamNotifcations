using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Markekraus.TwitchStreamNotifications.Models
{
    public class TwitchGame
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("popularity")]
        public int Popularity { get; set; }

        [JsonProperty("_id")]
        public int Id { get; set; }

        [JsonProperty("giantbomb_id")]
        public string GiantbombID { get; set; }

        [JsonProperty("box")]
        public TwitchImage Box { get; set; }

        [JsonProperty("logo")]
        public TwitchImage Logo { get; set; }

        [JsonProperty("localized_name")]
        public string LocalizedName { get; set; }

        [JsonProperty("locale")]
        public string Locale { get; set; }
    }
}