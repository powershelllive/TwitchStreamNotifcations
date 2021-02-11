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
using Microsoft.Azure.WebJobs;

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
        private const string TwitchChannelEventUriTemplate = "https://api.twitch.tv/v5/channels/{0}/events";
        private const string TwitchGamesEndpointUri = "https://api.twitch.tv/helix/games";
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
            message.Headers.TryAddWithoutValidation(ClientIdHeaderName, clientId);
            
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

            var authToken = await GetOAuthResponse(Log);
            var message = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(requestUri)
            };
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer",authToken.AccessToken);
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

            var authToken = await GetOAuthResponse(Log);
            var message = new HttpRequestMessage()
            {
                Content = new StringContent(requestBody, Encoding.UTF8, Utility.ApplicationJsonContentType),
                Method = HttpMethod.Post,
                RequestUri = new Uri(TwitchWebhooksHubEndpointUri)
            };
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer",authToken.AccessToken);
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

        public static async Task<TwitchChannelEventResponse> GetTwitchSubscriptionEvents (TwitchSubscription Subscription, ILogger Log)
        {
            Log.LogInformation("GetTwitchSubscriptionEvents Begin");
            Log.LogInformation($"GetTwitchSubscriptionEvents TwitchName: {Subscription.TwitchName}");
            Log.LogInformation($"GetTwitchSubscriptionEvents TwitterName: {Subscription.TwitterName}");
            Log.LogInformation($"GetTwitchSubscriptionEvents DiscordName: {Subscription.DiscordName}");

            var userId = await GetTwitchStreamUserId(Subscription.TwitchName, Log);
            Log.LogInformation($"GetTwitchSubscriptionEvents UserID: {userId}");

            var requestUri = string.Format(TwitchChannelEventUriTemplate, userId);
            Log.LogInformation($"GetTwitchSubscriptionEvents RequestUri: {requestUri}");

            var message = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(requestUri)
            };
            message.Headers.TryAddWithoutValidation(ClientIdHeaderName, clientId);

            var httpResponse = await client.SendAsync(message,HttpCompletionOption.ResponseContentRead);

            LogHttpResponse(httpResponse, "GetTwitchSubscriptionEvents", Log);

            if(!httpResponse.IsSuccessStatusCode)
            {
                Log.LogInformation("GetTwitchSubscriptionEvents End");
                return null;
            }
            else
            {
                var responseBody = await httpResponse.Content.ReadAsStringAsync();
                Log.LogInformation("GetTwitchSubscriptionEvents ResponseBody:");
                Log.LogInformation(responseBody);

                var response = JsonConvert.DeserializeObject<TwitchChannelEventResponse>(responseBody);

                Log.LogInformation("GetTwitchStreamUserId End");
                return response;
            }
        }

        public static async Task<TwitchSubscriptionRegistrationResponse> InvokeSubscriptionRegistration (
            IList<TwitchSubscription> TwitchSubscriptions,
            IAsyncCollector<TwitchSubscription> SubscribeQueue,
            IAsyncCollector<TwitchSubscription> UnsubscribeQueue,
            ILogger log,
            string CallingFunction)
        {
            var logPrefix = $"{CallingFunction}.{nameof(InvokeSubscriptionRegistration)}";

            var response = new TwitchSubscriptionRegistrationResponse();
            response.RequestSubscriptions = TwitchSubscriptions;

            log.LogInformation($"{logPrefix} Get current subscriptions");
            var currentSubscriptions = await GetTwitchWebhookSubscriptions(log);
            response.CurrentSubscriptions = currentSubscriptions;

            log.LogInformation($"{logPrefix} Create currentSubDictionary dictionary");
            var currentSubDictionary = new Dictionary<Tuple<string, string, string>,TwitchSubscription>();
            var currentWebhookSubDictionary = new Dictionary<Tuple<string, string, string>,TwitchWebhookSubscription>();
            string currentTwittername;
            string currentDiscordname;
            string currentTwitchname;
            foreach(var subscription in currentSubscriptions)
            {
                currentTwittername = string.IsNullOrWhiteSpace(subscription.Subscription.TwitterName) ? Utility.NameNullString : subscription.Subscription.TwitterName.ToLower();
                currentDiscordname = string.IsNullOrWhiteSpace(subscription.Subscription.DiscordName) ? Utility.NameNullString : subscription.Subscription.DiscordName.ToLower();
                currentTwitchname = subscription.Subscription.TwitchName.ToLower();
                log.LogInformation($"{logPrefix} Add TwitchName {subscription.Subscription.TwitchName} TwitterName {subscription.Subscription.TwitterName} DiscordName {subscription.Subscription.DiscordName}");
                var keyTuple = new Tuple<string, string, string>(currentTwitchname, currentTwittername, currentDiscordname);
                currentSubDictionary.Add(
                    keyTuple, 
                    subscription.Subscription);
                currentWebhookSubDictionary.Add(keyTuple, subscription);
            }

            log.LogInformation($"{logPrefix} Create requestedSubDictionary dictionary");
            var requestedSubDictionary = new Dictionary<Tuple<string, string, string>,TwitchSubscription>();
            foreach(var subscription in TwitchSubscriptions)
            {
                currentTwittername = string.IsNullOrWhiteSpace(subscription.TwitterName) ? Utility.NameNullString : subscription.TwitterName.ToLower();
                currentDiscordname = string.IsNullOrWhiteSpace(subscription.DiscordName) ? Utility.NameNullString : subscription.DiscordName.ToLower();
                currentTwitchname = subscription.TwitchName.ToLower();
                log.LogInformation($"{logPrefix} Add TwitchName {subscription.TwitchName} TwitterName {subscription.TwitterName} DiscordName {subscription.DiscordName}");
                requestedSubDictionary.Add(
                    new Tuple<string, string, string>(currentTwitchname, currentTwittername, currentDiscordname),
                    subscription);
            }

            log.LogInformation($"{logPrefix} Find missing subscriptions to add");
            response.AddSubscriptions = new List<TwitchSubscription>();
            foreach(var missing in Enumerable.Except(requestedSubDictionary.Keys, currentSubDictionary.Keys))
            {
                log.LogInformation($"{logPrefix} Add Queue TwitchName {requestedSubDictionary[missing].TwitchName} TwitterName {requestedSubDictionary[missing].TwitterName}");
                await SubscribeQueue.AddAsync(requestedSubDictionary[missing]);
                response.AddSubscriptions.Add(requestedSubDictionary[missing]);
            }

            log.LogInformation($"{logPrefix} Find extra subscriptions to remove");
            response.RemoveSubscriptions = new List<TwitchSubscription>();
            foreach(var extra in Enumerable.Except(currentSubDictionary.Keys, requestedSubDictionary.Keys))
            {
                log.LogInformation($"{logPrefix} Remove Queue TwitchName {currentSubDictionary[extra].TwitchName} TwitterName {currentSubDictionary[extra].TwitterName}");
                await UnsubscribeQueue.AddAsync(currentSubDictionary[extra]);
                response.RemoveSubscriptions.Add(currentSubDictionary[extra]);
            }

            var renewalableKeyList = Enumerable.Intersect(requestedSubDictionary.Keys, currentWebhookSubDictionary.Keys);
            response.RenewSubscriptions = new List<TwitchSubscription>();
            foreach (var renewableKey in renewalableKeyList)
            {
                var renewableSub = currentWebhookSubDictionary[renewableKey];
                if(DateTime.Parse(renewableSub.ExpiresAt).ToUniversalTime() <= DateTime.UtcNow.AddHours(1))
                {
                    log.LogInformation($"{logPrefix} Renew Queue TwitchName {renewableSub.Subscription.TwitchName} TwitterName {renewableSub.Subscription.TwitterName}");
                    await SubscribeQueue.AddAsync(renewableSub.Subscription);
                    response.RenewSubscriptions.Add(renewableSub.Subscription);
                }
            }

            return response;
        }

        public static async Task<TwitchGame> GetGame(string GameId, ILogger Log)
        {
            Log.LogInformation("GetGame Begin");
            Log.LogInformation($"GetGame GameId: {GameId}");

            var requestUri = $"{TwitchGamesEndpointUri}?id={GameId}";
            Log.LogInformation($"GetGame RequestUri: {requestUri}");

            var authToken = await GetOAuthResponse(Log);
            var message = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(requestUri)
            };
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer",authToken.AccessToken);
            message.Headers.TryAddWithoutValidation(ClientIdHeaderName, clientId);

            var httpResponse = await client.SendAsync(message,HttpCompletionOption.ResponseContentRead);

            LogHttpResponse(httpResponse, "GetGame", Log);

            if(!httpResponse.IsSuccessStatusCode)
            {
                Log.LogInformation("GetGame End");
                return null;
            }
            else
            {
                var responseBody = await httpResponse.Content.ReadAsStringAsync();
                Log.LogInformation("GetGame ResponseBody:");
                Log.LogInformation(responseBody);

                var response = JsonConvert.DeserializeObject<TwitchGamesData>(responseBody).Data.First();

                Log.LogInformation("GetGame End");
                return response;
            }
        }
    }
}