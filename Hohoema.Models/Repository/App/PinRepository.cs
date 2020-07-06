using Hohoema.Models.Pages;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.App
{
    public sealed class PinRepository : LiteDBServiceBase<Hohoema.Models.Pages.HohoemaPin>
    {
        public PinRepository(ILiteDatabase liteDatabase)
            : base(liteDatabase)
        { }

        public List<Hohoema.Models.Pages.HohoemaPin> GetSortedPins()
        {
            return _collection.FindAll()
                .OrderBy(x => x.SortIndex)
                .ToList();
        }
    }
}
