using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Markekraus.TwitchStreamNotifications.Models;
using Newtonsoft.Json;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Markekraus.TwitchStreamNotifications
{
    public static class TwitchClient
    {
        static private HttpClient client = new HttpClient();
        static private readonly string clientId = Environment.GetEnvironmentVariable("TwitchClientId");
        static private readonly string clientSecret = Environment.GetEnvironmentVariable("TwitchClientSecret");
        static private readonly string clientRedirectUri = Environment.GetEnvironmentVariable("TwitchClientRedirectUri");
        private static readonly string HubSecret = Environment.GetEnvironmentVariable("TwitchSubscriptionsHashSecret");
        private static readonly string TwitchWebhookBaseUri = Environment.GetEnvironmentVariable("TwitchWebhookBaseUri");

        private const string TwitchOAuthBaseUri = "https://id.twitch.tv/oauth2/token";
        private const string TwitchUsersEndpointUri = "https://api.twitch.tv/helix/users";
        private const string TwitchStreamsEndpointUri = "https://api.twitch.tv/helix/streams";
        private const string TwitchWebhooksHubEndpointUri = "https://api.twitch.tv/helix/webhooks/hub";
        private const string TwitchWebhooksSubscriptionsEndpointUri = "https://api.twitch.tv/helix/webhooks/subscriptions";
        private const string ClientIdHeaderName = "Client-ID";

        private enum TwitchSubscriptionMode
        {
            Subscribe,
            Unsubscribe
        }

        private static async Task<TwitchOAuthResponse> GetOAuthResponse (ILogger Log)
        {
            Log.LogInformation("GetOAuthResponse Begin");
            var dict = new Dictionary<string, string>();
            dict.Add("client_id", clientId);
            dict.Add("client_secret", clientSecret);
            dict.Add("grant_type", "client_credentials");

            var requestUri = TwitchOAuthBaseUri;
            Log.LogInformation($"RequestUri: {requestUri}");

            var message = new HttpRequestMessage()
            {
                Content = new FormUrlEncodedContent(dict),
                Method = HttpMethod.Post,
                RequestUri = new Uri(requestUri)
            };
            message.Headers.TryAddWithoutValidation("Accept",Utility.ApplicationJsonContentType);
            var response = await client.SendAsync(message, HttpCompletionOption.ResponseContentRead);
            LogHttpResponse(response, "GetOAuthResponse", Log);


            var responseBody = await response.Content.ReadAsStringAsync();
            if(!response.IsSuccessStatusCode)
            {
                Log.LogInformation("ResponseBody:");
                Log.LogInformation(responseBody);
                Log.LogInformation("GetOAuthResponse End");
                return null;
            }
            else
            {
                Log.LogInformation("GetOAuthResponse End");
                return JsonConvert.DeserializeObject<TwitchOAuthResponse>(responseBody);
            }
        }

        public static async Task<IList<TwitchWebhookSubscription>> GetTwitchWebhookSubscriptions(ILogger Log)
        {
            Log.LogInformation("GetTwitchWebhookSubscriptions Begin");

            var requestUri = $"{TwitchWebhooksSubscriptionsEndpointUri}?first=100";
            Log.LogInformation($"RequestUri: {requestUri}");

            var authToken = await GetOAuthResponse(Log);

            var message = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(requestUri)
            };
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer",authToken.AccessToken);

            var response = await client.SendAsync(message,HttpCompletionOption.ResponseContentRead);

            LogHttpResponse(response, "GetTwitchWebhookSubscriptions", Log);

            var responseBody = await response.Content.ReadAsStringAsync();
            if(!response.IsSuccessStatusCode)
            {
                Log.LogInformation("GetTwitchWebhookSubscriptions End");
                return null;
            }
            else
            {
                var subscriptionData = JsonConvert.DeserializeObject<TwitchWebhookSubscriptionData>(responseBody);
                return subscriptionData.Data;
            }
        }

        private static async Task<string> GetTwitchStreamUserId(string TwitchName, ILogger Log)
        {
            Log.LogInformation("GetTwitchStreamUserId Begin");

            var requestUri = $"{TwitchUsersEndpointUri}?login={WebUtility.UrlEncode(TwitchName)}";
            Log.LogInformation($"RequestUri: {requestUri}");

            var message = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(requestUri)
            };
            message.Headers.TryAddWithoutValidation(ClientIdHeaderName, clientId);

            var response = await client.SendAsync(message,HttpCompletionOption.ResponseContentRead);

            LogHttpResponse(response, "GetTwitchStreamUserId", Log);

            if(!response.IsSuccessStatusCode)
            {
                Log.LogInformation("GetTwitchStreamUserId End");
                return null;
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                Log.LogInformation("ResponseBody:");
                Log.LogInformation(responseBody);

                var userData = JsonConvert.DeserializeObject<TwitchUserData>(responseBody);

                Log.LogInformation("GetTwitchStreamUserId End");
                return userData.Data.FirstOrDefault().Id;
            }
        }

        public static async Task SubscribeTwitchStreamWebhook (TwitchSubscription Subscription,ILogger Log)
        {
           Log.LogInformation("SubscribeTwitchStreamWebhook Begin");
           await SubscriptionActionTwitchStreamWebhook(Subscription, TwitchSubscriptionMode.Subscribe, Log);
           Log.LogInformation("SubscribeTwitchStreamWebhook End");
        }

        public static async Task UnsubscribeTwitchStreamWebhook (TwitchSubscription Subscription,ILogger Log)
        {
           Log.LogInformation("UnsubscribeTwitchStreamWebhook Begin");
           await SubscriptionActionTwitchStreamWebhook(Subscription, TwitchSubscriptionMode.Unsubscribe, Log);
           Log.LogInformation("UnsubscribeTwitchStreamWebhook End");
        }

        private static async Task SubscriptionActionTwitchStreamWebhook(TwitchSubscription Subscription, TwitchSubscriptionMode SubscriptionMode ,ILogger Log)
        {
            Log.LogInformation("SubscriptionActionTwitchStreamWebhook Begin");
            Log.LogInformation($"TwitchName: {Subscription.TwitchName}");
            Log.LogInformation($"TwitterName: {Subscription.TwitterName}");
            Log.LogInformation($"DiscordName: {Subscription.DiscordName}");

            var userId = await GetTwitchStreamUserId(Subscription.TwitchName, Log);
            Log.LogInformation($"UserID: {userId}");

            var twitterPart = string.IsNullOrWhiteSpace(Subscription.TwitterName) ? Utility.NameNullString : Subscription.TwitterName;
            var discordPart = string.IsNullOrWhiteSpace(Subscription.DiscordName) ? Utility.NameNullString : Subscription.DiscordName;
            var callbackUri = $"{TwitchWebhookBaseUri}/{Subscription.TwitchName}/{twitterPart}/{discordPart}";
            Log.LogInformation($"CallbackUri: {callbackUri}");

            var hubTopic = $"{TwitchStreamsEndpointUri}?user_id={userId}";
            Log.LogInformation($"HubTopic: {hubTopic}");

            var hubSubscription = new TwitchHubSubscription()
            {
                HubMode = SubscriptionMode.ToString().ToLower(),
                HubSecret = HubSecret,
                HubTopic = hubTopic,
                HubCallback = callbackUri,
                HubLeaseSeconds = 864000
            };

            var requestBody = JsonConvert.SerializeObject(hubSubscription);

            var message = new HttpRequestMessage()
            {
                Content = new StringContent(requestBody, Encoding.UTF8, Utility.ApplicationJsonContentType),
                Method = HttpMethod.Post,
                RequestUri = new Uri(TwitchWebhooksHubEndpointUri)
            };
            message.Headers.TryAddWithoutValidation(ClientIdHeaderName, clientId);

            var response = await client.SendAsync(message, HttpCompletionOption.ResponseContentRead);

            LogHttpResponse(response, "SubscriptionActionTwitchStreamWebhook", Log);

            var responseBody = await response.Content.ReadAsStringAsync();
            Log.LogInformation($"Response: {responseBody}");

            Log.LogInformation("SubscriptionActionTwitchStreamWebhook End");
        }

        private static void LogHttpResponse (HttpResponseMessage Response, string Operation, ILogger Log)
        {
            if(!Response.IsSuccessStatusCode)
            {
                Log.LogError($"{Operation} Request Failed");
            }
            Log.LogInformation($"Success: {Response.IsSuccessStatusCode}");
            Log.LogInformation($"StatusCode: {Response.StatusCode}");
            Log.LogInformation($"ReasonPhrase: {Response.ReasonPhrase}");
        }
    }
}