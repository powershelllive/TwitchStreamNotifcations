using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Markekraus.TwitchStreamNotifications.Models;
using Markekraus.TwitchStreamNotifications;
using Microsoft.Extensions.Logging;

namespace TwitchLib.Webhook.Models
{
    public class Stream
    {

        private const string DefaultGame = "unknown game";

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("user_id")]
        public string UserId { get; set; }

        [JsonProperty("user_name")]
        public string UserName { get; set; }

        [JsonProperty("game_id")]
        public string GameId { get; set; }

        [JsonProperty("community_ids")]
        public IList<string> CommunityIds { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("viewer_count")]
        public int ViewerCount { get; set; }

        [JsonProperty("started_at")]
        public DateTime StartedAt { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("thumbnail_url")]
        public string ThumbnailUrl { get; set; }

        [JsonProperty("subscription")]
        public TwitchSubscription Subscription { get; set; }

        public async Task<string> GetGameName(ILogger Log)
        {
            string game;
            if(string.IsNullOrWhiteSpace(this.GameId))
            {
                game = DefaultGame;
            }
            else
            {
                try
                {
                  game = (await TwitchClient.GetGame(this.GameId, Log)).Name;
                }
                catch (Exception e)
                {
                  Log.LogError($"Failed to get game. GameId {this.GameId}: {e.Message}: {e.StackTrace}");
                  game = DefaultGame;
                }
                if(string.IsNullOrWhiteSpace(game))
                {
                  Log.LogError($"GetGame returned null. GameId {this.GameId}");
                  game = DefaultGame;
                }
            }
            return game;
        }
    }
}
