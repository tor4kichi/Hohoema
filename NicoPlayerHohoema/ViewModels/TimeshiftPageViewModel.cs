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
using Prism.Windows.Navigation;
using Microsoft.Practices.Unity;
using NicoPlayerHohoema.Services.Helpers;
using NicoPlayerHohoema.Models.Provider;

namespace NicoPlayerHohoema.ViewModels
{
    public sealed class TimeshiftPageViewModel : HohoemaListingPageViewModelBase<LiveInfoListItemViewModel>
    {
        public TimeshiftPageViewModel(
            LoginUserLiveReservationProvider loginUserLiveReservationProvider,
            NicoLiveProvider nicoLiveProvider,
            Services.HohoemaPlaylist hohoemaPlaylist,
            Services.PageManager pageManager, 
            Services.DialogService dialogService
            ) 
            : base(pageManager, useDefaultPageTitle: true)
        {
            LoginUserLiveReservationProvider = loginUserLiveReservationProvider;
            NicoLiveProvider = nicoLiveProvider;
            HohoemaPlaylist = hohoemaPlaylist;
            DialogService = dialogService;
        }

        public LoginUserLiveReservationProvider LoginUserLiveReservationProvider { get; }
        public NicoLiveProvider NicoLiveProvider { get; }
        public Services.HohoemaPlaylist HohoemaPlaylist { get; }
        public Services.DialogService DialogService { get; }

        private DelegateCommand _DeleteOutdatedReservations;
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

                        await PageManager.StartNoUIWork(
                            "DeletingReservations".ToCulturelizeString()
                            , dateOutReservations.Count,
                            () => AsyncInfo.Run<uint>(async (cancelToken, progress) =>
                            {
                                uint cnt = 0;
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

        private DelegateCommand _DeleteSelectedReservations;
        public DelegateCommand DeleteSelectedReservations
        {
            get
            {
                return _DeleteSelectedReservations
                    ?? (_DeleteSelectedReservations = new DelegateCommand(async () =>
                    {
                        var reservations = await LoginUserLiveReservationProvider.GetReservtionsAsync();

                        var selectedReservations = SelectedItems.ToList();

                        if (selectedReservations.Count == 0) { return; }

                        var dialogService = App.Current.Container.Resolve<Services.DialogService>();

                        var reservationTitlesText = string.Join("\r", selectedReservations.Select(x => x.Label));
                        var acceptDeletion = await dialogService.ShowMessageDialog(
                            "DeleteReservationConfirmText".ToCulturelizeString() + "\r\r" + reservationTitlesText,
                            "DeleteSelectedReservationConfirm_Title".ToCulturelizeString(),
                            "DeleteReservationConfirm_Agree".ToCulturelizeString(),
                            "Cancel".ToCulturelizeString()
                            );

                        if (!acceptDeletion) { return; }

                        await PageManager.StartNoUIWork(
                            "DeletingReservations".ToCulturelizeString()
                            , selectedReservations.Count,
                            () => AsyncInfo.Run<uint>(async (cancelToken, progress) =>
                            {
                                uint cnt = 0;
                                var token = await LoginUserLiveReservationProvider.GetReservationTokenAsync();

                                foreach (var reservation in selectedReservations)
                                {
                                    await LoginUserLiveReservationProvider.DeleteReservationAsync(reservation.LiveId, token);

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


        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            base.OnNavigatedTo(e, viewModelState);
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

            base.PostResetList();
        }

        protected override IIncrementalSource<LiveInfoListItemViewModel> GenerateIncrementalSource()
        {
            return new TimeshiftIncrementalCollectionSource(LoginUserLiveReservationProvider, NicoLiveProvider);
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

                    var liveInfoVM = App.Current.Container.Resolve<LiveInfoListItemViewModel>();
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
