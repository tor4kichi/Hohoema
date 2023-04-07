using Hohoema.Infra;
using NiconicoToolkit.Activity.VideoWatchHistory;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Video.WatchHistory.LoginUser
{
    public sealed class LoginUserVideoWatchHistoryProvider : ProviderBase
    {
        private readonly VideoPlayedHistoryRepository _videoPlayedHistoryRepository;

        public LoginUserVideoWatchHistoryProvider(NiconicoSession niconicoSession,
            VideoPlayedHistoryRepository videoPlayedHistoryRepository
            )
            : base(niconicoSession)
        {
            _videoPlayedHistoryRepository = videoPlayedHistoryRepository;
        }

        public async Task<VideoWatchHistory.VideoWatchHistoryItem[]> GetHistoryAsync(int page = 0, int pageSize = 100)
        {
            using var _ = await _niconicoSession.SigninLock.LockAsync();
            
            if (!_niconicoSession.IsLoggedIn)
            {
                throw new HohoemaException("Failed get login user video watch history. Require LogIn.");
            }

            var res = await _niconicoSession.ToolkitContext.Activity.VideoWachHistory.GetWatchHistoryAsync(0, 100);

            if (res.Meta.IsSuccess is false) { throw new HohoemaException("Failed get login user video watch history"); }

            foreach (var history in res.Data.Items)
            {
                _videoPlayedHistoryRepository.VideoPlayedIfNotWatched(history.WatchId, TimeSpan.MaxValue);
            }

            return res.Data.Items;
        }


        public async Task<bool> RemoveAllHistoriesAsync()
        {
            var res = await _niconicoSession.ToolkitContext.Activity.VideoWachHistory.DeleteAllWatchHistoriesAsync();
            return res.IsSuccess;
        }

        public async Task<bool> RemoveHistoryAsync(VideoId videoId)
        {
            var res = await _niconicoSession.ToolkitContext.Activity.VideoWachHistory.DeleteWatchHistoriesAsync(videoId);
            return res.IsSuccess;
        }

        public async Task<bool> RemoveHistoryAsync(IEnumerable<VideoId> videoIdList)
        {
            var res = await _niconicoSession.ToolkitContext.Activity.VideoWachHistory.DeleteWatchHistoriesAsync(videoIdList);
            return res.IsSuccess;
        }
    }

}
