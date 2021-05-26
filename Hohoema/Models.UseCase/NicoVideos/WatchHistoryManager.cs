using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Niconico.Video.WatchHistory.LoginUser;
using Hohoema.Presentation.Services;
using I18NPortable;
using NiconicoToolkit.Activity.VideoWatchHistory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.NicoVideos
{
    public sealed class WatchHistoryRemovedEventArgs
    {
        public string VideoId { get; set; }
    }

    public sealed class ContentWatchedEventArgs
    {
        public string ContentId { get; set; }
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
                var result = await _LoginUserVideoWatchHistoryProvider.RemoveHistoryAsync(watchHistory.Id);
                if (result)
                {
                    WatchHistoryRemoved?.Invoke(this, new WatchHistoryRemovedEventArgs() { VideoId = watchHistory.Id });

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
                var result = await _LoginUserVideoWatchHistoryProvider.RemoveHistoryAsync(watchHistoryItems.Select(x => x.Id));
                if (result)
                {
                    foreach (var item in watchHistoryItems)
                    {
                        WatchHistoryRemoved?.Invoke(this, new WatchHistoryRemovedEventArgs() { VideoId = item.Id });
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
