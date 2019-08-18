using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Markekraus.TwitchStreamNotifications
{
    // Borrowing from https://markheath.net/post/randomly-scheduled-tweets-azure-functions
    public static class TwitterEventHandler
    {
        private readonly static string TwitterTweetTemplate = Environment.GetEnvironmentVariable("TwitterTweetTemplate");

        [FunctionName("TwitterEventHandler")]
        public static async Task Run([QueueTrigger("%TwitterNotifications%", Connection = "TwitchStreamStorage")]TwitchLib.Webhook.Models.Stream StreamEvent, ILogger log)
        {
            log.LogInformation($"TwitterEventHandler processing: {StreamEvent.UserName} type {StreamEvent.Type} started at {StreamEvent.StartedAt}");

            if (StreamEvent.Type != "live")
            { 
                log.LogInformation($"TwitterEventHandler Processing event skipped. type: {StreamEvent.Type}");
                return;
            }


            string username;
            if (string.IsNullOrWhiteSpace(StreamEvent.Subscription.TwitterName))
            {
                username = StreamEvent.UserName;
                log.LogInformation($"TwitterEventHandler Stream username {username} will be used");
            }
            else
            {
                username = $"@{StreamEvent.Subscription.TwitterName}";
                log.LogInformation($"TwitterEventHandler Twitter username {username} will be used");
            }

            var game = await StreamEvent.GetGameName(log);
            log.LogInformation($"TwitterEventHandler Twitter game {game} will be used");

            string streamUri = $"https://twitch.tv/{StreamEvent.UserName}";
            log.LogInformation($"TwitterEventHandler Stream Uri: {streamUri}");

            string myTweet = string.Format(TwitterTweetTemplate, streamUri, username, DateTime.UtcNow.ToString("u"), game);
            
            TwitterClient.PublishTweet(myTweet, log);
        }
    }
}
