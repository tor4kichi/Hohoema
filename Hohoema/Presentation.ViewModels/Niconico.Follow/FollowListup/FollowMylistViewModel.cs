using Hohoema.Models.Domain.Niconico.Mylist;
using Mntone.Nico2.Users.Mylist;
using System;
using System.Linq;
using Mntone.Nico2.Users.Follow;

namespace Hohoema.Presentation.ViewModels.Niconico.Follow
{
    public sealed class FollowMylistViewModel : IMylist
    {
        private readonly FollowMylist _followMylist;

        public FollowMylistViewModel(FollowMylist followMylist)
        {
            _followMylist = followMylist;
        }

        public string Label => _followMylist.Detail.Name;

        public string Id => _followMylist.Id.ToString();

        public string Description => _followMylist.Detail.Description;

        public string UserId => _followMylist.Detail.Owner.Id?.ToString();

        public bool IsPublic => _followMylist.Detail.IsPublic;

        public MylistSortOrder DefaultSortOrder => _followMylist.Detail.DefaultSortOrder;

        public MylistSortKey DefaultSortKey => _followMylist.Detail.DefaultSortKey;

        public DateTime CreateTime => _followMylist.Detail.CreatedAt.DateTime;

        public int SortIndex => 0;

        public int Count => (int)_followMylist.Detail.ItemsCount;

        public int FollowerCount => (int)_followMylist.Detail.FollowerCount;

        public Uri[] ThumbnailImages => _followMylist.Detail.SampleItems.Select(x => x.Video.Thumbnail.MiddleUrl).ToArray();

        public Uri ThumbnailImage => ThumbnailImages.FirstOrDefault();

        public string ThumbnailImageString => ThumbnailImages.FirstOrDefault()?.OriginalString;
    }
}
