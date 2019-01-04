using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace Markekraus.TwitchStreamNotifications.Models
{
    public class TwitchNotificationsEntry : TableEntity
    {
        public TwitchNotificationsEntry(string StreamName, string Id)
        {
            this.PartitionKey = StreamName;
            this.RowKey = Id;
        }

        public TwitchNotificationsEntry() { }

        public DateTime Date { get; set; }
    }
}
