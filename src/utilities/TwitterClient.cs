using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tweetinvi.Exceptions;
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

        public static async Task<ITweet> PublishTweet(string TweetMessage, ILogger log)
        {
            log.LogInformation($"PublishTweet Tweet: {TweetMessage}");

            if (TweetMessage.Length > MaxTweetLength)
            {
                log.LogWarning($"PublishTweet Tweet too long {TweetMessage.Length} max {MaxTweetLength}");
            }

            if (Environment.GetEnvironmentVariable(Utility.DISABLE_NOTIFICATIONS).ToLower() == "true")
            {
                log.LogInformation("PublishTweet Notifications are disabled. exiting");
                return null;
            }

            try
            {
                var tweetinvi = new Tweetinvi.TwitterClient(ConsumerKey, ConsumerSecret, AccessToken, AccessTokenSecret);
                var publishedTweet = await tweetinvi.Tweets.PublishTweetAsync(TweetMessage);
                log.LogInformation($"PublishTweet published tweet {publishedTweet.Id}");
                return publishedTweet;
            }
            catch (TwitterException e)
            {
                log.LogError($"Failed to tweet: {e.ToString()}");
            }
            catch (Exception e)
            {
                log.LogError($"Unhandled error when sending tweet: {e.ToString()}");
                throw;
            }
            return null;
        }
    }
}