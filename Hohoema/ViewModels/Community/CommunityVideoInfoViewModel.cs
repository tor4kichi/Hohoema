#nullable enable
using Hohoema.Models.Niconico.Video;
using Hohoema.ViewModels.VideoListPage;
using System;

namespace Hohoema.ViewModels.Community;

public class CommunityVideoInfoViewModel : VideoListItemControlViewModel, IVideoContent
{       
    public CommunityVideoInfoViewModel(NiconicoToolkit.Community.CommunityVideoListItemsResponse.CommunityVideoListItem video)
        : base(video.Id, video.Title, video.ThumbnailUrl.OriginalString, TimeSpan.FromSeconds(video.ContentLength), DateTimeOffset.Parse(video.CreateTime).DateTime)
		{
    }
}
