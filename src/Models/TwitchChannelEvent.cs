using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Markekraus.TwitchStreamNotifications.Models
{
    public class TwitchChannelEvent
    {

        [JsonProperty("_id")]
        public string Id { get; set; }

        [JsonProperty("owner_id")]
        public int OwnerId { get; set; }

        [JsonProperty("channel_id")]
        public int ChannelId { get; set; }

        [JsonProperty("start_time")]
        public DateTime StartTime { get; set; }

        [JsonProperty("end_time")]
        public DateTime EndTime { get; set; }

        [JsonProperty("time_zone_id")]
        public string TimeZoneId { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("game_id")]
        public string Int { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("cover_image_id")]
        public string CoverImageId { get; set; }

        [JsonProperty("cover_image_url")]
        public string CoverImageUrl { get; set; }

        [JsonProperty("channel")]
        public TwitchChannel Channel { get; set; }

        [JsonProperty("game")]
        public TwitchGame Game { get; set; }

    }
}
