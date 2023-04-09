#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hohoema.Helpers;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Live;
using Hohoema.Models.Niconico.Live.LoginUser;
using Hohoema.Services;
using Hohoema.ViewModels.Niconico.Share;
using I18NPortable;
using Microsoft.Extensions.Logging;
using NiconicoToolkit.Live;
using NiconicoToolkit.Live.Cas;
using NiconicoToolkit.Live.Timeshift;
using NiconicoToolkit.Search.Live;
using NiconicoToolkit.SearchWithPage.Live;
using System;
using System.Linq;
using System.Threading.Tasks;
using ZLogger;

namespace Hohoema.ViewModels.Niconico.Live;

public class LiveInfoListItemViewModel : ObservableObject, ILiveContent, ILiveContentProvider
{
    private static readonly ILogger<LiveInfoListItemViewModel> _logger;

    public static PageManager PageManager { get; }
    public static OpenLiveContentCommand OpenLiveContentCommand { get; }
    public static OpenShareUICommand OpenShareUICommand { get; }
    public static CopyToClipboardCommand CopyToClipboardCommand { get; }
    public static CopyToClipboardWithShareTextCommand CopyToClipboardWithShareTextCommand { get; }


    static LiveInfoListItemViewModel()
    {
        _logger = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<ILoggerFactory>().CreateLogger<LiveInfoListItemViewModel>();
        PageManager = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<PageManager>();
        OpenLiveContentCommand = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<OpenLiveContentCommand>();
        OpenShareUICommand = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<OpenShareUICommand>();
        CopyToClipboardCommand = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<CopyToClipboardCommand>();
        CopyToClipboardWithShareTextCommand = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<CopyToClipboardWithShareTextCommand>();
    }






    public LiveInfoListItemViewModel(string liveId)
    {
        LiveId = liveId;
        _niconicoSession = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<NiconicoSession>();
    }

    private readonly NiconicoSession _niconicoSession;

    public TimeshiftReservationDetailItem Reservation { get; private set; }


    public LiveId LiveId { get; }

		public string CommunityName { get; protected set; }
		public string CommunityThumbnail { get; protected set; }
		public string CommunityGlobalId { get; protected set; }
		public ProviderType CommunityType { get; protected set; }

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


    string ILiveContentProvider.ProviderId => CommunityGlobalId;

    string ILiveContentProvider.ProviderName => CommunityName;

    ProviderType ILiveContentProvider.ProviderType => CommunityType;

    LiveId ILiveContent.LiveId => LiveId;

    public string Title => LiveTitle;

