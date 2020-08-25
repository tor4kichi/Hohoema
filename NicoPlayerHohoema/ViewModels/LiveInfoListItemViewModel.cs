using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mntone.Nico2.Live;
using Mntone.Nico2.Live.Reservation;
using Mntone.Nico2.Searches.Live;
using NicoPlayerHohoema.Database;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Services;
using Prism.Commands;
using Unity;
using NicoPlayerHohoema.Services.Helpers;
using Prism.Unity;
using NicoPlayerHohoema.UseCase.Playlist;
using I18NPortable;
using NicoPlayerHohoema.UseCase.NicoVideoPlayer.Commands;
using Mntone.Nico2;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Threading;

namespace NicoPlayerHohoema.ViewModels
{
    public class LiveInfoListItemViewModel : HohoemaListingPageItemBase, Interfaces.ILiveContent, Views.Extensions.ListViewBase.IDeferInitialize
    {
        public LiveInfoListItemViewModel(string liveId)
        {
            LiveId = liveId;
            PageManager = App.Current.Container.Resolve<Services.PageManager>();
            ExternalAccessService = App.Current.Container.Resolve<Services.ExternalAccessService>();
            OpenLiveContentCommand = App.Current.Container.Resolve<OpenLiveContentCommand>();
            _niconicoSession = App.Current.Container.Resolve<Models.NiconicoSession>();
        }

        public PageManager PageManager { get; }
        public ExternalAccessService ExternalAccessService { get; }
        public OpenLiveContentCommand OpenLiveContentCommand { get; }

        private readonly Models.NiconicoSession _niconicoSession;

        public Mntone.Nico2.Live.ReservationsInDetail.Program Reservation { get; private set; }


        public string LiveId { get; }

		public string CommunityName { get; protected set; }
		public string CommunityThumbnail { get; protected set; }
		public string CommunityGlobalId { get; protected set; }
		public Mntone.Nico2.Live.CommunityType CommunityType { get; protected set; }

		public string LiveTitle { get; protected set; }
		public int ViewCounter { get; protected set; }
		public int CommentCount { get; protected set; }
		public DateTimeOffset OpenTime { get; private set; }
		public DateTimeOffset StartTime { get; private set; }
		public bool HasEndTime { get; private set; }
		public DateTimeOffset EndTime { get; private set; }
		public string DurationText { get; private set; }
		public bool IsTimeshiftEnabled { get; private set; }
		public bool IsCommunityMemberOnly { get; private set; }

        public bool IsXbox => Services.Helpers.DeviceTypeHelper.IsXbox;



        public bool NowLive => Elements.Any(x => x == LiveContentElement.Status_Open || x == LiveContentElement.Status_Start);

        public bool IsReserved => Elements.Any(x => x == LiveContentElement.Timeshift_Preserved || x == LiveContentElement.Timeshift_Watch);
        public bool IsTimedOut => Elements.Any(x => x == LiveContentElement.Timeshift_OutDated);

        public ObservableCollection<LiveContentElement> Elements { get; } = new ObservableCollection<LiveContentElement>();
        public DateTimeOffset ExpiredAt { get; internal set; }
        public Mntone.Nico2.Live.ReservationsInDetail.ReservationStatus? ReservationStatus { get; internal set; }


        string ILiveContent.ProviderId => CommunityGlobalId;

        string ILiveContent.ProviderName => CommunityName;

        CommunityType ILiveContent.ProviderType => CommunityType;

        string INiconicoObject.Id => LiveId;

        string INiconicoObject.Label => LiveTitle;




        bool Views.Extensions.ListViewBase.IDeferInitialize.IsInitialized { get; set; }
        
        Task Views.Extensions.ListViewBase.IDeferInitialize.DeferInitializeAsync(CancellationToken ct)
        {
            ResetElements();
            return Task.CompletedTask;
        }



        public void SetReservation(Mntone.Nico2.Live.ReservationsInDetail.Program reservationInfo)
        {
            Reservation = reservationInfo;
            ReservationStatus = NowLive ? null : reservationInfo?.GetReservationStatus();
            DeleteReservationCommand.RaiseCanExecuteChanged();
            AddReservationCommand.RaiseCanExecuteChanged();
        }

