using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Database.Local.Subscription
{
    public class SubsciptionSourceData
    {
        [BsonId(autoId:true)]
        public int Id { get; set; }

        public string Label { get; set; }
        public Models.Subscription.SubscriptionSourceType SourceType { get; set; }
        public string Parameter { get; set; }
    }

    public class SubscriptionDestinationData
    {
        [BsonId(autoId: true)]
        public int Id { get; set; }

        public string Label { get; set; }
        public string PlaylistId { get; set; }
        public Models.Subscription.SubscriptionDestinationTarget Target { get; set; }
    }

    internal class SubscriptionData
    {
        [BsonId]
        public Guid Id { get; set; }

        public string Label { get; set; }

        public int Order { get; set; }

        public List<SubsciptionSourceData> Sources { get; set; }

        public List<SubscriptionDestinationData> Destinations { get; set; }

        public string DoNotNoticeKeyword { get; set; }
        public bool DoNotNoticeKeywordAsRegex { get; set; }
    }

    public static class SubscriptionDb
    {
        public static IEnumerable<Models.Subscription.Subscription> GetOrderedSubscriptions()
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            var items = db.Fetch<SubscriptionData>()
                .OrderBy(x => x.Order)
                .Select(x =>
                {
                    var subsc = new Models.Subscription.Subscription(
                        x.Id,
                        x.Label
                        );

                    foreach (var source in x.Sources ?? Enumerable.Empty<SubsciptionSourceData>())
                    {
                        subsc.Sources.Add(new Models.Subscription.SubscriptionSource(source.Label, source.SourceType, source.Parameter));
                    }

                    foreach (var dest in x.Destinations ?? Enumerable.Empty<SubscriptionDestinationData>())
                    {
                        subsc.Destinations.Add(new Models.Subscription.SubscriptionDestination(dest.Label, dest.Target, dest.PlaylistId));
                    }

                    subsc.DoNotNoticeKeyword = x.DoNotNoticeKeyword;
                    subsc.DoNotNoticeKeywordAsRegex = x.DoNotNoticeKeywordAsRegex;

                    return subsc;
                }
                )
                .ToArray();

            return items;
        }

        public static void AddOrUpdateSubscription(Models.Subscription.Subscription subscription, int order)
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            var data = db.SingleOrDefault<SubscriptionData>(x => x.Id == subscription.Id)
                ?? new SubscriptionData();

            data.Id = subscription.Id == default(Guid) ? Guid.NewGuid() : subscription.Id;
            data.Label = subscription.Label;
            data.Order = order;
            data.Sources = subscription.Sources.Select(x => new SubsciptionSourceData()
            {
                Label = x.Label,
                SourceType = x.SourceType,
                Parameter = x.Parameter
            })
            .ToList();

            data.Destinations = subscription.Destinations.Select(x => new SubscriptionDestinationData()
            {
                Label = x.Label,
                Target = x.Target,
                PlaylistId = x.PlaylistId
            })
            .ToList();

            data.DoNotNoticeKeyword = subscription.DoNotNoticeKeyword;
            data.DoNotNoticeKeywordAsRegex = subscription.DoNotNoticeKeywordAsRegex;

            db.Upsert(data);
        }


        public static bool RemoveSubscription(Models.Subscription.Subscription subscription)
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            return db.Delete<SubscriptionData>(x => x.Id == subscription.Id) > 0;
        }
    }

    public class FeedResultSet
    {
        public Models.Subscription.SubscriptionSourceType SourceType { get; set; }

        public string Parameter { get; set; }

        public List<string> Items { get; set; }

        public List<string> NewItems { get; set; }

        public DateTimeOffset LastUpdated { get; set; }
    }

    public class SubscriptionFeedResult
    {
        [BsonId]
        public Guid SubscriptionId { get; set; }

        public List<FeedResultSet> FeedResultItems { get; set; }

        internal void AddOrUpdateFeedResultSet(Models.Subscription.SubscriptionSource source, IEnumerable<string> items)
        {
            FeedResultItems = FeedResultItems ?? new List<FeedResultSet>();
            var item = FeedResultItems.FirstOrDefault(x => x.SourceType == source.SourceType && x.Parameter == source.Parameter)
                ;

            if (item != null)
            {
                FeedResultItems.Remove(item);
            }
            else
            {
                item = new FeedResultSet() { SourceType = source.SourceType, Parameter = source.Parameter };
            }

            item.NewItems = items?.ToList() ?? new List<string>();
            item.Items = (item.Items ?? new List<string>()).Concat(item.NewItems).Distinct().ToList();
            item.LastUpdated = DateTimeOffset.Now;

            FeedResultItems.Add(item);
        }

        public FeedResultSet GetFeedResultSet(Models.Subscription.SubscriptionSource source)
        {
            return FeedResultItems?.FirstOrDefault(x => x.SourceType == source.SourceType && x.Parameter == source.Parameter)
                ;
        }
    }

    public static class SubscriptionFeedResultDb
    {
        public static SubscriptionFeedResult GetEnsureFeedResult(Models.Subscription.Subscription subscription)
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            var feedResult = db.SingleOrDefault<SubscriptionFeedResult>(x => x.SubscriptionId == subscription.Id);
            if (feedResult == null)
            {
                feedResult = new SubscriptionFeedResult()
                {
                    SubscriptionId = subscription.Id,
                    FeedResultItems = new List<FeedResultSet>()
                };
                db.Insert(feedResult);
            }

            return feedResult;
        }

        public static void AddOrUpdateFeedResult(Models.Subscription.Subscription subscription, Models.Subscription.SubscriptionSource source, IEnumerable<string> items)
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();

            var feedResult = db.SingleOrDefault<SubscriptionFeedResult>(x => x.SubscriptionId == subscription.Id)
                ?? new SubscriptionFeedResult() { SubscriptionId = subscription.Id }
                ;

            feedResult.AddOrUpdateFeedResultSet(source, items);

            db.Upsert(feedResult);
        }

        public static bool DeleteFeedResult(Models.Subscription.Subscription subscription)
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            return db.Delete<SubscriptionFeedResult>(x => x.SubscriptionId == subscription.Id) > 0;
        }
    }
}
