using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Tweetinvi;
using TwitchLib.Webhook.Models;

namespace Markekraus.TwitchStreamNotifications
{
    public static class DiscordEventHandler
    {
      private readonly static string DiscordWebhookUri = Environment.GetEnvironmentVariable("DiscordWebhookUri");
      private readonly static string DiscordMessageTemplate = Environment.GetEnvironmentVariable("DiscordMessageTemplate");
      private const int MaxMessageSize = 2000;
      private static HttpClient DiscordClient = new HttpClient();

        [FunctionName("DiscordEventHandler")]
        public static async Task Run(
            [QueueTrigger("%DiscordNotifications%", Connection = "TwitchStreamStorage")]
            TwitchLib.Webhook.Models.Stream StreamEvent,
            ILogger log)
        {
            log.LogInformation($"DiscordEventHandler processing: {StreamEvent.UserName} type {StreamEvent.Type} started at {StreamEvent.StartedAt}");

            if (StreamEvent.Type != "live")
            { 
                log.LogInformation($"Processing event skipped. type: {StreamEvent.Type}");
                return;
            }

            string username;
            if (string.IsNullOrWhiteSpace(StreamEvent.Subscription.DiscordName))
            {
                username = StreamEvent.UserName;
                log.LogInformation($"Stream username {username} will be used");
            }
            else
            {
                username = $"<@{StreamEvent.Subscription.DiscordName}>";
                log.LogInformation($"Discord username {username} will be used");
            }

            string streamUri = $"https://twitch.tv/{StreamEvent.UserName}";
            log.LogInformation($"Stream Uri: {streamUri}");

            var myDiscordMessage = new DiscordMessage()
            {
                Content = string.Format(DiscordMessageTemplate, streamUri, username, DateTime.UtcNow.ToString("u"))
            };

            log.LogInformation($"DiscordMessage: {myDiscordMessage.Content}");

            if(myDiscordMessage.Content.Length >= MaxMessageSize)
            {
                log.LogError($"Discord messages is {myDiscordMessage.Content.Length} long and exceeds the {MaxMessageSize} max length.");
                return;
            }

            if(Environment.GetEnvironmentVariable(Utility.DISABLE_NOTIFICATIONS).ToLower() == "true") {
                log.LogInformation("Notifications are disabled. exiting");
                return;
            }

            var httpMessageBody = JsonConvert.SerializeObject(myDiscordMessage);
            log.LogInformation("HttpMessageBody:");
            log.LogInformation(httpMessageBody);

            var httpMessage = new HttpRequestMessage()
            {
                RequestUri = new Uri(DiscordWebhookUri),
                Content = new StringContent(httpMessageBody, Encoding.UTF8, Utility.ApplicationJsonContentType),
                Method = HttpMethod.Post
            };

            var httpResponse = await DiscordClient.SendAsync(httpMessage, HttpCompletionOption.ResponseHeadersRead);

            if(!httpResponse.IsSuccessStatusCode)
            {
                log.LogError($"Request Failed");
            }
            log.LogInformation($"Success: {httpResponse.IsSuccessStatusCode}");
            log.LogInformation($"StatusCode: {httpResponse.StatusCode}");
            log.LogInformation($"ReasonPhrase: {httpResponse.ReasonPhrase}");
            
            var responseBody = await httpResponse.Content.ReadAsStringAsync();
            log.LogInformation($"Response:");
            log.LogInformation(responseBody);
        }
    }
}
