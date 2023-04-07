using Hohoema.Models.Domain.Niconico.Video;
using System;
using System.Linq;
using System.Reactive.Linq;
using NiconicoToolkit.Video;
using Hohoema.ViewModels.VideoListPage;

namespace Hohoema.ViewModels.Community
{
    public class CommunityVideoInfoViewModel : VideoListItemControlViewModel, IVideoContent
    {       
        public CommunityVideoInfoViewModel(NiconicoToolkit.Community.CommunityVideoListItemsResponse.CommunityVideoListItem video)
            : base(video.Id, video.Title, video.ThumbnailUrl.OriginalString, TimeSpan.FromSeconds(video.ContentLength), DateTimeOffset.Parse(video.CreateTime).DateTime)
		{
        }
    }
}