        public void Setup(Mntone.Nico2.Live.Recommend.LiveRecommendData liveVideoInfo)
        {
            CommunityThumbnail = liveVideoInfo.ThumbnailUrl;

            CommunityGlobalId = liveVideoInfo.DefaultCommunity;
            CommunityType = liveVideoInfo.ProviderType;

            LiveTitle = liveVideoInfo.Title;
            OpenTime = liveVideoInfo.OpenTime;
            StartTime = liveVideoInfo.StartTime;

            IsTimeshiftEnabled = false;
            //IsCommunityMemberOnly = liveVideoInfo.Video.CommunityOnly;

            AddImageUrl(CommunityThumbnail);

            //Description = $"来場者:{ViewCounter} コメ:{CommentCount}";

            switch (liveVideoInfo.CurrentStatus)
            {
                case Mntone.Nico2.Live.StatusType.Invalid:
                    break;
                case Mntone.Nico2.Live.StatusType.OnAir:
                    DurationText = $"{StartTime - DateTimeOffset.Now} 経過";
                    break;
                case Mntone.Nico2.Live.StatusType.ComingSoon:
                    DurationText = $"開始予定: {StartTime.LocalDateTime.ToString("g")}";
                    break;
                case Mntone.Nico2.Live.StatusType.Closed:
                    DurationText = $"放送終了";
                    break;
                default:
                    break;
            }

            OptionText = DurationText;

            var endTime = liveVideoInfo.CurrentStatus == StatusType.Closed ? DateTimeOffset.Now + TimeSpan.FromMinutes(60) : DateTime.MaxValue;
        }


        public void Setup(LiveSearchResultItem liveVideoInfo)
        {
            CommunityName = liveVideoInfo.CommunityText;
            CommunityThumbnail = liveVideoInfo.CommunityIcon ?? liveVideoInfo.ThumbnailUrl;

            CommunityGlobalId = liveVideoInfo.CommunityId?.ToString() ?? liveVideoInfo.ChannelId?.ToString() ?? string.Empty;
            CommunityType = liveVideoInfo.GetCommunityType();

            LiveTitle = liveVideoInfo.Title;
            ViewCounter = liveVideoInfo.ViewCounter ?? 0;
            CommentCount = liveVideoInfo.CommentCounter ?? 0;
            OpenTime = new DateTimeOffset(liveVideoInfo.OpenTime ?? DateTime.MinValue, TimeSpan.FromHours(9));
            StartTime = new DateTimeOffset(liveVideoInfo.StartTime ?? DateTime.MinValue, TimeSpan.FromHours(9));
            EndTime = new DateTimeOffset(liveVideoInfo.LiveEndTime ?? DateTime.MinValue, TimeSpan.FromHours(9));
            IsTimeshiftEnabled = liveVideoInfo.TimeshiftEnabled ?? false;
            IsCommunityMemberOnly = liveVideoInfo.MemberOnly ?? false;

            Label = liveVideoInfo.Title;
            AddImageUrl(CommunityThumbnail);

            Description = $"来場者:{ViewCounter} コメ:{CommentCount}";

            if (StartTime > DateTimeOffset.Now)
            {
                // 予約
                DurationText = $" 開始予定: {StartTime.LocalDateTime.ToString("g")}";
            }
            else if (EndTime > DateTimeOffset.Now)
            {
                var duration = DateTimeOffset.Now - StartTime;
                // 放送中
                if (duration.Hours > 0)
                {
                    DurationText = $"{duration.Hours}時間 {duration.Minutes}分 経過";
                }
                else
                {
                    DurationText = $"{duration.Minutes}分 経過";
                }
            }
            else
            {
                var duration = EndTime - StartTime;
                // 終了
                if (duration.Hours > 0)
                {
                    DurationText = $"{EndTime.ToString("g")} 終了（{duration.Hours}時間 {duration.Minutes}分）";
                }
                else
                {
                    DurationText = $"{EndTime.ToString("g")} 終了（{duration.Minutes}分）";
                }
            }

            OptionText = DurationText;
        }

