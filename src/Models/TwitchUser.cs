using System;
using System.Collections.Generic;
using Newtonsoft.Json;
//{
//  "data": [{
//    "id": "44322889",
//    "login": "dallas",
//    "display_name": "dallas",
//    "type": "staff",
//    "broadcaster_type": "",
//    "description": "Just a gamer playing games and chatting. :)",
//    "profile_image_url": "https://static-cdn.jtvnw.net/jtv_user_pictures/dallas-profile_image-1a2c906ee2c35f12-300x300.png",
//    "offline_image_url": "https://static-cdn.jtvnw.net/jtv_user_pictures/dallas-channel_offline_image-1a2c906ee2c35f12-1920x1080.png",
//    "view_count": 191836881,
//    "email": "login@provider.com"
//  }]
//}
namespace Markekraus.TwitchStreamNotifications.Models
{
    public class TwitchUser
    {

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("login")]
        public string Login { get; set; }

        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("broadcaster_type")]
        public string BroadcasterType { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("profile_image_url")]
        public string ProfileImageUrl { get; set; }

        [JsonProperty("offline_image_url")]
        public string OfflineImageUrl { get; set; }

        [JsonProperty("view_count")]
        public Int64 ViewCount { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }
    }
}