using Hohoema.Models.Domain.Niconico.Mylist;
using Hohoema.Models.Domain.Playlist;
using I18NPortable;
using NiconicoToolkit;
using NiconicoToolkit.Mylist;
using NiconicoToolkit.User;
using System;
using System.Linq;

namespace Hohoema.Presentation.ViewModels.Niconico.Follow
{
    public sealed class FollowMylistViewModel : IMylist
    {
        private readonly NvapiMylistItem _followMylist;

        public FollowMylistViewModel(MylistId id, NvapiMylistItem followMylist, ContentStatus status)
        {
            _followMylist = followMylist;
            Status = status;
            PlaylistId = new PlaylistId() { Id = id, Origin = PlaylistItemsSourceOrigin.Mylist };
        }

        public string Name => _followMylist?.Name ?? $"ID: {PlaylistId.Id} {Status.Translate()}";

        public PlaylistId PlaylistId { get; }

        public MylistId MylistId => _followMylist?.Id ?? default(MylistId);

        public string Description => _followMylist?.Description;

        public string UserId => _followMylist?.Owner?.Id?.ToString();

        public bool IsPublic => _followMylist?.IsPublic ?? false;

        public MylistSortOrder DefaultSortOrder => _followMylist?.DefaultSortOrder ?? MylistSortOrder.Desc;

        public MylistSortKey DefaultSortKey => _followMylist?.DefaultSortKey ?? MylistSortKey.AddedAt;

        public DateTime CreateTime => _followMylist?.CreatedAt.DateTime ?? default;

        public int SortIndex => 0;

        public int Count => (int)(_followMylist?.ItemsCount ?? 0);

        public int FollowerCount => (int)(_followMylist?.FollowerCount ?? 0);

        public Uri[] ThumbnailImages => _followMylist?.SampleItems.Select(x => x.Video.Thumbnail.MiddleUrl).ToArray();

        public Uri ThumbnailImage => ThumbnailImages?.FirstOrDefault();

        public string ThumbnailImageString => ThumbnailImages?.FirstOrDefault()?.OriginalString;

        public ContentStatus Status { get; }
    }
}
