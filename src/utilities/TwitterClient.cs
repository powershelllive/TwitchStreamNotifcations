using System;
using Microsoft.Extensions.Logging;
using Tweetinvi;
using Tweetinvi.Models;

namespace Markekraus.TwitchStreamNotifications
{
    public static class TwitterClient
    {
        private readonly static string ConsumerKey = Environment.GetEnvironmentVariable("TwitterConsumerKey");
        private readonly static string ConsumerSecret = Environment.GetEnvironmentVariable("TwitterConsumerSecret");
        private readonly static string AccessToken = Environment.GetEnvironmentVariable("TwitterAccessToken");
        private readonly static string AccessTokenSecret = Environment.GetEnvironmentVariable("TwitterAccessTokenSecret");
        private readonly static string TwitterTweetTemplate = Environment.GetEnvironmentVariable("TwitterTweetTemplate");
        public const int MaxTweetLength = 280;

        public static ITweet PublishTweet(string TweetMessage, ILogger log)
        {
            log.LogInformation($"PublishTweet Tweet: {TweetMessage}");

            if (TweetMessage.Length > MaxTweetLength)
            {
                log.LogWarning($"PublishTweet Tweet too long {TweetMessage.Length} max {MaxTweetLength}");
            }

            if(Environment.GetEnvironmentVariable(Utility.DISABLE_NOTIFICATIONS).ToLower() == "true") {
                log.LogInformation("PublishTweet Notifications are disabled. exiting");
                return null;
            }

            Auth.SetUserCredentials(ConsumerKey, ConsumerSecret, AccessToken, AccessTokenSecret);
            var publishedTweet = Tweet.PublishTweet(TweetMessage);
            // by default TweetInvi doesn't throw exceptions: https://github.com/linvi/tweetinvi/wiki/Exception-Handling
            if (publishedTweet == null)
            {
                log.LogError($"PublishTweet Failed to publish");
            }
            else
            {
                log.LogInformation($"PublishTweet Published tweet {publishedTweet.Id}");
            }
            return publishedTweet;
        }
    }
}