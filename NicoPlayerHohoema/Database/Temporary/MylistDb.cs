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


    public class MylistItemIdContainer
    {
        public string MylistGroupId { get; set; }

        public string VideoId { get; set; }

        public string ItemId { get; set; }
    }

    public static class MylistDb
    {
        static MylistDb()
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            var col = db.Database.GetCollection<MylistItemIdContainer>();
            col.EnsureIndex(x => x.MylistGroupId);
            col.EnsureIndex(x => x.VideoId);
        }

        static public void Clear(string mylistGroupId)
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            db.Delete<MylistItemIdContainer>((x) => x.MylistGroupId == mylistGroupId);
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
            return db.Fetch<MylistItemIdContainer>(x => x.MylistGroupId == mylistGroupId && s.Contains(x.VideoId));
        }

        static public MylistItemIdContainer GetItemId(string mylistGroupId, string videoIds)
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            return db.SingleOrDefault<MylistItemIdContainer>(x => x.MylistGroupId == mylistGroupId && x.VideoId == videoIds);
        }

        static public bool RemoveItemId(MylistItemIdContainer item)
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            return db.Delete<MylistItemIdContainer>(x => x.VideoId == item.VideoId && x.MylistGroupId == item.MylistGroupId) > 0;
        }

    }
}
