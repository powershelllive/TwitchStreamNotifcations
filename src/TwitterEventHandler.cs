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
        [FunctionName("TwitterEventHandler")]
        public static void Run([QueueTrigger("%TwitterNotifications%", Connection = "TwitchStreamStorage")]TwitchLib.Webhook.Models.Stream StreamEvent, ILogger log)
        {
            if (StreamEvent.Type != "live") { return; }

            log.LogInformation($"TwitchStreamEventHandler processing: {StreamEvent.UserName} type {StreamEvent.Type} started at {StreamEvent.StartedAt}");
            var consumerKey = Environment.GetEnvironmentVariable("TwitterConsumerKey");
            var consumerSecret = Environment.GetEnvironmentVariable("TwitterConsumerSecret");
            var accessToken = Environment.GetEnvironmentVariable("TwitterAccessToken");
            var accessTokenSecret = Environment.GetEnvironmentVariable("TwitterAccessTokenSecret");
            Auth.SetUserCredentials(consumerKey, consumerSecret, accessToken, accessTokenSecret);

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
