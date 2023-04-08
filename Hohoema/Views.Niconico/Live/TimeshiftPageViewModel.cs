using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Live;
using Hohoema.Models.Niconico.Live.LoginUser;
using Hohoema.Helpers;
using Hohoema.Services;
using Hohoema.Services.Playlist;
using Hohoema.Services;
using Hohoema.ViewModels.Niconico.Live;
using I18NPortable;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Collections;
using NiconicoToolkit.Live.Timeshift;
using CommunityToolkit.Mvvm.Input;
using Hohoema.Services.Navigations;
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
using ZLogger;

namespace Hohoema.ViewModels.Pages.Niconico.Live
{
    public sealed class TimeshiftPageViewModel : HohoemaListingPageViewModelBase<LiveInfoListItemViewModel>
    {
        public TimeshiftPageViewModel(
            ILoggerFactory loggerFactory,
            ApplicationLayoutManager applicationLayoutManager,
            LoginUserLiveReservationProvider loginUserLiveReservationProvider,
            NicoLiveProvider nicoLiveProvider,
            NoUIProcessScreenContext noUIProcessScreenContext, 
            Services.DialogService dialogService,
            OpenLiveContentCommand openLiveContentCommand
            )
            : base(loggerFactory.CreateLogger<TimeshiftPageViewModel>())
        {
            ApplicationLayoutManager = applicationLayoutManager;
            LoginUserLiveReservationProvider = loginUserLiveReservationProvider;
            NicoLiveProvider = nicoLiveProvider;
            _noUIProcessScreenContext = noUIProcessScreenContext;
            DialogService = dialogService;
            OpenLiveContentCommand = openLiveContentCommand;
        }

        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public LoginUserLiveReservationProvider LoginUserLiveReservationProvider { get; }
        public NicoLiveProvider NicoLiveProvider { get; }
        public Services.DialogService DialogService { get; }
        public OpenLiveContentCommand OpenLiveContentCommand { get; }

        private RelayCommand _DeleteOutdatedReservations;
        private readonly NoUIProcessScreenContext _noUIProcessScreenContext;

        public RelayCommand DeleteOutdatedReservations 
        {
            get
            {
                return _DeleteOutdatedReservations
                    ?? (_DeleteOutdatedReservations = new RelayCommand(async () => 
                    {
                        try
                        {
                            var reservations = await LoginUserLiveReservationProvider.GetReservtionsDetailAsync();

                            var dateOutReservations = reservations.Data.Items.Where(x => x.IsOutDated).ToList();

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
                                    foreach (var reservation in dateOutReservations)
                                    {
                                        await LoginUserLiveReservationProvider.DeleteReservationAsync(reservation.LiveId);

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
                            _logger.ZLogError(e, "DeleteOutdatedReservations failed");
                            //ErrorTrackingManager.TrackError(e);
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

        protected override (int, IIncrementalSource<LiveInfoListItemViewModel>) GenerateIncrementalSource()
        {
            return (TimeshiftIncrementalCollectionSource.OneTimeLoadCount, new TimeshiftIncrementalCollectionSource(LoginUserLiveReservationProvider, NicoLiveProvider));
        }
    }



    public class TimeshiftIncrementalCollectionSource : IIncrementalSource<LiveInfoListItemViewModel>
    {
        public TimeshiftIncrementalCollectionSource(LoginUserLiveReservationProvider liveReservationProvider, NicoLiveProvider nicoLiveProvider)
        {
            LiveReservationProvider = liveReservationProvider;
            NicoLiveProvider = nicoLiveProvider;
        }

        public const int OneTimeLoadCount = 30;

        public NiconicoSession NiconicoSession { get; }
        public LoginUserLiveReservationProvider LiveReservationProvider { get; }
        public NicoLiveProvider NicoLiveProvider { get; }

        TimeshiftReservationsDetailResponse _Reservations;
        TimeshiftReservationsResponse _TimeshiftList;


        async Task<IEnumerable<LiveInfoListItemViewModel>> IIncrementalSource<LiveInfoListItemViewModel>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken ct)
        {
            _Reservations ??= await LiveReservationProvider.GetReservtionsDetailAsync();

            ct.ThrowIfCancellationRequested();

            // 生放送情報の詳細をローカルDBに確保
            // 既に存在する場合はスキップ
            var head = pageIndex * pageSize;
            List<LiveInfoListItemViewModel> items = new ();
            foreach (var item in _Reservations.Data.Items.Skip(head).Take(pageSize))
            {
                var liveData = await NicoLiveProvider.GetLiveInfoAsync(item.LiveId);
                //var tsItem = _TimeshiftList.Data.Items.FirstOrDefault(y => y.Id == item.LiveIdWithoutPrefix);

                var liveInfoVM = new LiveInfoListItemViewModel(item.LiveId);
                liveInfoVM.ExpiredAt = item.ExpiredAt?.LocalDateTime;
                liveInfoVM.Setup(liveData.Data);

                liveInfoVM.SetReservation(item);

                items.Add(liveInfoVM);

                ct.ThrowIfCancellationRequested();
            }

            return items;
        }
    }
}
