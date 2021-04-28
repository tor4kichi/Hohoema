using Mntone.Nico2.Videos.Histories;
using Mntone.Nico2.Videos.Recommend;
using Mntone.Nico2.Videos.RemoveHistory;
using Hohoema.Models.Infrastructure;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.LoginUser
{
    public sealed class LoginUserHistoryProvider : ProviderBase
    {
        private readonly VideoPlayedHistoryRepository _videoPlayedHistoryRepository;

        public LoginUserHistoryProvider(NiconicoSession niconicoSession,
            VideoPlayedHistoryRepository videoPlayedHistoryRepository
            )
            : base(niconicoSession)
        {
            _videoPlayedHistoryRepository = videoPlayedHistoryRepository;
        }

        public async Task<HistoriesResponse> GetHistory()
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return null;
            }

            var res = await ContextActionWithPageAccessWaitAsync(async context =>
            {
                return await context.Video.GetHistoriesAsync();
            });

            foreach (var history in res?.Histories ?? Enumerable.Empty<History>())
            {
                _videoPlayedHistoryRepository.VideoPlayed(history.Id, TimeSpan.MaxValue);
            }

            return res;
        }


        public Task<RemoveHistoryResponse> RemoveAllHistoriesAsync(string token)
        {
            return ContextActionAsync(async context =>
            {
                return await context.Video.RemoveAllHistoriesAsync(token);
            });
            
        }

        public Task<RemoveHistoryResponse> RemoveHistoryAsync(string token, string videoId)
        {
            return ContextActionAsync(async context =>
            {
                return await context.Video.RemoveHistoryAsync(token, videoId);
            });
        }
    }

}
