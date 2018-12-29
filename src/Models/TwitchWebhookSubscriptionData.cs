using System.Collections.Generic;
using Newtonsoft.Json;

namespace Markekraus.TwitchStreamNotifications.Models
{
    public class TwitchWebhookSubscriptionData
    {

        [JsonProperty("data")]
        public IList<TwitchWebhookSubscription> Data { get; set; }
    }
}