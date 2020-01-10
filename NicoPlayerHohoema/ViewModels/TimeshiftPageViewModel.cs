using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using NicoPlayerHohoema.Database;
using NicoPlayerHohoema.Models.Helpers;
using NicoPlayerHohoema.Models;
using Prism.Commands;
using Unity;
using NicoPlayerHohoema.Services.Helpers;
using NicoPlayerHohoema.Models.Provider;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections.ObjectModel;
using NicoPlayerHohoema.Services.Page;
using NicoPlayerHohoema.UseCase.Playlist;
using NicoPlayerHohoema.Services;

namespace NicoPlayerHohoema.ViewModels
{
    public sealed class TimeshiftPageViewModel : HohoemaListingPageViewModelBase<LiveInfoListItemViewModel>
    {
        public TimeshiftPageViewModel(
            LoginUserLiveReservationProvider loginUserLiveReservationProvider,
            NicoLiveProvider nicoLiveProvider,
            HohoemaPlaylist hohoemaPlaylist,
            NoUIProcessScreenContext noUIProcessScreenContext, 
            Services.DialogService dialogService
            ) 
        {
            LoginUserLiveReservationProvider = loginUserLiveReservationProvider;
            NicoLiveProvider = nicoLiveProvider;
            HohoemaPlaylist = hohoemaPlaylist;
            _noUIProcessScreenContext = noUIProcessScreenContext;
            DialogService = dialogService;
        }

        public LoginUserLiveReservationProvider LoginUserLiveReservationProvider { get; }
        public NicoLiveProvider NicoLiveProvider { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public Services.DialogService DialogService { get; }

        private DelegateCommand _DeleteOutdatedReservations;
        private readonly NoUIProcessScreenContext _noUIProcessScreenContext;

        public DelegateCommand DeleteOutdatedReservations 
        {
            get
            {
                return _DeleteOutdatedReservations
                    ?? (_DeleteOutdatedReservations = new DelegateCommand(async () => 
                    {
                        var reservations = await LoginUserLiveReservationProvider.GetReservtionsAsync();

                        var dateOutReservations = reservations.ReservedProgram.Where(x => x.IsOutDated).ToList();

                        if (dateOutReservations.Count == 0) { return; }

                        var reservationTitlesText = string.Join("\r", dateOutReservations.Select(x => x.Title));
                        var acceptDeletion = await DialogService.ShowMessageDialog(
                            "DeleteReservationConfirmText".ToCulturelizeString() + "\r\r" + reservationTitlesText,
                            "DeleteOutdatedReservationConfirm_Title".ToCulturelizeString(),
                            "DeleteReservationConfirm_Agree".ToCulturelizeString(),
                            "Cancel".ToCulturelizeString()
                            );

                        if (!acceptDeletion) { return; }

                        await _noUIProcessScreenContext.StartNoUIWork(
                            "DeletingReservations".ToCulturelizeString()
                            , dateOutReservations.Count,
                            () => AsyncInfo.Run<int>(async (cancelToken, progress) =>
                            {
                                int cnt = 0;
                                var token = await LoginUserLiveReservationProvider.GetReservationTokenAsync();

                                foreach (var reservation in dateOutReservations)
                                {
                                    await LoginUserLiveReservationProvider.DeleteReservationAsync(reservation.Id, token);

                                    await Task.Delay(TimeSpan.FromSeconds(1));

                                    cnt++;

                                    progress.Report(cnt);
                                }

                                await ResetList();
                            })
                            );
                    }));
            }
        }

        protected override void PostResetList()
        {
            AddSortDescription(
                new Microsoft.Toolkit.Uwp.UI.SortDescription("NowLive", Microsoft.Toolkit.Uwp.UI.SortDirection.Descending)
                );

            AddSortDescription(
                new Microsoft.Toolkit.Uwp.UI.SortDescription("IsTimedOut", Microsoft.Toolkit.Uwp.UI.SortDirection.Ascending)
                );

            AddSortDescription(
                new Microsoft.Toolkit.Uwp.UI.SortDescription("IsReserved", Microsoft.Toolkit.Uwp.UI.SortDirection.Ascending)
                );

            AddSortDescription(
                new Microsoft.Toolkit.Uwp.UI.SortDescription("ExpiredAt", Microsoft.Toolkit.Uwp.UI.SortDirection.Ascending)
                );

            (ItemsView.Source as ObservableCollection<LiveInfoListItemViewModel>).ObserveElementPropertyChanged()
                .Where(x => x.EventArgs.PropertyName == nameof(x.Sender.Reservation) && x.Sender.Reservation == null)
                .Subscribe(x =>
                {
                    ItemsView.Remove(x.Sender);
                });

            base.PostResetList();
        }

        protected override IIncrementalSource<LiveInfoListItemViewModel> GenerateIncrementalSource()
        {
            return new TimeshiftIncrementalCollectionSource(LoginUserLiveReservationProvider, NicoLiveProvider);
        }

        protected override bool TryGetHohoemaPin(out HohoemaPin pin)
        {
            pin = null;
            return false;
        }
    }



