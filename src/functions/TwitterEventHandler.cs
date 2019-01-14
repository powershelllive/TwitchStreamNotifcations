using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Markekraus.TwitchStreamNotifications
{
    // Borrowing from https://markheath.net/post/randomly-scheduled-tweets-azure-functions
    public static class TwitterEventHandler
    {
        private readonly static string TwitterTweetTemplate = Environment.GetEnvironmentVariable("TwitterTweetTemplate");

        [FunctionName("TwitterEventHandler")]
        public static void Run([QueueTrigger("%TwitterNotifications%", Connection = "TwitchStreamStorage")]TwitchLib.Webhook.Models.Stream StreamEvent, ILogger log)
        {
            log.LogInformation($"TwitchStreamEventHandler processing: {StreamEvent.UserName} type {StreamEvent.Type} started at {StreamEvent.StartedAt}");

            if (StreamEvent.Type != "live")
            { 
                log.LogInformation($"Processing event skipped. type: {StreamEvent.Type}");
                return;
            }


            string username;
            if (string.IsNullOrWhiteSpace(StreamEvent.Subscription.TwitterName))
            {
                username = StreamEvent.UserName;
                log.LogInformation($"Stream username {username} will be used");
            }
            else
            {
                username = $"@{StreamEvent.Subscription.TwitterName}";
                log.LogInformation($"Twitter username {username} will be used");
            }

            string streamUri = $"https://twitch.tv/{StreamEvent.UserName}";
            log.LogInformation($"Stream Uri: {streamUri}");

            string myTweet = string.Format(TwitterTweetTemplate, streamUri, username, DateTime.UtcNow.ToString("u"));
            
            TwitterClient.PublishTweet(myTweet, log);
        }
    }
}
