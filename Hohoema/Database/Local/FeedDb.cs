using System.Collections.Generic;
using System.Linq;
using LiteDB;

namespace Hohoema.Database
{
    public static class FeedDb
    {
        public static Feed Get(int id)
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            return db.Query<Feed>()
                .Include(x => x.Sources)
                .Where(x => x.Id == id)
                .SingleOrDefault();
        }

        public static List<Feed> GetAll()
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            return db.Query<Feed>()
                .Include(x => x.Sources)
                .ToList();
        }

        public static bool AddOrUpdate(Feed feedGroup)
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();

            // 未登録のBookmarkをDbに登録
            var notExistItems = feedGroup.Sources
                .Where(x => null == db.SingleOrDefault<Bookmark>(y => x.BookmarkType == y.BookmarkType && x.Content == y.Content));
                                
            db.Upsert(notExistItems);

            return db.Upsert(feedGroup);
        }

        public static bool Delete(Feed feedGroup)
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            return db.Delete<Feed>(feedGroup.Id);
        }
    }
}
