using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Markekraus.TwitchStreamNotifications.Models;
using Newtonsoft.Json;

namespace Markekraus.TwitchStreamNotifications
{
    public static class DiscordClient
    {
        private readonly static string DiscordWebhookUri = Environment.GetEnvironmentVariable("DiscordWebhookUri");
        private const int MaxMessageSize = 2000;
        private static HttpClient client = new HttpClient();

        public static async Task<HttpResponseMessage> SendDiscordMessageAsync(DiscordMessage Message, ILogger log)
        {
            log.LogInformation($"SendDiscordMessageAsync DiscordMessage: {Message.Content}");

            if (Message.Content.Length >= MaxMessageSize)
            {
                log.LogError($"SendDiscordMessageAsync Discord messages is {Message.Content.Length} long and exceeds the {MaxMessageSize} max length.");
                return null;
            }

            if (Environment.GetEnvironmentVariable(Utility.DISABLE_NOTIFICATIONS).ToLower() == "true")
            {
                log.LogInformation("SendDiscordMessageAsync Notifications are disabled. exiting");
                return null;
            }

            var httpMessageBody = JsonConvert.SerializeObject(Message);
            log.LogInformation("SendDiscordMessageAsync HttpMessageBody:");
            log.LogInformation(httpMessageBody);

            var httpMessage = new HttpRequestMessage()
            {
                RequestUri = new Uri(DiscordWebhookUri),
                Content = new StringContent(httpMessageBody, Encoding.UTF8, Utility.ApplicationJsonContentType),
                Method = HttpMethod.Post
            };

            var httpResponse = await client.SendAsync(httpMessage, HttpCompletionOption.ResponseHeadersRead);

            if (!httpResponse.IsSuccessStatusCode)
            {
                log.LogError($"SendDiscordMessageAsync Request Failed");
            }
            log.LogInformation($"SendDiscordMessageAsync Success: {httpResponse.IsSuccessStatusCode}");
            log.LogInformation($"SendDiscordMessageAsync StatusCode: {httpResponse.StatusCode}");
            log.LogInformation($"SendDiscordMessageAsync ReasonPhrase: {httpResponse.ReasonPhrase}");

            var responseBody = await httpResponse.Content.ReadAsStringAsync();
            log.LogInformation($"SendDiscordMessageAsync Response:");
            log.LogInformation(responseBody);

            return httpResponse;
        }
    }
}
