using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.PageNavigation
{
	public enum HohoemaPageType
	{
		RankingCategoryList,
		RankingCategory,
		UserMylist,
		Mylist,
		FollowManage,
		WatchHistory,

		Search,
        SearchSummary,

        [Obsolete]
        SearchResultCommunity,
        [Obsolete]
        SearchResultTag,
        [Obsolete]
        SearchResultKeyword,
        [Obsolete]
        SearchResultMylist,
        [Obsolete]
        SearchResultLive,

		FeedGroupManage,
		FeedGroup,
		FeedVideoList,

		UserInfo,
		UserVideo,

		Community,
		CommunityVideo,

        VideoInfomation,

        CacheManagement,

        Settings,

        Splash,
        VideoPlayer,

        NicoRepo,
        Recommend,

        ChannelInfo,
        ChannelVideo,

        PrologueIntroduction,
        NicoAccountIntroduction,
        VideoCacheIntroduction,
        EpilogueIntroduction,

        LiveInfomation,
        Timeshift,


        Subscription,

        LocalPlaylist,


        UserSeries,
        Series,

        VideoQueue, // @view

        SubscriptionManagement,
        
        OwnerMylistManage,
        LocalPlaylistManage,

        SubscVideoList,
    }
}
