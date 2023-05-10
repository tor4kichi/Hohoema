#nullable enable
using System;

namespace Hohoema.Models.PageNavigation;

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

    VideoQueue, // @view

    SubscriptionManagement,

    OwnerMylistManage,
    LocalPlaylistManage,

    SubscVideoList,
}
