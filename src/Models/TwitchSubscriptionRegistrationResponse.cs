using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Markekraus.TwitchStreamNotifications.Models
{
    public class TwitchSubscriptionRegistrationResponse
    {
        [JsonProperty("RequestSubscriptions")]
        public IList<TwitchSubscription> RequestSubscriptions { get; set; }

        [JsonProperty("CurrentSubscriptions")]
        public IList<TwitchWebhookSubscription> CurrentSubscriptions { get; set; }

        [JsonProperty("AddSubscriptions")]
        public IList<TwitchSubscription> AddSubscriptions { get; set; }

        [JsonProperty("RemoveSubscriptions")]
        public IList<TwitchSubscription> RemoveSubscriptions { get; set; }

        [JsonProperty("RenewSubscriptions")]
        public IList<TwitchSubscription> RenewSubscriptions { get; set; }
    }
}