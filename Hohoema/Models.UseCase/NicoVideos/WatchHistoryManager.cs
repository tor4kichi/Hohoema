using Hohoema.Models.Domain.Niconico.Video.WatchHistory.LoginUser;
using Hohoema.Presentation.Services;
using I18NPortable;
using Mntone.Nico2.Videos.Histories;
using System;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.NicoVideos
{
    public sealed class WatchHistoryRemovedEventArgs
    {
        public string ItemId { get; set; }
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

        public async Task<bool> RemoveHistoryAsync(IWatchHistory watchHistory)
        {
            var result = await _LoginUserVideoWatchHistoryProvider.RemoveHistoryAsync(watchHistory.RemoveToken, watchHistory.ItemId);
            if (result.IsOK)
            {
                WatchHistoryRemoved?.Invoke(this, new WatchHistoryRemovedEventArgs() { ItemId = watchHistory.ItemId });

                _notificationService.ShowLiteInAppNotification_Success("VideoHistory_DeleteOne_Success".Translate());
            }
            else
            {
                _notificationService.ShowLiteInAppNotification_Success("VideoHistory_DeleteOne_Fail".Translate());
            }

            return result.IsOK;
        }

        string _watchHitoryRemoveToken;
        public async Task<HistoriesResponse> GetWatchHistoriesAsync()
        {
            var res = await _LoginUserVideoWatchHistoryProvider.GetHistory();
            _watchHitoryRemoveToken = res?.Token;
            return res;
        }

        public async Task<bool> RemoveAllHistoriesAsync()
        {
            if (_watchHitoryRemoveToken == null) { return false; }

            var res = await _LoginUserVideoWatchHistoryProvider.RemoveAllHistoriesAsync(_watchHitoryRemoveToken);
            if (res.IsOK)
            {
                WatchHistoryAllRemoved?.Invoke(this, EventArgs.Empty);

                _notificationService.ShowLiteInAppNotification_Success("VideoHistories_AllDelete_Success".Translate());
            }
            else
            {
                _notificationService.ShowLiteInAppNotification_Success("VideoHistories_AllDeleted_Fail".Translate());
            }

            return res.IsOK;
        }
    }
}
