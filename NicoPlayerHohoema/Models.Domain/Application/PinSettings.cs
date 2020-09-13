using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Infrastructure;
using Hohoema.Presentation.Services.Page;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Application
{
    public sealed class PinSettings : LiteDBServiceBase<HohoemaPin>
    {
        public PinSettings(LiteDB.LiteDatabase liteDatabase) : base(liteDatabase)
        {
        }

        HohoemaPin CreatePin(string label, HohoemaPageType pageType, string parameter)
        {
            var sortIndex = _collection.Max(x => x.SortIndex) ?? 0;

            return CreateItem(new Domain.PageNavigation.HohoemaPin()
            {
                Label = label,
                Parameter = parameter,
                PageType = pageType,
                SortIndex = sortIndex + 1
            });
        }
    }
}
