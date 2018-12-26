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
        private readonly static string ConsumerKey = Environment.GetEnvironmentVariable("TwitterConsumerKey");
        private readonly static string ConsumerSecret = Environment.GetEnvironmentVariable("TwitterConsumerSecret");
        private readonly static string AccessToken = Environment.GetEnvironmentVariable("TwitterAccessToken");
        private readonly static string AccessTokenSecret = Environment.GetEnvironmentVariable("TwitterAccessTokenSecret");

        [FunctionName("TwitterEventHandler")]
        public static void Run([QueueTrigger("%TwitterNotifications%", Connection = "TwitchStreamStorage")]TwitchLib.Webhook.Models.Stream StreamEvent, ILogger log)
        {
            log.LogInformation($"TwitchStreamEventHandler processing: {StreamEvent.UserName} type {StreamEvent.Type} started at {StreamEvent.StartedAt}");

            if (StreamEvent.Type != "live")
            { 
                log.LogInformation($"Processing event skipped. type: {StreamEvent.Type}");
                return;
            }

            Auth.SetUserCredentials(ConsumerKey, ConsumerSecret, AccessToken, AccessTokenSecret);

            string myTweet = $"https://twitch.tv/{StreamEvent.UserName} {StreamEvent.UserName} is now streaming live!";

            if (myTweet.Length > 140)
            {
                log.LogWarning($"Tweet too long {myTweet.Length}");
            }

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
