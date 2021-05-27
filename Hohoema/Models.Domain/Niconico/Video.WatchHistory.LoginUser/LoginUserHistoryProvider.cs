using Hohoema.Models.Infrastructure;
using NiconicoToolkit.Activity.VideoWatchHistory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.Video.WatchHistory.LoginUser
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
            using var _ = await NiconicoSession.SigninLock.LockAsync();
            
            if (!NiconicoSession.IsLoggedIn)
            {
                throw new HohoemaExpception("Failed get login user video watch history. Require LogIn.");
            }

            var res = await NiconicoSession.ToolkitContext.Activity.VideoWachHistory.GetWatchHistoryAsync(0, 100);

            if (res.Meta.IsOK is false) { throw new HohoemaExpception("Failed get login user video watch history"); }

            foreach (var history in res.Data.Items)
            {
                _videoPlayedHistoryRepository.VideoPlayedIfNotWatched(history.WatchId, TimeSpan.MaxValue);
            }

            return res.Data.Items;
        }


        public async Task<bool> RemoveAllHistoriesAsync()
        {
            var res = await NiconicoSession.ToolkitContext.Activity.VideoWachHistory.DeleteAllWatchHistoriesAsync();
            return res.IsOK;
        }

        public async Task<bool> RemoveHistoryAsync(string videoId)
        {
            var res = await NiconicoSession.ToolkitContext.Activity.VideoWachHistory.DeleteWatchHistoriesAsync(videoId);
            return res.IsOK;
        }

        public async Task<bool> RemoveHistoryAsync(IEnumerable<string> videoIdList)
        {
            var res = await NiconicoSession.ToolkitContext.Activity.VideoWachHistory.DeleteWatchHistoriesAsync(videoIdList);
            return res.IsOK;
        }
    }

}
