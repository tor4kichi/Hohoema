using Hohoema.Models.Niconico;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.Ichiba
{
    public sealed class NicoIchibaRepository
    {
        private readonly NiconicoSession _niconicoSession;

        public NicoIchibaRepository(NiconicoSession niconicoSession)
        {
            _niconicoSession = niconicoSession;
        }

        public async Task<IchibaResponse> GetIchibaAsync(string contentId)
        {
            var ichibaResponse = await _niconicoSession.Context.Embed.GetIchiba(contentId);
            return new IchibaResponse(ichibaResponse);
        }
    }

    public class IchibaResponse
    {
        private Mntone.Nico2.Embed.Ichiba.IchibaResponse _ichibaResponse;

        public IchibaResponse(Mntone.Nico2.Embed.Ichiba.IchibaResponse ichibaResponse)
        {
            _ichibaResponse = ichibaResponse;
        }

        public string Pickup { get; set; }

        public string Main { get; set; }

        Polling _Polling;
        public Polling Polling => _Polling ??= new Polling(_ichibaResponse.Polling);

        List<IchibaItem> _MainItems;
        public IReadOnlyList<IchibaItem> MainItems => _MainItems ??= _ichibaResponse.GetMainIchibaItems().Select(x => new IchibaItem(x)).ToList();

        List<IchibaItem> _PickupItems;
        public IReadOnlyList<IchibaItem> PickupItems => _PickupItems ??= _ichibaResponse.GetPickupIchibaItems().Select(x => new IchibaItem(x)).ToList();


    }

    public sealed class Polling
    {
        private Mntone.Nico2.Embed.Ichiba.Polling _polling;

        public Polling(Mntone.Nico2.Embed.Ichiba.Polling polling)
        {
            _polling = polling;
        }

        public int ShortIntarval => _polling.ShortIntarval;

        public int LongIntarval => _polling.LongIntarval;

        public int DefaultIntarval => _polling.DefaultIntarval;

        public int MaxNoChangeCount => _polling.MaxNoChangeCount;
    }

    public class IchibaItem
    {
        private readonly Mntone.Nico2.Embed.Ichiba.IchibaItem _ichibaItem;

        public IchibaItem(Mntone.Nico2.Embed.Ichiba.IchibaItem ichibaItem)
        {
            _ichibaItem = ichibaItem;
        }

        public string Id => _ichibaItem.Id;
        public string Title => _ichibaItem.Title;
        public Uri ThumbnailUrl => _ichibaItem.ThumbnailUrl;
        public Uri AmazonItemLink => _ichibaItem.AmazonItemLink;
        public string Maker => _ichibaItem.Maker;
        public string Price => _ichibaItem.Price;
        public string DiscountText => _ichibaItem.DiscountText;

        public Uri IchibaUrl => _ichibaItem.IchibaUrl;

        IchibaItemReservation _reservation;
        public IchibaItemReservation Reservation => _reservation ??= new IchibaItemReservation(_ichibaItem.Reservation);

        IchibaItemSell _sell;
        public IchibaItemSell Sell => _sell ??= new IchibaItemSell(_ichibaItem.Sell);
    }


    public class IchibaItemReservation : IchibaItemSellBase
    {
        private Mntone.Nico2.Embed.Ichiba.IchibaItemReservation _reservation;

        public IchibaItemReservation(Mntone.Nico2.Embed.Ichiba.IchibaItemReservation reservation)
            : base(reservation)
        {
            _reservation = reservation;
        }

        public string ReleaseDate => _reservation.ReleaseDate;
        public string ReservationActionText => _reservation.ReservationActionText;
        public string YesterdayReservationActionText => _reservation.YesterdayReservationActionText;

    }

    public class IchibaItemSell : IchibaItemSellBase
    {
        private Mntone.Nico2.Embed.Ichiba.IchibaItemSell _sell;

        public IchibaItemSell(Mntone.Nico2.Embed.Ichiba.IchibaItemSell sell)
            : base(sell)
        {
            _sell = sell;
        }

        public string BuyActionText => _sell.BuyActionText;
        public string YesterdayBuyActionText => _sell.YesterdayBuyActionText;

    }

    public class IchibaItemSellBase
    {
        private Mntone.Nico2.Embed.Ichiba.IchibaItemSellBase _reservation;

        public IchibaItemSellBase(Mntone.Nico2.Embed.Ichiba.IchibaItemSellBase reservation)
        {
            _reservation = reservation;
        }

        public string ClickActionText => _reservation.ClickActionText;
        public string ClickInThisContentText => _reservation.ClickInThisContentText;

    }
}
