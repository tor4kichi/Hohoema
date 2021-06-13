using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Live;
using Hohoema.Models.Domain.Niconico.Live.LoginUser;
using Hohoema.Models.Helpers;
using Hohoema.Presentation.Services;
using Hohoema.Models.UseCase.PageNavigation;
using I18NPortable;
using Mntone.Nico2.Live;
using NiconicoToolkit.Live;
using NiconicoToolkit.SearchWithPage.Live;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Unity;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Unity;
using Hohoema.Presentation.ViewModels.Niconico.Share;
using Hohoema.Models.UseCase;
using NiconicoToolkit.Live.Cas;
using NiconicoToolkit.Live.Timeshift;

namespace Hohoema.Presentation.ViewModels.Niconico.Live
{
    public class LiveInfoListItemViewModel : BindableBase, ILiveContent
    {

        public static PageManager PageManager { get; }
        public static OpenLiveContentCommand OpenLiveContentCommand { get; }
        public static OpenShareUICommand OpenShareUICommand { get; }
        public static CopyToClipboardCommand CopyToClipboardCommand { get; }
        public static CopyToClipboardWithShareTextCommand CopyToClipboardWithShareTextCommand { get; }


        static LiveInfoListItemViewModel()
        {
            PageManager = App.Current.Container.Resolve<PageManager>();
            OpenLiveContentCommand = App.Current.Container.Resolve<OpenLiveContentCommand>();
            OpenShareUICommand = App.Current.Container.Resolve<OpenShareUICommand>();
            CopyToClipboardCommand = App.Current.Container.Resolve<CopyToClipboardCommand>();
            CopyToClipboardWithShareTextCommand = App.Current.Container.Resolve<CopyToClipboardWithShareTextCommand>();
        }






        public LiveInfoListItemViewModel(string liveId)
        {
            LiveId = liveId;
            _niconicoSession = App.Current.Container.Resolve<NiconicoSession>();
        }

        private readonly NiconicoSession _niconicoSession;

        public TimeshiftReservationDetailItem Reservation { get; private set; }


        public string LiveId { get; }

		public string CommunityName { get; protected set; }
		public string CommunityThumbnail { get; protected set; }
		public string CommunityGlobalId { get; protected set; }
		public Mntone.Nico2.Live.CommunityType CommunityType { get; protected set; }

		public string LiveTitle { get; protected set; }
        public string ShortDescription { get; protected set; }
        public int ViewCounter { get; protected set; }
        public int CommentCount { get; protected set; }
        public int TimeshiftCount { get; protected set; }
        public DateTime StartTime { get; protected set; }
		public bool HasEndTime { get; private set; }
		public DateTime? EndTime { get; protected set; }
        public TimeSpan Duration { get; protected set; }
        public LiveStatus LiveStatus { get; protected set; }
        public bool IsOnair => LiveStatus is LiveStatus.Onair;
        public bool IsPast => LiveStatus is LiveStatus.Past;
        public bool IsReserved => LiveStatus is LiveStatus.Reserved;

        public bool IsTimeshiftEnabled { get; protected set; }
		public bool IsCommunityMemberOnly { get; protected set; }
        public bool IsOfficialContent { get; protected set; }
        public bool IsChannelContent { get; protected set; }
        public bool IsPayRequired { get; protected set; }

        public string ThumbnailUrl { get; protected set; }

        public bool IsXbox => DeviceTypeHelper.IsXbox;

        public DateTime? ExpiredAt { get; internal set; }
        public ReservationStatus? ReservationStatus { get; internal set; }


        string ILiveContent.ProviderId => CommunityGlobalId;

        string ILiveContent.ProviderName => CommunityName;

        ProviderType ILiveContent.ProviderType => CommunityType switch
        {
            CommunityType.Official => ProviderType.Official,
            CommunityType.Community => ProviderType.Community,
            CommunityType.Channel => ProviderType.Channel,
            _ => throw new NotSupportedException(CommunityType.ToString()),
        };

        string INiconicoObject.Id => LiveId;

        string INiconicoObject.Label => LiveTitle;

        public void SetReservation(TimeshiftReservationDetailItem reservationInfo)
        {
            Reservation = reservationInfo;
            ReservationStatus = LiveStatus is LiveStatus.Onair ? null : reservationInfo?.GetReservationStatus();
            DeleteReservationCommand.RaiseCanExecuteChanged();
            AddReservationCommand.RaiseCanExecuteChanged();
        }

