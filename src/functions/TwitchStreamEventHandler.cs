using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using TwitchLib.Webhook.Models;
using Microsoft.WindowsAzure.Storage.Table;
using Markekraus.TwitchStreamNotifications.Models;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;

namespace Markekraus.TwitchStreamNotifications
{
    public static class TwitchStreamEventHandler
    {
        [FunctionName("TwitchStreamEventHandler")]
        public static async Task Run(
            [QueueTrigger("%TwitchStreamActivity%", Connection = "TwitchStreamStorage")] Stream StreamEvent,
            [Queue("%TwitterNotifications%")] IAsyncCollector<Stream> TwitterQueue,
            [Queue("%DiscordNotifications%")] IAsyncCollector<Stream> DiscordQueue,
            [Table("%TwitchNotificationsTable%")] CloudTable NotificationsTable,
            ILogger log)
        {
            log.LogInformation($"TwitchStreamEventHandler processing: {StreamEvent.UserName} type {StreamEvent.Type} started at {StreamEvent.StartedAt}");

            var retrieveOperation = TableOperation.Retrieve<TwitchNotificationsEntry>(StreamEvent.UserName.ToLower(), StreamEvent.Id.ToLower());
            try
            {
                var retrievedResult = await NotificationsTable.ExecuteAsync(retrieveOperation);
                if (retrievedResult.Result != null)
                {
                    log.LogWarning($"Notifications for StreamName {StreamEvent.UserName} Id {StreamEvent.Id} have already been queued");
                    return;
                }
            }
            catch (StorageException e)
            {
                if (e.RequestInformation != null && e.RequestInformation.HttpStatusCode == 404)
                {
                    log.LogInformation($"Notifications for StreamName {StreamEvent.UserName} Id {StreamEvent.Id} have already not been queued");
                }
                else
                {
                    log.LogError(e, "Unkown Exception");
                    return;
                }
            }
            catch (Exception e)
            {
                log.LogError(e, "Unkown Exception");
                return;
            }

            var tableEntry = new TwitchNotificationsEntry(StreamEvent.UserName.ToLower(), StreamEvent.Id.ToLower());
            tableEntry.Date = DateTime.UtcNow;
            var insertOperation = TableOperation.Insert(tableEntry);
            try
            {
                await NotificationsTable.ExecuteAsync(insertOperation);
                log.LogInformation($"Add StreamName {StreamEvent.UserName} Id {StreamEvent.Id} to Table {NotificationsTable.Name}");
            }
            catch (Exception e)
            {
                log.LogWarning($"Notifications for StreamName {StreamEvent.UserName} Id {StreamEvent.Id} have already been queued???");
                log.LogError(e, "Exception");
                return;
            }

            log.LogInformation($"{nameof(TwitterQueue)} add {StreamEvent.UserName} type {StreamEvent.Type} started at {StreamEvent.StartedAt}");
            await TwitterQueue.AddAsync(StreamEvent);

            log.LogInformation($"{nameof(DiscordQueue)} add {StreamEvent.UserName} type {StreamEvent.Type} started at {StreamEvent.StartedAt}");
            await DiscordQueue.AddAsync(StreamEvent);

            log.LogInformation("TwitchStreamEventHandler complete");
        }
    }
}
