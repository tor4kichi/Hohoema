using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Database.Temporary
{
    // Note: ログインユーザーのマイリストアイテムを操作するためのアイテムIDを保存する

    // マイリストと動画IDの複合キーで

    public class MylistItemIdContainer
    {
        [BsonField]
        public string MylistGroupId { get; set; }

        [BsonField]
        public string VideoId { get; set; }
        
        [BsonId]
        public string ItemId { get; set; }
    }

    public static class MylistDb
    {
        static MylistDb()
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            var col = db.Database.GetCollection<MylistItemIdContainer>();
            col.EnsureIndex(x => x.VideoId);
            col.EnsureIndex(x => x.MylistGroupId);
        }

        static public void Clear(string mylistGroupId)
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            db.DeleteMany<MylistItemIdContainer>((x) => x.MylistGroupId == mylistGroupId);
        }

        static public void Clear()
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            db.DeleteMany<MylistItemIdContainer>(x => true);
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
            return db.Fetch<MylistItemIdContainer>(x => x.MylistGroupId == mylistGroupId && s.Contains(x.VideoId));
        }

        static public MylistItemIdContainer GetItemId(string mylistGroupId, string videoId)
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            return db.SingleOrDefault<MylistItemIdContainer>(x => x.VideoId == videoId && x.MylistGroupId == mylistGroupId);
        }

        static public bool RemoveItemId(MylistItemIdContainer item)
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            return db.DeleteMany<MylistItemIdContainer>(x => x.VideoId == item.VideoId && x.MylistGroupId == item.MylistGroupId) > 0;
        }

    }
}