        public void Setup(Mntone.Nico2.Live.Recommend.LiveRecommendData liveVideoInfo)
        {
            CommunityThumbnail = liveVideoInfo.ThumbnailUrl;

            CommunityGlobalId = liveVideoInfo.DefaultCommunity;
            CommunityType = liveVideoInfo.ProviderType;

            LiveTitle = liveVideoInfo.Title;
            StartTime = liveVideoInfo.StartTime.LocalDateTime;

            IsTimeshiftEnabled = false;

            ThumbnailUrl = CommunityThumbnail;

            //Description = $"来場者:{ViewCounter} コメ:{CommentCount}";

            switch (liveVideoInfo.CurrentStatus)
            {
                case Mntone.Nico2.Live.StatusType.Invalid:
                    break;
                case Mntone.Nico2.Live.StatusType.OnAir:
                    Duration = DateTime.Now - liveVideoInfo.StartTime;
                    LiveStatus = LiveStatus.Onair;
                    break;
                case Mntone.Nico2.Live.StatusType.ComingSoon:
                    Duration = TimeSpan.Zero;
                    LiveStatus = LiveStatus.Reserved;
                    break;
                case Mntone.Nico2.Live.StatusType.Closed:
                    //Duration = liveVideoInfo.end;
                    // 放送中のリコメンドであるためタイムシフトはオススメに表示ｓれない
                    LiveStatus = LiveStatus.Past;
                    throw new NotSupportedException();
                    //break;
                default:
                    break;
            }
        }

        public void Setup(LiveSearchPageLiveContentItem item)
        {
            CommunityName = item.ProviderName;
            CommunityThumbnail = item.ProviderIcon?.OriginalString;

            CommunityGlobalId = item.ProviderId;
            CommunityType = item.ProviderType switch
            {
                ProviderType.Channel => CommunityType.Channel,
                ProviderType.Community => CommunityType.Community,
                ProviderType.Official => CommunityType.Official,
                _ => throw new NotSupportedException(),
            };

            LiveTitle = item.Title;
            ShortDescription = new string(item.ShortDescription.Where(x => x is not '\n' && x is not '\t').ToArray());
            ViewCounter = item.VisitorCount;
            CommentCount = item.CommentCount;
            TimeshiftCount = item.TimeshiftCount;
            StartTime = item.StartAt;
            IsTimeshiftEnabled = item.IsTimeshiftAvairable || item.LiveStatus == LiveSearchItemStatus.PastAndPresentTimeshift;
            IsCommunityMemberOnly = item.IsMemberOnly;
            IsOfficialContent = item.ProviderType is ProviderType.Official;
            IsChannelContent = item.ProviderType is ProviderType.Channel;
            IsPayRequired = item.IsRequerePay;

            ThumbnailUrl = item.Thumbnail?.OriginalString;

            LiveStatus = item.LiveStatus switch
            {
                LiveSearchItemStatus.Reserved => LiveStatus.Reserved,
                LiveSearchItemStatus.OnAir => LiveStatus.Onair,
                LiveSearchItemStatus.PastAndPresentTimeshift => LiveStatus.Past,
                LiveSearchItemStatus.PastAndNotPresentTimeshift => LiveStatus.Past,
                _ => throw new NotSupportedException(item.LiveStatus.ToString()),
            };

            if (item.LiveStatus is LiveSearchItemStatus.Reserved)
            {
                // 予約
                //Duration = DateTime.Now - StartTime.LocalDateTime;
            }
            else if (item.LiveStatus is LiveSearchItemStatus.OnAir)
            {
                Duration = DateTimeOffset.Now - StartTime;                
            }
            else
            {
                Duration = item.Duration;
                EndTime = item.StartAt + item.Duration;
            }
        }

        public void Setup(NicoLive liveData)
        {
            CommunityName = liveData.BroadcasterName;
            CommunityThumbnail = liveData.PictureUrl;

            CommunityGlobalId = liveData.BroadcasterId;
            CommunityType = liveData.ProviderType;

            LiveTitle = liveData.Title;
            ShortDescription = liveData.Description;
            ViewCounter = liveData.ViewCount;
            CommentCount = liveData.CommentCount;
            StartTime = liveData.StartTime.LocalDateTime;
            EndTime = liveData.EndTime.LocalDateTime;
            IsTimeshiftEnabled = liveData.TimeshiftEnabled;
            IsCommunityMemberOnly = liveData.IsMemberOnly;

            ThumbnailUrl = liveData.ThumbnailUrl;

            if (StartTime > DateTimeOffset.Now)
            {
                LiveStatus = LiveStatus.Reserved;
            }
            else if (EndTime > DateTimeOffset.Now)
            {
                LiveStatus = LiveStatus.Onair;
                Duration = DateTimeOffset.Now - StartTime;
            }
            else
            {
                LiveStatus = LiveStatus.Past;
                Duration = EndTime.Value - StartTime;
            }
        }