        public void Setup(NicoLive liveData)
        {
            CommunityName = liveData.BroadcasterName;
            if (liveData.ThumbnailUrl != null)
            {
                CommunityThumbnail = liveData.ThumbnailUrl;
            }
            else
            {
                CommunityThumbnail = liveData.PictureUrl;
            }

            CommunityGlobalId = liveData.BroadcasterId;
            CommunityType = liveData.ProviderType;

            LiveTitle = liveData.Title;
            ViewCounter = liveData.ViewCount;
            CommentCount = liveData.CommentCount;
            OpenTime = liveData.OpenTime;
            StartTime = liveData.StartTime;
            EndTime = liveData.EndTime;
            IsTimeshiftEnabled = liveData.TimeshiftEnabled;
            IsCommunityMemberOnly = liveData.IsMemberOnly;

            Label = LiveTitle;
            AddImageUrl(CommunityThumbnail);

            Description = $"来場者:{ViewCounter} コメ:{CommentCount}";

            if (StartTime > DateTimeOffset.Now)
            {
                // 予約
                DurationText = $" 開始予定: {StartTime.LocalDateTime.ToString("g")}";
            }
            else if (EndTime > DateTimeOffset.Now)
            {
                var duration = DateTimeOffset.Now - StartTime;
                // 放送中
                if (duration.Hours > 0)
                {
                    DurationText = $"{duration.Hours}時間 {duration.Minutes}分 経過";
                }
                else
                {
                    DurationText = $"{duration.Minutes}分 経過";
                }
            }
            else
            {
                var duration = EndTime - StartTime;
                // 終了
                if (duration.Hours > 0)
                {
                    DurationText = $"{EndTime.LocalDateTime.ToString("g")} 終了（{duration.Hours}時間 {duration.Minutes}分）";
                }
                else
                {
                    DurationText = $"{EndTime.LocalDateTime.ToString("g")} 終了（{duration.Minutes}分）";
                }
            }

            OptionText = DurationText;
        }

        public void Setup(Mntone.Nico2.Nicocas.Live.NicoCasLiveProgramData liveData)
        {
            //CommunityName = data.;
            
            if (liveData.ThumbnailUrl != null)
            {
                CommunityThumbnail = liveData.ThumbnailUrl;
            }

            CommunityGlobalId = liveData.ProviderId;
            CommunityType = liveData.CommunityType;

            LiveTitle = liveData.Title;
            ViewCounter = liveData.Viewers;
            CommentCount = liveData.Comments;
            OpenTime = liveData.OnAirTime.BeginAt;
            StartTime = liveData.ShowTime.BeginAt;
            EndTime = liveData.ShowTime.EndAt;
            IsTimeshiftEnabled = liveData.Timeshift.Enabled;
            IsCommunityMemberOnly = liveData.IsMemberOnly;

            Label = LiveTitle;
            AddImageUrl(CommunityThumbnail);

            Description = $"来場者:{ViewCounter} コメ:{CommentCount}";

            if (StartTime > DateTimeOffset.Now)
            {
                // 予約
                DurationText = $" 開始予定: {StartTime.LocalDateTime.ToString("g")}";
            }
            else if (EndTime > DateTimeOffset.Now)
            {
                var duration = DateTimeOffset.Now - StartTime;
                // 放送中
                if (duration.Hours > 0)
                {
                    DurationText = $"{duration.Hours}時間 {duration.Minutes}分 経過";
                }
                else
                {
                    DurationText = $"{duration.Minutes}分 経過";
                }
            }
            else
            {
                var duration = EndTime - StartTime;
                // 終了
                if (duration.Hours > 0)
                {
                    DurationText = $"{EndTime.LocalDateTime.ToString("g")} 終了（{duration.Hours}時間 {duration.Minutes}分）";
                }
                else
                {
                    DurationText = $"{EndTime.LocalDateTime.ToString("g")} 終了（{duration.Minutes}分）";
                }
            }

            OptionText = DurationText;
        }

