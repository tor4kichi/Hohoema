using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Database.Local.LocalMylist
{
    public sealed class LocalMylistData
    {
        [LiteDB.BsonId(autoId:true)]
        public string Id { get; set; }

        public string Label { get; set; }

        public List<string> Items { get; set; }

        public int SortIndex { get; set; }
    }

    public static class LocalMylistDb 
    {
        static public List<LocalMylistData> GetLocalMylistGroups()
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();

            return db.Fetch<LocalMylistData>();
        }

        static public LocalMylistData Get(string id)
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();

            return db.SingleById<LocalMylistData>(id);
        }

        static public void AddOrUpdate(LocalMylistData localMylist)
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();

            db.Upsert(localMylist);
        }

        static public bool Remove(LocalMylistData localMylist)
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            return db.Delete<LocalMylistData>(x => x.Id == localMylist.Id) > 0;
        }
    }
}
