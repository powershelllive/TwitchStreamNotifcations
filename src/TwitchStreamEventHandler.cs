using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using TwitchLib.Webhook.Models;

namespace Markekraus.TwitchStreamNotifications
{
    public static class TwitchStreamEventHandler
    {
        [FunctionName("TwitchStreamEventHandler")]
        public static void Run(
            [QueueTrigger("%TwitchStreamActivity%", Connection = "TwitchStreamStorage")]Stream StreamEvent,
            [Queue("%TwitterNotifications%")] ICollector<Stream> TwitterQueue,
            ILogger log)
        {
            log.LogInformation($"TwitchStreamEventHandler processing: {StreamEvent.UserName} type {StreamEvent.Type} started at {StreamEvent.StartedAt}");

            log.LogInformation($"{nameof(TwitterQueue)} add {StreamEvent.UserName} type {StreamEvent.Type} started at {StreamEvent.StartedAt}");
            TwitterQueue.Add(StreamEvent);

            log.LogInformation("TwitchStreamEventHandler complete");
        }
    }
}
