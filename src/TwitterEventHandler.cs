using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Tweetinvi;
using TwitchLib.Webhook.Models;

namespace Markekraus.TwitchStreamNotifications
{
    // Borrowing from https://markheath.net/post/randomly-scheduled-tweets-azure-functions
    public static class TwitterEventHandler
    {
        private const string DISABLE_NOTIFICATIONS = "DISABLE_NOTIFICATIONS";
        private readonly static string ConsumerKey = Environment.GetEnvironmentVariable("TwitterConsumerKey");
        private readonly static string ConsumerSecret = Environment.GetEnvironmentVariable("TwitterConsumerSecret");
        private readonly static string AccessToken = Environment.GetEnvironmentVariable("TwitterAccessToken");
        private readonly static string AccessTokenSecret = Environment.GetEnvironmentVariable("TwitterAccessTokenSecret");
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
            if (string.IsNullOrWhiteSpace(StreamEvent.TwitterName))
            {
                username = StreamEvent.UserName;
                log.LogInformation($"Stream username {username} will be used");
            }
            else
            {
                username = $"@{StreamEvent.TwitterName}";
                log.LogInformation($"Twitter username {username} will be used");
            }

            string streamUri = $"https://twitch.tv/{StreamEvent.UserName}";
            log.LogInformation($"Stream Uri: {streamUri}");

            string myTweet = string.Format(TwitterTweetTemplate, streamUri, username, DateTime.UtcNow.ToString("u"));
            log.LogInformation($"Tweet: {myTweet}");

            if (myTweet.Length > 280)
            {
                log.LogWarning($"Tweet too long {myTweet.Length}");
            }

            if(Environment.GetEnvironmentVariable(DISABLE_NOTIFICATIONS).ToLower() == "true") {
                log.LogInformation("Notifications are disabled. exiting");
                return;
            }

            Auth.SetUserCredentials(ConsumerKey, ConsumerSecret, AccessToken, AccessTokenSecret);
            var publishedTweet = Tweet.PublishTweet(myTweet);
            // by default TweetInvi doesn't throw exceptions: https://github.com/linvi/tweetinvi/wiki/Exception-Handling
            if (publishedTweet == null)
            {
                log.LogError($"Failed to publish");
            }
            else
            {
                log.LogInformation($"Published tweet {publishedTweet.Id}");
            }
        }
    }
}
