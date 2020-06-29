using Hohoema.Models.Niconico.Follow;
using Mntone.Nico2;
using Mntone.Nico2.Users.Follow;
using Mntone.Nico2.Users.FollowCommunity;
using Mntone.Nico2.Videos.Histories;
using Mntone.Nico2.Videos.Recommend;
using Mntone.Nico2.Videos.RemoveHistory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.NicoVideo.Histories
{
    using NiconicoSession = Hohoema.Models.Niconico.NiconicoSession;

    public sealed class LoginUserHistoryProvider : ProviderBase
    {
        public LoginUserHistoryProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }

        public async Task<HistoriesResponse> GetHistory()
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return null;
            }

            var res = await ContextActionWithPageAccessWaitAsync(async context =>
            {
                return await context.Video.GetHistoriesFromMyPageAsync();
            });

            foreach (var history in res?.Histories ?? Enumerable.Empty<Mntone.Nico2.Videos.Histories.History>())
            {
                Database.VideoPlayedHistoryDb.VideoPlayed(history.Id);
            }

            return new HistoriesResponse(res);
        }


        public async Task<RemoveHistoryResponse> RemoveAllHistoriesAsync(string token)
        {
            var res = await ContextActionAsync(async context =>
            {
                return await context.Video.RemoveAllHistoriesAsync(token);
            });

            return new RemoveHistoryResponse(res);
        }

        public async Task<RemoveHistoryResponse> RemoveHistoryAsync(string token, string videoId)
        {
            var res = await ContextActionAsync(async context =>
            {
                return await context.Video.RemoveHistoryAsync(token, videoId);
            });

            return new RemoveHistoryResponse(res);
        }
    }

    public sealed class HistoriesResponse
    {
        private readonly Mntone.Nico2.Videos.Histories.HistoriesResponse _res;

        internal HistoriesResponse(Mntone.Nico2.Videos.Histories.HistoriesResponse res)
        {
            _res = res;
        }

        /// <summary>
        /// トークン
        /// </summary>
        public string Token => _res.Token;

        private List<History> _histories;
        /// <summary>
        /// 視聴した動画の一覧
        /// </summary>
        public IReadOnlyList<History> Histories => _histories ??= _res.Histories.Select(x => new History(x)).ToList();

    }

	public sealed class History
	{
        private readonly Mntone.Nico2.Videos.Histories.History _infraHistory;

        internal History(Mntone.Nico2.Videos.Histories.History infraHistory)
        {
            _infraHistory = infraHistory;
        }

        /// <summary>
        /// 削除された (非公開含む) か
        /// </summary>
        public PrivateReasonType DeleteStatus => _infraHistory.DeleteStatus;

        /// <summary>
        /// デバイス
        /// </summary>
        public ushort Device => _infraHistory.Device;

        /// <summary>
        /// 要素の ID
        /// </summary>
        public string ItemId => _infraHistory.ItemId;

        /// <summary>
        /// 長さ
        /// </summary>
        public TimeSpan Length => _infraHistory.Length;


        /// <summary>
        /// サムネール URL
        /// </summary>
        public Uri ThumbnailUrl => _infraHistory.ThumbnailUrl;

        /// <summary>
        /// 題名
        /// </summary>
        public string Title => _infraHistory.Title;

        /// <summary>
        /// 動画 ID
        /// </summary>
        public string Id => _infraHistory.Id;

        /// <summary>
        /// 閲覧数
        /// </summary>
        public uint WatchCount => _infraHistory.WatchCount;

        /// <summary>
        /// 開場時間
        /// </summary>
        public DateTimeOffset WatchedAt => _infraHistory.WatchedAt;

    }
}
