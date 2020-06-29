using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Database
{
    public enum NicoVideoUserType
    {
        User,
        Channel,
    }

    public class NicoVideoOwner
    {
        [BsonId]
        public string OwnerId { get; set; }

        public string IconUrl { get; set; }

        public NicoVideoUserType UserType { get; set; }

        public string ScreenName { get; set; }
    }


    public static class NicoVideoOwnerDb
    {
        public static NicoVideoOwner Get(string id)
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            {
                return db.SingleOrDefault<NicoVideoOwner>(x => x.OwnerId == id);
            }
        }

        public static bool AddOrUpdate(NicoVideoOwner owner)
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            {
                return db.Upsert(owner);
            }
        }

        public static IEnumerable<NicoVideoOwner> SearchFromScreenName(string keyword)
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            {
                return db.Fetch<NicoVideoOwner>(Query.Contains(nameof(NicoVideoOwner.ScreenName), keyword));
            }
        }

        public static int Delete(Expression<Func<NicoVideoOwner, bool>> expression)
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            {
                return db.DeleteMany(expression);
            }
        }
    }
}
