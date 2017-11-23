using System.Collections.Generic;

namespace NicoPlayerHohoema.Database
{
    public static class FeedDb
    {
        public static Feed Get(int id)
        {
            var db = HohoemaLiteDb.GetLiteRepository();
            return db.Query<Feed>()
                .Include(x => x.Sources)
                .Where(x => x.Id == id)
                .SingleOrDefault();
        }

        public static List<Feed> GetAll()
        {
            var db = HohoemaLiteDb.GetLiteRepository();
            return db.Query<Feed>()
                .Include(x => x.Sources)
                .ToList();
        }

        public static bool AddOrUpdate(Feed feedGroup)
        {
            var db = HohoemaLiteDb.GetLiteRepository();
            return db.Upsert(feedGroup);
        }

        public static bool Delete(Feed feedGroup)
        {
            var db = HohoemaLiteDb.GetLiteRepository();
            return db.Delete<Feed>(feedGroup.Id);
        }
    }
}
