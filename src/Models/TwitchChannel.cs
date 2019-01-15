using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Markekraus.TwitchStreamNotifications.Models
{
    public class TwitchChannel
    {
        [JsonProperty("mature")]
        public bool Mature { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("broadcaster_language")]
        public string BroadcasterLanguage { get; set; }

        [JsonProperty("broadcaster_software")]
        public string BroadcasterSoftware { get; set; }

        [JsonProperty("game")]
        public string Game { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("_id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonProperty("partner")]
        public bool Partner { get; set; }

        [JsonProperty("logo")]
        public string Logo { get; set; }

        [JsonProperty("video_banner")]
        public string VideoBanner { get; set; }

        [JsonProperty("profile_banner")]
        public string ProfileBanner { get; set; }

        [JsonProperty("profile_banner_background_color")]
        public string ProfileBannerBackgroundColor { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }

        [JsonProperty("views")]
        public int Views { get; set; }

        [JsonProperty("followers")]
        public int Followers { get; set; }

        [JsonProperty("broadcaster_type")]
        public string BroadcasterType { get; set; }

        [JsonProperty("description")]
        public string description { get; set; }

        [JsonProperty("private_video")]
        public bool PrivateVideo { get; set; }

        [JsonProperty("privacy_options_enabled")]
        public bool PrivacyOptionsEnabled { get; set; }
    }
}