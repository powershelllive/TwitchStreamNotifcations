using System.Collections.Generic;
using Newtonsoft.Json;

namespace Markekraus.TwitchStreamNotifications.Models
{
    public class TwitchSubscriptionData
    {

        [JsonProperty("data")]
        public IList<TwitchSubscription> Data { get; set; }
    }
}