    public void SetReservation(TimeshiftReservationDetailItem reservationInfo)
    {
        Reservation = reservationInfo;
        ReservationStatus = LiveStatus is LiveStatus.Onair ? null : reservationInfo?.GetReservationStatus();
        DeleteReservationCommand.NotifyCanExecuteChanged();
        AddReservationCommand.NotifyCanExecuteChanged();
    }
    /*
    public void Setup(Mntone.Nico2.Live.Recommend.LiveRecommendData liveVideoInfo)
    {
        CommunityThumbnail = liveVideoInfo.ThumbnailUrl;

        CommunityGlobalId = liveVideoInfo.DefaultCommunity;
        CommunityType = liveVideoInfo.ProviderType switch
        {
            Mntone.Nico2.Live.CommunityType.Official => ProviderType.Official,
            Mntone.Nico2.Live.CommunityType.Community => ProviderType.Community,
            Mntone.Nico2.Live.CommunityType.Channel => ProviderType.Channel,
            _ => throw new NotSupportedException(),
        };

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
    */
    public void Setup(LiveSearchPageLiveContentItem item)
    {
        CommunityName = item.ProviderName;
        CommunityThumbnail = item.ProviderIcon?.OriginalString;

        CommunityGlobalId = item.ProviderId;
        CommunityType = item.ProviderType;

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
        CommunityType = liveData.ProviderType;

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


    internal void Setup(LiveSearchItem item)
    {
        if (item.Thumbnail is not null and var thumb)
        {
            ThumbnailUrl =thumb.Screenshot?.Small?.OriginalString
                ?? thumb.Screenshot?.Large?.OriginalString
                ?? thumb.Small?.OriginalString
                ?? thumb.Huge?.S352X198?.OriginalString
                ?? thumb.Huge?.S640X360?.OriginalString
                ?? string.Empty;
        }

        if (item.ProgramProvider != null)
        {
            CommunityThumbnail = item.ProgramProvider.icons?.Uri50x50.OriginalString ?? string.Empty;
            CommunityGlobalId = item.ProgramProvider.ProgramProviderId;
            CommunityName = item.ProgramProvider.Name;
        }
        else if (item.SocialGroup != null)
        {
            CommunityThumbnail = item.SocialGroup.ThumbnailSmall?.OriginalString ?? string.Empty;
            CommunityGlobalId = item.SocialGroup.SocialGroupId;
            CommunityName = item.SocialGroup.Name;
        }

        // サムネが無い場合は放送者の画像を使う
        ThumbnailUrl ??= CommunityThumbnail;

        CommunityType = item.Program.Provider switch
        {
            Provider.COMMUNITY => ProviderType.Community,
            Provider.CHANNEL => ProviderType.Channel,
            Provider.OFFICIAL => ProviderType.Official,
            _ => throw new NotSupportedException(),
        };

        LiveTitle = item.Program.Title;
        //ShortDescription = item.ProgramProvider.;
        ViewCounter = item.Statistics.Viewers;
        CommentCount = item.Statistics.Comments;
        StartTime = item.Program.Schedule.OpenTime;
        EndTime = item.Program.Schedule.EndTime;
        IsTimeshiftEnabled = item.TimeshiftSetting != null;
        IsCommunityMemberOnly = item.Features.Enabled.Any(x => x is Feature.MEMBER_ONLY);


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


    private RelayCommand _DeleteReservationCommand;
    public RelayCommand DeleteReservationCommand
    {
        get
        {
            return _DeleteReservationCommand
                ?? (_DeleteReservationCommand = new RelayCommand(async () =>
                {
                    try
                    {
                        var isDeleted = await DeleteReservation(LiveId, LiveTitle);

                        if (isDeleted)
                        {
                            // 予約状態が削除になったことを通知
                            Reservation = null;
                            OnPropertyChanged(nameof(Reservation));
                            ReservationStatus = null;
                            OnPropertyChanged(nameof(ReservationStatus));

                            AddReservationCommand.NotifyCanExecuteChanged();
                        }

                        _logger.ZLogInformation("Reservation deletion result: {0}", isDeleted);
                    }
                    catch (Exception e)
                    {
                        _logger.ZLogError(e, "DeleteReservation failed");
                    }
                }
                , () => Reservation != null
                ));
        }
    }


    private async Task<bool> DeleteReservation(LiveId liveId, string liveTitle)
    {
        if (string.IsNullOrEmpty(liveId)) { throw new ArgumentException(nameof(liveId)); }

        var niconicoSession = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<NiconicoSession>();
        var hohoemaDialogService = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<DialogService>();

        bool isDeleted = false;

        var token = await niconicoSession.ToolkitContext.Timeshift.GetReservationTokenAsync();

        if (token == null) { return isDeleted; }

        if (await hohoemaDialogService.ShowMessageDialog(
            $"{liveTitle}",
            "ConfirmDeleteTimeshift".Translate()
            , "DeleteTimeshift".Translate()
            , "Cancel".Translate()
            )
            )
        {
            await niconicoSession.ToolkitContext.Timeshift.DeleteTimeshiftReservationAsync(liveId, token);

            var deleteAfterReservations = await niconicoSession.ToolkitContext.Timeshift.GetTimeshiftReservationsDetailAsync();

            isDeleted = !deleteAfterReservations.Data.Items.Any(x => liveId == x.LiveId);
            if (isDeleted)
            {
                // 削除成功
                var notificationService = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<Services.NotificationService>();
                notificationService.ShowLiteInAppNotification_Success("InAppNotification_DeletedTimeshift".Translate());
            }
            else
            {
                // まだ存在するゾイ
                var notificationService = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<Services.NotificationService>();
                notificationService.ShowLiteInAppNotification_Fail("InAppNotification_FailedDeleteTimeshift".Translate());

                _logger.ZLogWarning("タイムシフト削除に失敗しました: {0}", liveId);
            }
        }

        return isDeleted;

    }


    private RelayCommand _AddReservationCommand;
    public RelayCommand AddReservationCommand
    {
        get
        {
            return _AddReservationCommand
                ?? (_AddReservationCommand = new RelayCommand(async () =>
                {
                    try
                    {
                        var result = await AddReservationAsync(LiveId, LiveTitle);
                        if (result)
                        {
                            var reservationProvider = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<LoginUserLiveReservationProvider>();
                            var reservations = await reservationProvider.GetReservtionsDetailAsync();
                            var reservation = reservations.Data.Items.FirstOrDefault(x => LiveId == x.LiveId);
                            if (reservation != null)
                            {
                                SetReservation(reservation);
                            }

                            OnPropertyChanged(nameof(Reservation));
                            OnPropertyChanged(nameof(ReservationStatus));
                        }

                        _logger.ZLogInformation("Reservation registration result: {0}", result);
                    }
                    catch (Exception e)
                    {
                        _logger.ZLogError(e, "Reservation registration failed.");
                    }
                }
                , () => IsTimeshiftEnabled && (StartTime - TimeSpan.FromMinutes(30) > DateTime.Now || _niconicoSession.IsPremiumAccount) &&  Reservation == null
                ));
        }
    }

    private async Task<bool> AddReservationAsync(LiveId liveId, string liveTitle)
    {
        var niconicoSession = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<NiconicoSession>();
        var hohoemaDialogService = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<DialogService>();
        var result = await niconicoSession.ToolkitContext.Timeshift.ReserveTimeshiftAsync(liveId, overwrite: false);

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
                result = await niconicoSession.ToolkitContext.Timeshift.ReserveTimeshiftAsync(liveId, overwrite: true);
            }
        }

        if (result.IsSuccess)
        {
            // 予約できてるはず
            // LiveInfoのタイムシフト周りの情報と共に通知
            var notificationService = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<Services.NotificationService>();
            notificationService.ShowLiteInAppNotification_Success("InAppNotification_AddedTimeshiftWithTitle".Translate(liveTitle), TimeSpan.FromSeconds(3));

            isAdded = true;
        }
        else if (result.IsCanOverwrite)
        {
            // 一つ前のダイアログで明示的的にキャンセルしてるはずなので特に通知を表示しない
        }
        else if (result.IsReservationDeuplicated)
        {
            var notificationService = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<Services.NotificationService>();
            notificationService.ShowLiteInAppNotification_Success("InAppNotification_ExistTimeshift".Translate());
        }
        else if (result.IsReservationExpired)
        {
            var notificationService = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<Services.NotificationService>();
            notificationService.ShowLiteInAppNotification_Fail("InAppNotification_TimeshiftExpired".Translate());
        }

        return isAdded;
    }

}
