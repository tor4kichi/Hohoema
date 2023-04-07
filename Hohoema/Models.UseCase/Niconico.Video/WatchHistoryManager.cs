using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Niconico.Video.WatchHistory.LoginUser;
using Hohoema.Services;
using I18NPortable;
using NiconicoToolkit.Activity.VideoWatchHistory;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Niconico.Video
{
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
        private readonly NotificationService _notificationService;

        public WatchHistoryManager(
            LoginUserVideoWatchHistoryProvider LoginUserVideoWatchHistoryProvider,
            NotificationService notificationService
            )
        {
            _LoginUserVideoWatchHistoryProvider = LoginUserVideoWatchHistoryProvider;
            _notificationService = notificationService;
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
                var result = await _LoginUserVideoWatchHistoryProvider.RemoveHistoryAsync(watchHistory.VideoId);
                if (result)
                {
                    WatchHistoryRemoved?.Invoke(this, new WatchHistoryRemovedEventArgs() { VideoId = watchHistory.VideoId });

                    _notificationService.ShowLiteInAppNotification_Success("VideoHistory_DeleteOne_Success".Translate());
                }
                else
                {
                    _notificationService.ShowLiteInAppNotification_Success("VideoHistory_DeleteOne_Fail".Translate());
                }

                return result;
            }
            catch
            {
                _notificationService.ShowLiteInAppNotification_Success("VideoHistory_DeleteOne_Fail".Translate());
                throw;
            }
        }

        public async Task<bool> RemoveHistoryAsync(IEnumerable<IVideoContent> watchHistoryItems)
        {
            try
            {
                var result = await _LoginUserVideoWatchHistoryProvider.RemoveHistoryAsync(watchHistoryItems.Select(x => x.VideoId));
                if (result)
                {
                    foreach (var item in watchHistoryItems)
                    {
                        WatchHistoryRemoved?.Invoke(this, new WatchHistoryRemovedEventArgs() { VideoId = item.VideoId });
                    }

                    _notificationService.ShowLiteInAppNotification_Success("VideoHistory_DeleteOne_Success".Translate());
                }
                else
                {
                    _notificationService.ShowLiteInAppNotification_Success("VideoHistory_DeleteOne_Fail".Translate());
                }

                return result;
            }
            catch
            {
                _notificationService.ShowLiteInAppNotification_Success("VideoHistory_DeleteOne_Fail".Translate());
                throw;
            }
        }

        public async Task<bool> RemoveAllHistoriesAsync()
        {
            try
            {
                var res = await _LoginUserVideoWatchHistoryProvider.RemoveAllHistoriesAsync();
                if (res)
                {
                    WatchHistoryAllRemoved?.Invoke(this, EventArgs.Empty);

                    _notificationService.ShowLiteInAppNotification_Success("VideoHistories_AllDelete_Success".Translate());
                }
                else
                {
                    _notificationService.ShowLiteInAppNotification_Success("VideoHistories_AllDeleted_Fail".Translate());
                }
                return res;
            }
            catch
            {
                _notificationService.ShowLiteInAppNotification_Success("VideoHistories_AllDeleted_Fail".Translate());
                throw;
            }
        }
    }
}
