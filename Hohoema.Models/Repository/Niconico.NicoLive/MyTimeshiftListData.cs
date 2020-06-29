using System;
using System.Collections.Generic;
using System.Linq;

namespace Hohoema.Models.Repository.Niconico.NicoLive
{
    public sealed class MyTimeshiftListData
    {
        private readonly Mntone.Nico2.Live.Reservation.MyTimeshiftListData _myTimeshiftListData;

        internal MyTimeshiftListData(Mntone.Nico2.Live.Reservation.MyTimeshiftListData myTimeshiftListData)
        {
            _myTimeshiftListData = myTimeshiftListData;
        }

        public string Token => _myTimeshiftListData.Token;

        private List<MyTimeshiftListItem> _Items;

        public IReadOnlyList<MyTimeshiftListItem> Items => _Items ??= _myTimeshiftListData.Items.Select(x => new MyTimeshiftListItem(x)).ToList();



        public sealed class MyTimeshiftListItem
        {
            private readonly Mntone.Nico2.Live.Reservation.MyTimeshiftListItem _myTimeshiftListItem;

            internal MyTimeshiftListItem(Mntone.Nico2.Live.Reservation.MyTimeshiftListItem myTimeshiftListItem)
            {
                _myTimeshiftListItem = myTimeshiftListItem;
            }

            public DateTimeOffset? WatchTimeLimit => _myTimeshiftListItem.WatchTimeLimit;
            public bool IsCanWatch => IsCanWatch;
            public string Id => _myTimeshiftListItem.Id;

        }
    }

}
