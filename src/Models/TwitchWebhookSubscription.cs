using System;
using Newtonsoft.Json;

namespace Markekraus.TwitchStreamNotifications.Models
{
    public class TwitchWebhookSubscription
    {

        [JsonProperty("callback")]
        public string Callback { get; set; }

        [JsonProperty("topic")]
        public string Topic { get; set; }

        [JsonProperty("expires_at")]
        public string ExpiresAt { get; set; }

        private TwitchSubscription subscription;

        [JsonProperty("subscription")]
        public TwitchSubscription Subscription { 
            get
            {
                if(subscription != null || string.IsNullOrWhiteSpace(Callback))
                {
                    return subscription;
                }
                else
                {
                    subscription = new TwitchSubscription();

                    var parts = Callback.Split("/");
                    if(parts.Length >= 6)
                    {
                        subscription.TwitchName = parts[5];
                    }

                    if (parts.Length == 7)
                    {
                        subscription.TwitterName = parts[6];
                    }
                    return subscription;
                }
            }
        }
    }
}
