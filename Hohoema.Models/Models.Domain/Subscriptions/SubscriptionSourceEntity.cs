using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Subscriptions
{
    public sealed class SubscriptionSourceEntity
    {
        [BsonId]
        public ObjectId Id { get; internal set; }

        public int SortIndex { get; set; }

        public string Label { get; set; }

        public SubscriptionSourceType SourceType { get; set; }

        public string SourceParameter { get; set; }

        public bool IsEnabled { get; set; } = true;

        public DateTime LastUpdateAt { get; set; } = DateTime.MinValue;
    }

    public enum SubscriptionSourceType
    {
        Mylist,
        User,
        Channel,
        Series,
        SearchWithKeyword,
        SearchWithTag,
    }
}
