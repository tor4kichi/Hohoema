using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Live;
using Hohoema.Models.Domain.Niconico.Live.LoginUser;
using Hohoema.Models.Helpers;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.NicoVideos;
using Hohoema.Presentation.Services;
using Hohoema.Presentation.ViewModels.Niconico.Live;
using I18NPortable;
using Prism.Commands;
using Prism.Navigation;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Pages.Niconico.Live
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
                        try
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

                                    ResetList();
                                })
                                );
                        }
                        catch (Exception e)
                        {
                            ErrorTrackingManager.TrackError(e);
                        }
                    }));
            }
        }

        protected override void PostResetList()
        {
            AddSortDescription(
                new Microsoft.Toolkit.Uwp.UI.SortDescription(nameof(LiveInfoListItemViewModel.IsOnair), Microsoft.Toolkit.Uwp.UI.SortDirection.Descending)
                );

            AddSortDescription(
                new Microsoft.Toolkit.Uwp.UI.SortDescription(nameof(LiveInfoListItemViewModel.IsPast), Microsoft.Toolkit.Uwp.UI.SortDirection.Ascending)
                );

            AddSortDescription(
                new Microsoft.Toolkit.Uwp.UI.SortDescription(nameof(LiveInfoListItemViewModel.IsReserved), Microsoft.Toolkit.Uwp.UI.SortDirection.Ascending)
                );

            AddSortDescription(
                new Microsoft.Toolkit.Uwp.UI.SortDescription(nameof(LiveInfoListItemViewModel.ExpiredAt), Microsoft.Toolkit.Uwp.UI.SortDirection.Ascending)
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

        protected override async ValueTask<int> ResetSourceImpl()
        {
            var reservations = await LiveReservationProvider.GetReservtionsAsync();

            _Reservations = reservations.ReservedProgram;

            _TimeshiftList = await LiveReservationProvider.GetTimeshiftListAsync();

            return reservations.ReservedProgram.Count;
        }

        protected override async IAsyncEnumerable<LiveInfoListItemViewModel> GetPagedItemsImpl(int head, int count, [EnumeratorCancellation] CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            // 生放送情報の詳細をローカルDBに確保
            // 既に存在する場合はスキップ
            foreach (var item in _Reservations.Skip(head).Take(count).ToArray())
            {
                var liveData = await NicoLiveProvider.GetLiveInfoAsync(item.Id);
                var tsItem = _TimeshiftList?.Items.FirstOrDefault(y => y.Id == item.Id);

                var liveInfoVM = new LiveInfoListItemViewModel(item.Id);
                liveInfoVM.ExpiredAt = (tsItem?.WatchTimeLimit ?? item.ExpiredAt).LocalDateTime;
                liveInfoVM.Setup(liveData.Data);

                liveInfoVM.SetReservation(item);

                yield return liveInfoVM;

                ct.ThrowIfCancellationRequested();
            }
        }

        
    }
}
