using Hohoema.Models.Domain.Niconico.User;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.Domain.Video;
using Microsoft.Toolkit.Diagnostics;
using NiconicoToolkit.User;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.User
{
    public class UserVideoPlaylist : IUnlimitedPlaylist
    {
        private readonly UserId _userId;
        private readonly UserProvider _userProvider;

        public static UserVideoPlaylistSortOption[] SortOptions { get; } = new[]
        {
            UserVideoSortKey.RegisteredAt,
            UserVideoSortKey.ViewCount,
            UserVideoSortKey.CommentCount,
            UserVideoSortKey.LastCommentTime,
            UserVideoSortKey.MylistCount,
            UserVideoSortKey.Duration,
        }
        .SelectMany(x =>
        {
            return new[] { new UserVideoPlaylistSortOption(x, UserVideoSortOrder.Desc), new UserVideoPlaylistSortOption(x, UserVideoSortOrder.Asc) };
        })
            .ToArray();

        public static UserVideoPlaylistSortOption DefaultSortOption => SortOptions[0];


        public UserVideoPlaylist(UserId userId, PlaylistId playlistId, string nickname, UserProvider userProvider)
        {
            _userId = userId;
            PlaylistId = playlistId;
            Name = nickname;
            _userProvider = userProvider;
        }
        public string Name { get; }

        public PlaylistId PlaylistId { get; }

        IPlaylistSortOption[] IPlaylist.SortOptions => SortOptions;

        IPlaylistSortOption IPlaylist.DefaultSortOption => DefaultSortOption;

        public async Task<IEnumerable<IVideoContent>> GetPagedItemsAsync(int pageIndex, int pageSize, IPlaylistSortOption sortOption, CancellationToken cancellationToken = default)
        {
            var sort = sortOption as UserVideoPlaylistSortOption;
            var result = await _userProvider.GetUserVideosAsync(_userId, pageIndex, pageSize, sort.SortKey, sort.SortOrder);

            Guard.IsTrue(result.IsSuccess, nameof(result.IsSuccess));

            return result.Data.Items.Select(x => new NvapiVideoContent(x.Essential));
        }
    }
}