        public void Setup(LiveProgramData liveData)
        {
            //CommunityName = data.;

            ThumbnailUrl = liveData.ThumbnailUrl.OriginalString;
            CommunityThumbnail = liveData.ThumbnailUrl.OriginalString;

            CommunityGlobalId = liveData.ProviderId;
            CommunityType = liveData.ProviderType switch
            {
                ProviderType.Official => CommunityType.Official,
                ProviderType.Channel => CommunityType.Channel,
                ProviderType.Community => CommunityType.Community,
                _ => throw new NotSupportedException(),
            };

            LiveTitle = liveData.Title;
            ShortDescription = liveData.Description;
            ViewCounter = liveData.Viewers ?? 0;
            CommentCount = liveData.Comments ?? 0;
            StartTime = liveData.ShowTime.BeginAt.DateTime;
            EndTime = liveData.ShowTime.EndAt.DateTime;
            IsTimeshiftEnabled = liveData.Timeshift.Enabled;
            IsCommunityMemberOnly = liveData.IsMemberOnly;


            if (StartTime > DateTimeOffset.Now)
            {
                // 予約
                LiveStatus = LiveStatus.Reserved;
            }
            else if (EndTime > DateTimeOffset.Now)
            {
                LiveStatus = LiveStatus.Onair;
                Duration = DateTimeOffset.Now - StartTime;
            }
            else
            {
                LiveStatus = LiveStatus.Past;
                Duration = EndTime.Value - StartTime;
            }
        }

        /*
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
        */



        private DelegateCommand _DeleteReservationCommand;
        public DelegateCommand DeleteReservationCommand
        {
            get
            {
                return _DeleteReservationCommand
                    ?? (_DeleteReservationCommand = new DelegateCommand(async () =>
                    {
                        try
                        {
                            var isDeleted = await DeleteReservation(LiveId, LiveTitle);

                            if (isDeleted)
                            {
                                // 予約状態が削除になったことを通知
                                Reservation = null;
                                RaisePropertyChanged(nameof(Reservation));
                                ReservationStatus = null;
                                RaisePropertyChanged(nameof(ReservationStatus));

                                AddReservationCommand.RaiseCanExecuteChanged();
                            }
                        }
                        catch (Exception e)
                        {
                            ErrorTrackingManager.TrackError(e);
                        }
                    }
                    , () => Reservation != null
                    ));
            }
        }


        private async Task<bool> DeleteReservation(string liveId, string liveTitle)
        {
            if (string.IsNullOrEmpty(liveId)) { throw new ArgumentException(nameof(liveId)); }

            var niconicoSession = App.Current.Container.Resolve<NiconicoSession>();
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
                    notificationService.ShowLiteInAppNotification_Success("InAppNotification_DeletedTimeshift".Translate());
                }
                else
                {
                    // まだ存在するゾイ
                    var notificationService = App.Current.Container.Resolve<Services.NotificationService>();
                    notificationService.ShowLiteInAppNotification_Fail("InAppNotification_FailedDeleteTimeshift".Translate());

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
                        try
                        {
                            var result = await AddReservationAsync(LiveId, LiveTitle);
                            if (result)
                            {
                                var reservationProvider = App.Current.Container.Resolve<LoginUserLiveReservationProvider>();
                                var reservations = await reservationProvider.GetReservtionsDetailAsync();
                                var reservation = reservations.Data.Items.FirstOrDefault(x => LiveId.EndsWith(x.LiveIdWithoutPrefix));
                                if (reservation != null)
                                {
                                    SetReservation(reservation);
                                }

                                RaisePropertyChanged(nameof(Reservation));
                                RaisePropertyChanged(nameof(ReservationStatus));
                            }
                        }
                        catch (Exception e)
                        {
                            ErrorTrackingManager.TrackError(e);
                        }
                    }
                    , () => IsTimeshiftEnabled && (StartTime - TimeSpan.FromMinutes(30) > DateTime.Now || _niconicoSession.IsPremiumAccount) &&  Reservation == null
                    ));
            }
        }

        private async Task<bool> AddReservationAsync(string liveId, string liveTitle)
        {
            var niconicoSession = App.Current.Container.Resolve<NiconicoSession>();
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
                notificationService.ShowLiteInAppNotification_Success("InAppNotification_AddedTimeshiftWithTitle".Translate(liveTitle), TimeSpan.FromSeconds(3));

                isAdded = true;
            }
            else if (result.IsCanOverwrite)
            {
                // 一つ前のダイアログで明示的的にキャンセルしてるはずなので特に通知を表示しない
            }
            else if (result.IsReservationDeuplicated)
            {
                var notificationService = (App.Current as App).Container.Resolve<Services.NotificationService>();
                notificationService.ShowLiteInAppNotification_Success("InAppNotification_ExistTimeshift".Translate());
            }
            else if (result.IsReservationExpired)
            {
                var notificationService = (App.Current as App).Container.Resolve<Services.NotificationService>();
                notificationService.ShowLiteInAppNotification_Fail("InAppNotification_TimeshiftExpired".Translate());
            }

            return isAdded;
        }

    }


}