        private void ResetElements()
        {
            Elements.Clear();

            if (DateTimeOffset.Now < OpenTime)
            {
                Elements.Add(LiveContentElement.Status_Pending);
            }
            else if (OpenTime < DateTimeOffset.Now && DateTimeOffset.Now < StartTime)
            {
                Elements.Add(LiveContentElement.Status_Open);
            }
            else if (StartTime < DateTimeOffset.Now && DateTimeOffset.Now < EndTime)
            {
                Elements.Add(LiveContentElement.Status_Start);
            }
            else
            {
                Elements.Add(LiveContentElement.Status_Closed);
            }

            switch (CommunityType)
            {
                case Mntone.Nico2.Live.CommunityType.Official:
                    Elements.Add(LiveContentElement.Provider_Official);
                    break;
                case Mntone.Nico2.Live.CommunityType.Community:
                    Elements.Add(LiveContentElement.Provider_Community);
                    break;
                case Mntone.Nico2.Live.CommunityType.Channel:
                    Elements.Add(LiveContentElement.Provider_Channel);
                    break;
                default:
                    break;
            }

           
            if (IsCommunityMemberOnly)
            {
                Elements.Add(LiveContentElement.MemberOnly);
            }

            if (Reservation != null)
            {
                if (Reservation.IsCanWatch && Elements.Any(x => x == LiveContentElement.Status_Closed))
                {
                    Elements.Add(LiveContentElement.Timeshift_Watch);
                }
                else if (Reservation.IsOutDated)
                {
                    Elements.Add(LiveContentElement.Timeshift_OutDated);
                }
                else
                {
                    Elements.Add(LiveContentElement.Timeshift_Preserved);
                }
            }
            else if (IsTimeshiftEnabled)
            {
                Elements.Add(LiveContentElement.Timeshift_Enable);
            }
        }



        private DelegateCommand _DeleteReservationCommand;
        public DelegateCommand DeleteReservationCommand
        {
            get
            {
                return _DeleteReservationCommand
                    ?? (_DeleteReservationCommand = new DelegateCommand(async () =>
                    {
                        var isDeleted = await DeleteReservation(LiveId, Label);

                        if (isDeleted)
                        {
                            // 予約状態が削除になったことを通知
                            Reservation = null;
                            RaisePropertyChanged(nameof(Reservation));
                            ReservationStatus = null;
                            RaisePropertyChanged(nameof(ReservationStatus));

                            ResetElements();

                            AddReservationCommand.RaiseCanExecuteChanged();
                        }
                    }
                    , () => Reservation != null
                    ));
            }
        }


        private async Task<bool> DeleteReservation(string liveId, string liveTitle)
        {
            if (string.IsNullOrEmpty(liveId)) { throw new ArgumentException(nameof(liveId)); }

            var niconicoSession = App.Current.Container.Resolve<Models.NiconicoSession>();
            var hohoemaDialogService = App.Current.Container.Resolve<DialogService>();

            bool isDeleted = false;

            var token = await niconicoSession.Context.Live.GetReservationTokenAsync();

            if (token == null) { return isDeleted; }

            if (await hohoemaDialogService.ShowMessageDialog(
                $"{liveTitle}",
                "ConfirmDeleteTimeshift".Translate()
                , "DeleteTimeshift".Translate()
                , "Cancel".Translate()
                )
                )
            {
                await niconicoSession.Context.Live.DeleteReservationAsync(liveId, token);

                var deleteAfterReservations = await niconicoSession.Context.Live.GetReservationsAsync();

                isDeleted = !deleteAfterReservations.Any(x => liveId.EndsWith(x));
                if (isDeleted)
                {
                    // 削除成功
                    var notificationService = (App.Current as App).Container.Resolve<Services.NotificationService>();
                    notificationService.ShowInAppNotification(new InAppNotificationPayload()
                    {
                        Content = "InAppNotification_DeletedTimeshift".Translate(),
                        IsShowDismissButton = true,
                    });
                }
                else
                {
                    // まだ存在するゾイ
                    var notificationService = App.Current.Container.Resolve<Services.NotificationService>();
                    notificationService.ShowInAppNotification(new InAppNotificationPayload()
                    {
                        Content = "InAppNotification_FailedDeleteTimeshift".Translate(),
                        IsShowDismissButton = true,
                    });

                    Debug.Fail("タイムシフト削除に失敗しました: " + liveId);
                }
            }

            return isDeleted;

        }


