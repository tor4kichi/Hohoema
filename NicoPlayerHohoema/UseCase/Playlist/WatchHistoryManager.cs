using Mntone.Nico2.Videos.Histories;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.UseCase.Playlist
{
    public sealed class WatchHistoryRemovedEventArgs
    {
        public string ItemId { get; set; }
    }

    public sealed class WatchHistoryManager
    {
        private readonly LoginUserHistoryProvider _loginUserHistoryProvider;

        public WatchHistoryManager(
            LoginUserHistoryProvider loginUserHistoryProvider
            )
        {
            _loginUserHistoryProvider = loginUserHistoryProvider;
        }

        public event EventHandler<WatchHistoryRemovedEventArgs> WatchHistoryRemoved;
        public event EventHandler WatchHistoryAllRemoved;

        public async Task<bool> RemoveHistoryAsync(IWatchHistory watchHistory)
        {
            var result = await _loginUserHistoryProvider.RemoveHistoryAsync(watchHistory.RemoveToken, watchHistory.ItemId);
            if (result.IsOK)
            {
                WatchHistoryRemoved?.Invoke(this, new WatchHistoryRemovedEventArgs() { ItemId = watchHistory.ItemId });
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
            }

            return res.IsOK;
        }
    }
}
