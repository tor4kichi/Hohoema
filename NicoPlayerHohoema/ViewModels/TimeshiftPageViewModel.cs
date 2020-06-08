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
using NicoPlayerHohoema.UseCase;
using I18NPortable;
using Prism.Navigation;
using NicoPlayerHohoema.UseCase.NicoVideoPlayer.Commands;

namespace NicoPlayerHohoema.ViewModels
{
    public sealed class TimeshiftPageViewModel : HohoemaListingPageViewModelBase<LiveInfoListItemViewModel>, INavigatedAwareAsync
    {
        public TimeshiftPageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            LoginUserLiveReservationProvider loginUserLiveReservationProvider,
            NicoLiveProvider nicoLiveProvider,
            HohoemaPlaylist hohoemaPlaylist,
            NoUIProcessScreenContext noUIProcessScreenContext, 
            Services.DialogService dialogService,
            OpenLiveContentCommand openLiveContentCommand
            ) 
        {
            ApplicationLayoutManager = applicationLayoutManager;
            LoginUserLiveReservationProvider = loginUserLiveReservationProvider;
            NicoLiveProvider = nicoLiveProvider;
            HohoemaPlaylist = hohoemaPlaylist;
            _noUIProcessScreenContext = noUIProcessScreenContext;
            DialogService = dialogService;
            OpenLiveContentCommand = openLiveContentCommand;
        }

        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public LoginUserLiveReservationProvider LoginUserLiveReservationProvider { get; }
        public NicoLiveProvider NicoLiveProvider { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public Services.DialogService DialogService { get; }
        public OpenLiveContentCommand OpenLiveContentCommand { get; }

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
                            "DeleteReservationConfirmText".Translate() + "\r\r" + reservationTitlesText,
                            "DeleteOutdatedReservationConfirm_Title".Translate(),
                            "DeleteReservationConfirm_Agree".Translate(),
                            "Cancel".Translate()
                            );

                        if (!acceptDeletion) { return; }

                        await _noUIProcessScreenContext.StartNoUIWork(
                            "DeletingReservations".Translate()
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
            List<LiveInfoListItemViewModel> returnItems = new List<LiveInfoListItemViewModel>();
            foreach (var item in items)
            {
                var liveData = await NicoLiveProvider.GetLiveInfoAsync(item.Id);
                var tsItem = _TimeshiftList?.Items.FirstOrDefault(y => y.Id == item.Id);

                var liveInfoVM = new LiveInfoListItemViewModel(item.Id);
                liveInfoVM.ExpiredAt = tsItem?.WatchTimeLimit ?? item.ExpiredAt;
                liveInfoVM.Setup(liveData.Data);

                liveInfoVM.SetReservation(item);

                returnItems.Add(liveInfoVM);
            }

            return returnItems.ToAsyncEnumerable();
        }

        
    }
}
