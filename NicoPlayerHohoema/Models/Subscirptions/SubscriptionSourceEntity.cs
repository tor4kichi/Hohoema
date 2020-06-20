using LiteDB;
using NicoPlayerHohoema.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Subscriptions
{
    public sealed class SubscriptionSourceEntity
    {
        [BsonId]
        public ObjectId Id { get; internal set; }

        public string Label { get; set; }

        public SubscriptionSourceType SourceType { get; set; }

        public string SourceParameter { get; set; }

        public bool IsEnabled { get; set; } = true;
    }

    public enum SubscriptionSourceType
    {
        Mylist,
        User,
        Channel,
        Series,
        Search,
    }
}
