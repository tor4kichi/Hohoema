using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Pages
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

        SearchResultCommunity,
        SearchResultTag,
        SearchResultKeyword,
        SearchResultMylist,
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

        WatchAfter, // @view

        SubscriptionManagement,
    }
}
