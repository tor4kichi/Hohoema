using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Database.Temporary
{
    // Note: ログインユーザーのマイリストアイテムを操作するためのアイテムIDを保存する

    // マイリストと動画IDの複合キーで


    public class MylistItemKey : IEquatable<MylistItemKey>, IEqualityComparer<MylistItemKey>
    {
        public string MylistGroupId { get; set; }

        public string VideoId { get; set; }

        public bool Equals(MylistItemKey x, MylistItemKey y)
        {
            return x.MylistGroupId == y.MylistGroupId
                && x.VideoId == y.VideoId;
        }

        public bool Equals(MylistItemKey other)
        {
            return Equals(this, other);
        }

        public int GetHashCode(MylistItemKey obj)
        {
            return obj.MylistGroupId.GetHashCode() ^ obj.VideoId.GetHashCode();
        }
    }


    public class MylistItemIdContainer
    {
        [BsonId]
        public MylistItemKey Key { get; set; }

        public string ItemId { get; set; }
    }

    public static class MylistDb
    {
        static MylistDb()
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            var col = db.Database.GetCollection<MylistItemIdContainer>();
        }

        static public void Clear(string mylistGroupId)
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            db.Delete<MylistItemIdContainer>((x) => x.Key.MylistGroupId == mylistGroupId);
        }

        static public void Clear()
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            db.Delete<MylistItemIdContainer>(Query.All());
        }


        static public void AddItemId(MylistItemIdContainer item)
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            db.Upsert<MylistItemIdContainer>(item);
        }

        static public void AddItemId(IEnumerable<MylistItemIdContainer> items)
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            db.Upsert<MylistItemIdContainer>(items);
        }

        static public List<MylistItemIdContainer> GetItemIdList(string mylistGroupId, IEnumerable<string> videoIds)
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            HashSet<string> s = new HashSet<string>(videoIds);
            return db.Fetch<MylistItemIdContainer>(x => x.Key.MylistGroupId == mylistGroupId && s.Contains(x.Key.VideoId));
        }

        static public MylistItemIdContainer GetItemId(string mylistGroupId, string videoIds)
        {
            var otherKey = new MylistItemKey() { MylistGroupId = mylistGroupId, VideoId = videoIds };
            var db = HohoemaLiteDb.GetTempLiteRepository();
            return db.SingleOrDefault<MylistItemIdContainer>(x => x.Key == otherKey);
        }

        static public bool RemoveItemId(MylistItemIdContainer item)
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            return db.Delete<MylistItemIdContainer>(x => x.Key == item.Key) > 0;
        }

    }
}
