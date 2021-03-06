﻿using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Infrastructure;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Pins
{
    public sealed class PinSettings : LiteDBServiceBase<HohoemaPin>
    {
        public PinSettings(LiteDB.LiteDatabase liteDatabase) : base(liteDatabase)
        {
            _collection.EnsureIndex(nameof(HohoemaPin.PageType));
            _collection.EnsureIndex(nameof(HohoemaPin.Parameter));
        }

        public bool HasPin(HohoemaPageType pageType, string parameter)
        {
            return _collection.Exists(x => x.PageType == pageType && x.Parameter == parameter);
        }

        public void RemovePin(HohoemaPageType pageType, string parameter)
        {
            _collection.Delete(x => x.PageType == pageType && x.Parameter == parameter);
        }


        HohoemaPin CreatePin(string label, HohoemaPageType pageType, string parameter)
        {
            var sortIndex = _collection.Max(x => x.SortIndex) ?? 0;

            var pin = new HohoemaPin()
            {
                Label = label,
                Parameter = parameter,
                PageType = pageType,
                SortIndex = sortIndex + 1
            };
            CreateItem(pin);

            return pin;
        }
    }
}
