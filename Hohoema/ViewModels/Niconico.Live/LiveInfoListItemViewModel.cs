#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hohoema.Helpers;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Live;
using Hohoema.Models.Niconico.Live.LoginUser;
using Hohoema.Services;
using Hohoema.ViewModels.Navigation.Commands;
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

public partial class LiveInfoListItemViewModel : ObservableObject, ILiveContent, ILiveContentProvider
{
    private static readonly ILogger<LiveInfoListItemViewModel> _logger;

    public static OpenLiveContentCommand OpenLiveContentCommand { get; }
    public static OpenShareUICommand OpenShareUICommand { get; }
    public static CopyToClipboardCommand CopyToClipboardCommand { get; }
    public static CopyToClipboardWithShareTextCommand CopyToClipboardWithShareTextCommand { get; }
    public static OpenPageCommand OpenPageCommand { get; }
    public static OpenContentOwnerPageCommand OpenPageCommanOpenContentOwnerPageCommand { get; }

    static LiveInfoListItemViewModel()
    {
        _logger = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<ILoggerFactory>().CreateLogger<LiveInfoListItemViewModel>();        
        OpenLiveContentCommand = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<OpenLiveContentCommand>();
        OpenShareUICommand = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<OpenShareUICommand>();
        CopyToClipboardCommand = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<CopyToClipboardCommand>();
        CopyToClipboardWithShareTextCommand = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<CopyToClipboardWithShareTextCommand>();
        OpenPageCommand = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<OpenPageCommand>();
        OpenPageCommanOpenContentOwnerPageCommand = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<OpenContentOwnerPageCommand>();
    }


    public LiveInfoListItemViewModel(string liveId)
    {
        LiveId = liveId;
        _niconicoSession = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<NiconicoSession>();
    }

    public LiveInfoListItemViewModel(Reservation reservation)
    {
        LiveId = reservation.ProgramId;
        _niconicoSession = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<NiconicoSession>();

        ThumbnailUrl = reservation.Thumbnail.Small?.OriginalString ?? reservation.Thumbnail.Large?.OriginalString;
        CommunityThumbnail = reservation.SocialGroup?.ThumbnailSmall;

        CommunityGlobalId = reservation.SocialGroup.SocialGroupId;
        CommunityType = reservation.SocialGroup.Type;

        LiveTitle = reservation.Program.Title;
        ShortDescription = reservation.Program.Description;
        ViewCounter = (int)reservation.Statistics.Viewers;
        CommentCount = (int)reservation.Statistics.Comments;
        StartTime = reservation.Program.Schedule.BeginTime.DateTime;
        EndTime = reservation.Program.Schedule.EndTime.DateTime;
        IsTimeshiftEnabled = reservation.IsActive;
        IsCommunityMemberOnly = reservation.Features.Enabled.Contains(nameof(LiveProgramFeature.MEMBER_ONLY));

        ExpiredAt = reservation.TimeshiftTicket.ExpireTime is { } time ? time.DateTime : null;

        
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

    private readonly NiconicoSession _niconicoSession;

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
    public bool? IsActive { get; internal set; }


    string ILiveContentProvider.ProviderId => CommunityGlobalId;

    string ILiveContentProvider.ProviderName => CommunityName;

    ProviderType ILiveContentProvider.ProviderType => CommunityType;

    LiveId ILiveContent.LiveId => LiveId;

    public string Title => LiveTitle;

    public Reservation? Reservation { get; private set; }
    public void SetReservation(Reservation? reservation)
    {
        Reservation = reservation;
        IsActive = LiveStatus is LiveStatus.Onair ? null : Reservation?.IsActive;
        OnPropertyChanged(nameof(Reservation));
        OnPropertyChanged(nameof(IsActive));
        DeleteReservationCommand.NotifyCanExecuteChanged();
        AddReservationCommand.NotifyCanExecuteChanged();
    }
 
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
            ThumbnailUrl = thumb.Screenshot?.Small?.OriginalString
                ?? thumb.Screenshot?.Large?.OriginalString
                ?? thumb.Small?.OriginalString
                ?? thumb.Huge?.S352X198?.OriginalString
                ?? thumb.Huge?.S640X360?.OriginalString
                ?? string.Empty;
        }

