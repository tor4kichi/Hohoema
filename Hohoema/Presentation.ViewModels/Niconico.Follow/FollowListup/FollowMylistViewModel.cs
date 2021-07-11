using Hohoema.Models.Domain.Niconico.Mylist;
using Hohoema.Models.Domain.Playlist;
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

        public FollowMylistViewModel(NvapiMylistItem followMylist)
        {
            _followMylist = followMylist;
            PlaylistId = new PlaylistId() { Id = followMylist.Id, Origin = PlaylistItemsSourceOrigin.Mylist };
        }

        public string Name => _followMylist.Name;

        public PlaylistId PlaylistId { get; }

        public MylistId MylistId => _followMylist.Id;

        public string Description => _followMylist.Description;

        public string UserId => _followMylist.Owner.Id?.ToString();

        public bool IsPublic => _followMylist.IsPublic;

        public MylistSortOrder DefaultSortOrder => _followMylist.DefaultSortOrder;

        public MylistSortKey DefaultSortKey => _followMylist.DefaultSortKey;

        public DateTime CreateTime => _followMylist.CreatedAt.DateTime;

        public int SortIndex => 0;

        public int Count => (int)_followMylist.ItemsCount;

        public int FollowerCount => (int)_followMylist.FollowerCount;

        public Uri[] ThumbnailImages => _followMylist.SampleItems.Select(x => x.Video.Thumbnail.MiddleUrl).ToArray();

        public Uri ThumbnailImage => ThumbnailImages.FirstOrDefault();

        public string ThumbnailImageString => ThumbnailImages.FirstOrDefault()?.OriginalString;
    }
}