    public class TimeshiftIncrementalCollectionSource : HohoemaIncrementalSourceBase<LiveInfoListItemViewModel>
    {
        public TimeshiftIncrementalCollectionSource(LoginUserLiveReservationProvider liveReservationProvider, NicoLiveProvider nicoLiveProvider)
        {
            LiveReservationProvider = liveReservationProvider;
            NicoLiveProvider = nicoLiveProvider;
        }

        public override uint OneTimeLoadCount => 30;

        public NiconicoSession NiconicoSession { get; }
        public LoginUserLiveReservationProvider LiveReservationProvider { get; }
        public NicoLiveProvider NicoLiveProvider { get; }

        IReadOnlyList<Mntone.Nico2.Live.ReservationsInDetail.Program> _Reservations;
        Mntone.Nico2.Live.Reservation.MyTimeshiftListData _TimeshiftList;

        protected override async Task<int> ResetSourceImpl()
        {
            var reservations = await LiveReservationProvider.GetReservtionsAsync();

            _Reservations = reservations.ReservedProgram;

            _TimeshiftList = await LiveReservationProvider.GetTimeshiftListAsync();

            return reservations.ReservedProgram.Count;
        }

        protected override async Task<IAsyncEnumerable<LiveInfoListItemViewModel>> GetPagedItemsImpl(int head, int count)
        {
            var items = _Reservations.Skip(head).Take(count).ToArray();

            // 生放送情報の詳細をローカルDBに確保
            // 既に存在する場合はスキップ
            foreach (var item in items)
            {
                if (NicoLiveDb.Get(item.Id) == null)
                {
                    var res = await NicoLiveProvider.GetLiveInfoAsync(item.Id);

                    // チャンネル放送などで期限切れになると生放送情報が取れなくなるので
                    // 別APIで取得チャレンジ
                    if ((!res?.IsOK) ?? true)
                    {
                        if (NicoLiveDb.Get(item.Id) == null)
                        {
                            await NicoLiveProvider.GetLiveProgramInfoAsync(item.Id);
                        }
                    }
                }
            }

            return
                items
                .Select(x =>
                {
                    var liveData = NicoLiveDb.Get(x.Id);
                    var tsItem = _TimeshiftList?.Items.FirstOrDefault(y => y.Id == x.Id);

                    var liveInfoVM = new LiveInfoListItemViewModel(x.Id);
                    liveInfoVM.ExpiredAt = tsItem?.WatchTimeLimit ?? x.ExpiredAt;
                    liveInfoVM.Setup(liveData);

                    liveInfoVM.SetReservation(x);

                    return liveInfoVM;
                }
                )
                .ToAsyncEnumerable();
                
        }

        
    }
}
