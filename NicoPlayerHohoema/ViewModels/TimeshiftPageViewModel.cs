using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mntone.Nico2;
using Mntone.Nico2.Live.ReservationsInDetail;
using NicoPlayerHohoema.Database;
using NicoPlayerHohoema.Helpers;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models;
using Prism.Commands;
using Prism.Windows.Navigation;
using Windows.Foundation;
using Microsoft.Practices.Unity;

namespace NicoPlayerHohoema.ViewModels
{
    public sealed class TimeshiftPageViewModel : HohoemaListingPageViewModelBase<LiveInfoListItemViewModel>
    {
        public TimeshiftPageViewModel(HohoemaApp app, PageManager pageManager) 
            : base(app, pageManager, useDefaultPageTitle: true)
        {
            
        }


        private DelegateCommand _DeleteOutdatedReservations;
        public DelegateCommand DeleteOutdatedReservations 
        {
            get
            {
                return _DeleteOutdatedReservations
                    ?? (_DeleteOutdatedReservations = new DelegateCommand(async () => 
                    {
                        var reservations = await HohoemaApp.ContentProvider.Context.Live.GetReservationsInDetailAsync();

                        var dateOutReservations = reservations.ReservedProgram.Where(x => x.IsOutDated).ToList();

                        if (dateOutReservations.Count == 0) { return; }

                        var dialogService = App.Current.Container.Resolve<Services.DialogService>();

                        var reservationTitlesText = string.Join("\r", dateOutReservations.Select(x => x.Title));
                        var acceptDeletion = await dialogService.ShowMessageDialog(
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
                                var token = await HohoemaApp.NiconicoContext.Live.GetReservationTokenAsync();

                                foreach (var reservation in dateOutReservations)
                                {
                                    await HohoemaApp.NiconicoContext.Live.DeleteReservationAsync(reservation.Id, token);

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
                        var reservations = await HohoemaApp.ContentProvider.Context.Live.GetReservationsInDetailAsync();

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
                                var token = await HohoemaApp.NiconicoContext.Live.GetReservationTokenAsync();

                                foreach (var reservation in selectedReservations)
                                {
                                    await HohoemaApp.NiconicoContext.Live.DeleteReservationAsync(reservation.Id, token);

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
            return new TimeshiftIncrementalCollectionSource(HohoemaApp.ContentProvider);
        }
    }



    public class TimeshiftIncrementalCollectionSource : HohoemaIncrementalSourceBase<LiveInfoListItemViewModel>
    {
        NiconicoContentProvider _ContentProvider;

        IReadOnlyList<Mntone.Nico2.Live.ReservationsInDetail.Program> _Reservations;


        public override uint OneTimeLoadCount => 30;

        Mntone.Nico2.Live.Reservation.MyTimeshiftListData _TimeshiftList;

        public TimeshiftIncrementalCollectionSource(NiconicoContentProvider contentProvider)
        {
            _ContentProvider = contentProvider;
        }

        protected override async Task<int> ResetSourceImpl()
        {
            var reservations = await _ContentProvider.Context.Live.GetReservationsInDetailAsync();

            _Reservations = reservations.ReservedProgram;

            _TimeshiftList = await _ContentProvider.Context.Live.GetMyTimeshiftListAsync();

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
                    var res = await _ContentProvider.GetLiveInfoAsync(item.Id);

                    // チャンネル放送などで期限切れになると生放送情報が取れなくなるので
                    // 別APIで取得チャレンジ
                    if ((!res?.IsOK) ?? true)
                    {
                        if (NicoLiveDb.Get(item.Id) == null)
                        {
                            await _ContentProvider.GetLiveProgramInfoAsync(item.Id);
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

                    return new LiveInfoListItemViewModel(liveData, x)
                    {
                        ExpiredAt = tsItem?.WatchTimeLimit ?? x.ExpiredAt,
                    };
                }
                )
                .ToAsyncEnumerable();
                
        }

        
    }
}
