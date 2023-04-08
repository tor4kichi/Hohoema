#nullable enable
using Hohoema.Contracts.Services;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Niconico.Video.WatchHistory.LoginUser;
using NiconicoToolkit.Activity.VideoWatchHistory;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hohoema.Services.Niconico;

public sealed class WatchHistoryRemovedEventArgs
{
    public VideoId VideoId { get; set; }
}

public sealed class ContentWatchedEventArgs
{
    public VideoId ContentId { get; set; }
}

public sealed class WatchHistoryManager
{
    private readonly LoginUserVideoWatchHistoryProvider _LoginUserVideoWatchHistoryProvider;
    private readonly INotificationService _notificationService;
    private readonly ILocalizeService _localizeService;

    public WatchHistoryManager(
        LoginUserVideoWatchHistoryProvider LoginUserVideoWatchHistoryProvider,
        INotificationService notificationService,
        ILocalizeService localizeService
        )
    {
        _LoginUserVideoWatchHistoryProvider = LoginUserVideoWatchHistoryProvider;
        _notificationService = notificationService;
        _localizeService = localizeService;
    }

    public event EventHandler<ContentWatchedEventArgs> ContentWatched;
    public event EventHandler<WatchHistoryRemovedEventArgs> WatchHistoryRemoved;
    public event EventHandler WatchHistoryAllRemoved;

    public Task<VideoWatchHistory.VideoWatchHistoryItem[]> GetWatchHistoryItemsAsync(int page = 0, int pageSize = 100)
    {
        return _LoginUserVideoWatchHistoryProvider.GetHistoryAsync(page, pageSize);
    }

    public async Task<bool> RemoveHistoryAsync(IVideoContent watchHistory)
    {
        try
        {
            bool result = await _LoginUserVideoWatchHistoryProvider.RemoveHistoryAsync(watchHistory.VideoId);
            if (result)
            {
                WatchHistoryRemoved?.Invoke(this, new WatchHistoryRemovedEventArgs() { VideoId = watchHistory.VideoId });

                _notificationService.ShowLiteInAppNotification_Success(_localizeService.Translate("VideoHistory_DeleteOne_Success"));
            }
            else
            {
                _notificationService.ShowLiteInAppNotification_Success(_localizeService.Translate("VideoHistory_DeleteOne_Fail"));
            }

            return result;
        }
        catch
        {
            _notificationService.ShowLiteInAppNotification_Success(_localizeService.Translate("VideoHistory_DeleteOne_Fail"));
            throw;
        }
    }

    public async Task<bool> RemoveHistoryAsync(IEnumerable<IVideoContent> watchHistoryItems)
    {
        try
        {
            bool result = await _LoginUserVideoWatchHistoryProvider.RemoveHistoryAsync(watchHistoryItems.Select(x => x.VideoId));
            if (result)
            {
                foreach (IVideoContent item in watchHistoryItems)
                {
                    WatchHistoryRemoved?.Invoke(this, new WatchHistoryRemovedEventArgs() { VideoId = item.VideoId });
                }

                _notificationService.ShowLiteInAppNotification_Success(_localizeService.Translate("VideoHistory_DeleteOne_Success"));
            }
            else
            {
                _notificationService.ShowLiteInAppNotification_Success(_localizeService.Translate("VideoHistory_DeleteOne_Fail"));
            }

            return result;
        }
        catch
        {
            _notificationService.ShowLiteInAppNotification_Success(_localizeService.Translate("VideoHistory_DeleteOne_Fail"));
            throw;
        }
    }

    public async Task<bool> RemoveAllHistoriesAsync()
    {
        try
        {
            bool res = await _LoginUserVideoWatchHistoryProvider.RemoveAllHistoriesAsync();
            if (res)
            {
                WatchHistoryAllRemoved?.Invoke(this, EventArgs.Empty);

                _notificationService.ShowLiteInAppNotification_Success(_localizeService.Translate("VideoHistories_AllDelete_Success"));
            }
            else
            {
                _notificationService.ShowLiteInAppNotification_Success(_localizeService.Translate("VideoHistories_AllDeleted_Fail"));
            }
            return res;
        }
        catch
        {
            _notificationService.ShowLiteInAppNotification_Success(_localizeService.Translate("VideoHistories_AllDeleted_Fail"));
            throw;
        }
    }
}
