﻿using Mntone.Nico2.Videos.Histories;
using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.UserFeature;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Presentation.Services;
using I18NPortable;

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
        private readonly LoginUserHistoryProvider _loginUserHistoryProvider;
        private readonly NotificationService _notificationService;

        public WatchHistoryManager(
            LoginUserHistoryProvider loginUserHistoryProvider,
            NotificationService notificationService
            )
        {
            _loginUserHistoryProvider = loginUserHistoryProvider;
            _notificationService = notificationService;
        }

        public event EventHandler<ContentWatchedEventArgs> ContentWatched;
        public event EventHandler<WatchHistoryRemovedEventArgs> WatchHistoryRemoved;
        public event EventHandler WatchHistoryAllRemoved;

        public async Task<bool> RemoveHistoryAsync(IWatchHistory watchHistory)
        {
            var result = await _loginUserHistoryProvider.RemoveHistoryAsync(watchHistory.RemoveToken, watchHistory.ItemId);
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
            var res = await _loginUserHistoryProvider.GetHistory();
            _watchHitoryRemoveToken = res?.Token;
            return res;
        }

        public async Task<bool> RemoveAllHistoriesAsync()
        {
            if (_watchHitoryRemoveToken == null) { return false; }

            var res = await _loginUserHistoryProvider.RemoveAllHistoriesAsync(_watchHitoryRemoveToken);
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