        private DelegateCommand _AddReservationCommand;
        public DelegateCommand AddReservationCommand
        {
            get
            {
                return _AddReservationCommand
                    ?? (_AddReservationCommand = new DelegateCommand(async () =>
                    {
                        var result = await AddReservationAsync(LiveId, Label);
                        if (result)
                        {
                            var reservationProvider = App.Current.Container.Resolve<Models.Provider.LoginUserLiveReservationProvider>();
                            var reservations = await reservationProvider.GetReservtionsAsync();
                            var reservation = reservations.ReservedProgram.FirstOrDefault(x => x.Id == LiveId);
                            if (reservation != null)
                            {
                                SetReservation(reservation);
                            }

                            RaisePropertyChanged(nameof(Reservation));
                            RaisePropertyChanged(nameof(ReservationStatus));
                            ResetElements();
                        }
                    }
                    , () => IsTimeshiftEnabled && (OpenTime - TimeSpan.FromMinutes(30) > DateTime.Now || _niconicoSession.IsPremiumAccount) &&  Reservation == null
                    ));
            }
        }

        private async Task<bool> AddReservationAsync(string liveId, string liveTitle)
        {
            var niconicoSession = App.Current.Container.Resolve<Models.NiconicoSession>();
            var hohoemaDialogService = App.Current.Container.Resolve<DialogService>();
            var result = await niconicoSession.Context.Live.ReservationAsync(liveId);

            bool isAdded = false;
            if (result.IsCanOverwrite)
            {
                // 予約数が上限到達、他のタイムシフトを削除すれば予約可能
                // いずれかの予約を削除するよう選択してもらう
                if (await hohoemaDialogService.ShowMessageDialog(
                       "DialogContent_ConfirmTimeshiftReservationiOverwrite".Translate(result.Data.Overwrite.Title, liveTitle),
                       "DialogTitle_ConfirmTimeshiftReservationiOverwrite".Translate(),
                       "Overwrite".Translate(),
                       "Cancel".Translate()
                    ))
                {
                    result = await niconicoSession.Context.Live.ReservationAsync(liveId, isOverwrite: true);
                }
            }

            if (result.IsOK)
            {
                // 予約できてるはず
                // LiveInfoのタイムシフト周りの情報と共に通知
                var notificationService = (App.Current as App).Container.Resolve<Services.NotificationService>();
                notificationService.ShowInAppNotification(new InAppNotificationPayload()
                {
                    Content = "InAppNotification_AddedTimeshiftWithTitle".Translate(liveTitle),
                });

                isAdded = true;
            }
            else if (result.IsCanOverwrite)
            {
                // 一つ前のダイアログで明示的的にキャンセルしてるはずなので特に通知を表示しない
            }
            else if (result.IsReservationDeuplicated)
            {
                var notificationService = (App.Current as App).Container.Resolve<Services.NotificationService>();
                notificationService.ShowInAppNotification(new InAppNotificationPayload()
                {
                    Content = "InAppNotification_ExistTimeshift".Translate(),
                });
            }
            else if (result.IsReservationExpired)
            {
                var notificationService = (App.Current as App).Container.Resolve<Services.NotificationService>();
                notificationService.ShowInAppNotification(new InAppNotificationPayload()
                {
                    Content = "InAppNotification_TimeshiftExpired".Translate(),
                });
            }

            return isAdded;
        }

    }



    public enum LiveContentElement 
    {
        Provider_Community,
        Provider_Channel,
        Provider_Official,

        Status_Pending,
        Status_Open,
        Status_Start,
        Status_Closed,

        Timeshift_Enable,
        Timeshift_Preserved,
        Timeshift_OutDated,
        Timeshift_Watch,

        MemberOnly, 
    }
}
