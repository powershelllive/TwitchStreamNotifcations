using System;
using System.Threading.Tasks;
using Markekraus.TwitchStreamNotifications.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Markekraus.TwitchStreamNotifications
{
    public static class DiscordEventHandler
    {
        private readonly static string DiscordMessageTemplate = Environment.GetEnvironmentVariable("DiscordMessageTemplate");

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

            await DiscordClient.SendDiscordMessageAsync(myDiscordMessage, log);
        }
    }
}