        if (item.ProgramProvider != null)
        {
            CommunityThumbnail = item.ProgramProvider.Icons?.Uri50x50.OriginalString ?? string.Empty;
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

    [RelayCommand(CanExecute = nameof(CanDeleteReservation))]
    async Task DeleteReservation()
    {
        try
        {
            if (await DeleteReservation(LiveId, LiveTitle) is bool isDeleted)
            {
                // 予約状態が削除になったことを通知
                Reservation = null;
                OnPropertyChanged(nameof(Reservation));
                IsActive = null;
                OnPropertyChanged(nameof(IsActive));

                AddReservationCommand.NotifyCanExecuteChanged();
            }

            _logger.ZLogInformation("Reservation deletion result: {0}", isDeleted);
        }
        catch (Exception e)
        {
            _logger.ZLogError(e, "DeleteReservation failed");
        }
    }

    bool CanDeleteReservation()
    {
        return Reservation != null;
    }

    private async Task<bool> DeleteReservation(LiveId liveId, string liveTitle)
    {
        if (string.IsNullOrEmpty(liveId)) { throw new ArgumentException(nameof(liveId)); }

        var niconicoSession = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<NiconicoSession>();
        var dialogService = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<IDialogService>();

        if (await niconicoSession.ToolkitContext.Timeshift.GetReservationTokenAsync() is not { } token)
        {
            return false;
        }

        if (await dialogService.ShowMessageDialog(
            content: $"{liveTitle}",
            title: "ConfirmDeleteTimeshift".Translate(),
            acceptButtonText: "DeleteTimeshift".Translate(),
            cancelButtonText: "Cancel".Translate()
            )
            )
        {
            await niconicoSession.ToolkitContext.Timeshift.DeleteTimeshiftReservationAsync(liveId, token);

            var deleteAfterReservations = await niconicoSession.ToolkitContext.Timeshift.GetTimeshiftReservationsAsync();

            if (deleteAfterReservations.Reservations.Items.Any(x => liveId == x.ProgramId) is false)
            {
                // 削除成功
                var notificationService = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<INotificationService>();
                notificationService.ShowLiteInAppNotification_Success("InAppNotification_DeletedTimeshift".Translate());
                return true;
            }
            else
            {
                // まだ存在する
                var notificationService = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<INotificationService>();
                notificationService.ShowLiteInAppNotification_Fail("InAppNotification_FailedDeleteTimeshift".Translate());

                _logger.ZLogWarning("タイムシフト削除に失敗しました: {0}", liveId);
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddReservation))]
    async Task AddReservation()
    {
        try
        {
            var result = await AddReservationAsync(LiveId, LiveTitle);
            if (result)
            {
                var reservationProvider = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<LoginUserLiveReservationProvider>();
                if (await reservationProvider.GetReservtionsAsync() is { } reservationRes
                    && reservationRes.Reservations.Items.FirstOrDefault(x => LiveId == x.ProgramId) is { } reservation)
                {
                    SetReservation(reservation);
                }
                else
                {
                    SetReservation(null);
                }
            }

            _logger.ZLogInformation("Reservation registration result: {0}", result);
        }
        catch (Exception e)
        {
            _logger.ZLogError(e, "Reservation registration failed.");
        }
    }

    bool CanAddReservation()
    {
        return IsTimeshiftEnabled && (StartTime - TimeSpan.FromMinutes(30) > DateTime.Now || _niconicoSession.IsPremiumAccount) && Reservation == null;
    }

    private async Task<bool> AddReservationAsync(LiveId liveId, string liveTitle)
    {
        var niconicoSession = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<NiconicoSession>();
        var hohoemaDialogService = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<IDialogService>();
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
            var notificationService = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<INotificationService>();
            notificationService.ShowLiteInAppNotification_Success("InAppNotification_AddedTimeshiftWithTitle".Translate(liveTitle), TimeSpan.FromSeconds(3));

            isAdded = true;
        }
        else if (result.IsCanOverwrite)
        {
            // 一つ前のダイアログで明示的的にキャンセルしてるはずなので特に通知を表示しない
        }
        else if (result.IsReservationDeuplicated)
        {
            var notificationService = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<INotificationService>();
            notificationService.ShowLiteInAppNotification_Success("InAppNotification_ExistTimeshift".Translate());
        }
        else if (result.IsReservationExpired)
        {
            var notificationService = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<INotificationService>();
            notificationService.ShowLiteInAppNotification_Fail("InAppNotification_TimeshiftExpired".Translate());
        }

        return isAdded;
    }

}
