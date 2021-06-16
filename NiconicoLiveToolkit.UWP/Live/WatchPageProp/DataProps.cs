using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.Live.WatchPageProp
{
    using NiconicoToolkit.User;
    // Generated with https://app.quicktype.io/

    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using System.Linq;
    using J = JsonPropertyNameAttribute;

    public partial class LiveWatchPageDataProp
    {
        [J("akashic")] public Akashic Akashic { get; set; }
        [J("site")] public Site Site { get; set; }
        [J("user")] public User User { get; set; }
        [J("program")] public Program Program { get; set; }
        [J("socialGroup")] public SocialGroup SocialGroup { get; set; }
        [J("player")] public Player Player { get; set; }
        [J("ad")] public Ad Ad { get; set; }
        //[J("billboard")] public Billboard Billboard { get; set; }
        //[J("assets")] public Assets Assets { get; set; }
        [J("nicoEnquete")] public NicoEnquete NicoEnquete { get; set; }
        [J("channel")] public Channel Channel { get; set; }
        [J("channelFollower")] public ChannelFollower ChannelFollower { get; set; }
        [J("channelMember")] public ChannelFollower ChannelMember { get; set; }
        [J("userProgramWatch")] public UserProgramWatch UserProgramWatch { get; set; }
        [J("userProgramReservation")] public UserProgramReservation UserProgramReservation { get; set; }
        [J("recommend")] public Recommend Recommend { get; set; }
        [J("programWatch")] public ProgramWatch ProgramWatch { get; set; }
        [J("programTimeshift")] public ProgramTimeshift ProgramTimeshift { get; set; }
        [J("programTimeshiftWatch")] public ProgramTimeshiftWatch ProgramTimeshiftWatch { get; set; }
        [J("premiumAppealBanner")] public PremiumAppealBanner PremiumAppealBanner { get; set; }
        [J("community")] public Community Community { get; set; }
        [J("communityFollower")] public ChannelFollower CommunityFollower { get; set; }
        [J("emotion")] public Emotion Emotion { get; set; }
        [J("supplierProgram")] public SupplierProgram SupplierProgram { get; set; }
    }

    public partial class Ad
    {
        [J("isBillboardEnabled")] public bool IsBillboardEnabled { get; set; }
        [J("isSiteHeaderBannerEnabled")] public bool IsSiteHeaderBannerEnabled { get; set; }
        [J("isProgramInformationEnabled")] public bool IsProgramInformationEnabled { get; set; }
        [J("isFooterEnabled")] public bool IsFooterEnabled { get; set; }
        [J("adsJsUrl")] public Uri AdsJsUrl { get; set; }
    }

    public partial class Akashic
    {
        [J("trustedChildOrigin")] public string TrustedChildOrigin { get; set; }
    }

    public partial class Assets
    {
        [J("scripts")] public Scripts Scripts { get; set; }
        [J("stylesheets")] public Stylesheets Stylesheets { get; set; }
    }

    public partial class Scripts
    {
        [J("vendor")] public string Vendor { get; set; }
        [J("nicolib")] public string Nicolib { get; set; }
        [J("broadcaster-tool")] public string BroadcasterTool { get; set; }
        [J("ichiba")] public string Ichiba { get; set; }
        [J("nicoheader")] public string Nicoheader { get; set; }
        [J("operator-tools")] public string OperatorTools { get; set; }
        [J("pc-watch")] public string PcWatch { get; set; }
        [J("pc-watch.all")] public string PcWatchAll { get; set; }
        [J("polyfill")] public string Polyfill { get; set; }
    }

    public partial class Stylesheets
    {
        [J("broadcaster-tool")] public string BroadcasterTool { get; set; }
        [J("ichiba")] public string Ichiba { get; set; }
        [J("nicoheader")] public string Nicoheader { get; set; }
        [J("operator-tools")] public string OperatorTools { get; set; }
        [J("pc-watch")] public string PcWatch { get; set; }
        [J("pc-watch.all")] public string PcWatchAll { get; set; }
    }

    public partial class Billboard
    {
    }

    public partial class Channel
    {
        [J("id")] public string Id { get; set; }
        [J("programHistoryPageUrl")] public Uri ProgramHistoryPageUrl { get; set; }
        [J("registerPageUrl")] public Uri RegisterPageUrl { get; set; }
    }

    public partial class ChannelFollower
    {
        [J("records")] public List<object> Records { get; set; }
    }

    public partial class Community
    {
        [J("id")] public string Id { get; set; }
        [J("followPageUrl")] public Uri FollowPageUrl { get; set; }
        [J("unfollowPageUrl")] public Uri UnfollowPageUrl { get; set; }
    }

    public partial class Emotion
    {
        [J("event")] public object Event { get; set; }
        [J("isEmotionEnabled")] public bool IsEmotionEnabled { get; set; }
    }

    public partial class NicoEnquete
    {
        [J("isEnabled")] public bool IsEnabled { get; set; }
    }

    public partial class Player
    {
        [J("name")] public string Name { get; set; }
        [J("audienceToken")] public string AudienceToken { get; set; }
        [J("isJumpDisabled")] public bool IsJumpDisabled { get; set; }
        [J("disablePlayVideoAd")] public bool DisablePlayVideoAd { get; set; }
        [J("isRestrictedCommentPost")] public bool IsRestrictedCommentPost { get; set; }
        [J("streamAllocationType")] public string StreamAllocationType { get; set; }
    }

    public partial class PremiumAppealBanner
    {
        [J("premiumRegistrationUrl")] public Uri PremiumRegistrationUrl { get; set; }
    }

    public partial class Program
    {
        [J("allegation")] public Allegation Allegation { get; set; }
        [J("nicoliveProgramId")] public LiveId NicoliveProgramId { get; set; }
        [J("reliveProgramId")] public string ReliveProgramId { get; set; }
        [J("providerType")] public ProviderType ProviderType { get; set; }
        [J("visualProviderType")] public ProviderType VisualProviderType { get; set; }
        [J("title")] public string Title { get; set; }
        [J("thumbnail")] public Thumbnail Thumbnail { get; set; }
        [J("supplier")] public Supplier Supplier { get; set; }
        [J("openTime")] public long OpenTime { get; set; }
        [J("beginTime")] public long BeginTime { get; set; }
        [J("vposBaseTime")] public long VposBaseTime { get; set; }
        [J("endTime")] public long EndTime { get; set; }
        [J("scheduledEndTime")] public long ScheduledEndTime { get; set; }
        [J("status")] public ProgramLiveStatus Status { get; set; }
        [J("description")] public string Description { get; set; }
        [J("substitute")] public Substitute Substitute { get; set; }
        [J("tag")] public ProgramTag Tag { get; set; }
        [J("links")] public Links Links { get; set; }
        [J("player")] public ProgramPlayer Player { get; set; }
        [J("watchPageUrl")] public Uri WatchPageUrl { get; set; }
        [J("gatePageUrl")] public Uri GatePageUrl { get; set; }
        [J("mediaServerType")] public string MediaServerType { get; set; }
        [J("isPrivate")] public bool IsPrivate { get; set; }
        [J("isTest")] public bool IsTest { get; set; }
        [J("isSdk")] public bool? IsSdk { get; set; }
        [J("zapping")] public Zapping Zapping { get; set; }
        [J("trialWatch")] public Payment TrialWatch { get; set; }
        [J("report")] public Report Report { get; set; }
        [J("isFollowerOnly")] public bool IsFollowerOnly { get; set; }
        [J("isMemberFree")] public bool? IsMemberFree { get; set; }
        [J("cueSheet")] public CueSheet CueSheet { get; set; }
        [J("cueSheetSnapshot")] public CueSheetSnapshot CueSheetSnapshot { get; set; }
        [J("nicoad")] public ProgramNicoad Nicoad { get; set; }
        [J("isGiftEnabled")] public bool? IsGiftEnabled { get; set; }
        [J("stream")] public ProgramStream Stream { get; set; }
        [J("superichiba")] public ProgramSuperichiba Superichiba { get; set; }
        [J("isChasePlayEnabled")] public bool IsChasePlayEnabled { get; set; }
        [J("isTimeshiftDownloadEnabled")] public bool? IsTimeshiftDownloadEnabled { get; set; }
        [J("statistics")] public Statistics Statistics { get; set; }
        [J("payment")] public Payment Payment { get; set; }
        [J("isPremiumAppealBannerEnabled")] public bool IsPremiumAppealBannerEnabled { get; set; }
        [J("isRecommendEnabled")] public bool IsRecommendEnabled { get; set; }
        [J("isEmotionEnabled")] public bool IsEmotionEnabled { get; set; }
        [J("screenshot")] public Screenshot Screenshot { get; set; }
        [J("additionalDescription")] public string AdditionalDescription { get; set; }
        [J("twitter")] public Twitter Twitter { get; set; }
    }

    public partial class Allegation
    {
        [J("commentAllegationApiUrl")] public Uri CommentAllegationApiUrl { get; set; }
    }

    public partial class CueSheet
    {
        [J("eventsApiUrl")] public Uri EventsApiUrl { get; set; }
    }

    public partial class CueSheetSnapshot
    {
        [J("commentLocked")] public bool CommentLocked { get; set; }
        [J("audienceCommentLayout")] public string AudienceCommentLayout { get; set; }
        [J("trialWatch")] public TrialWatch TrialWatch { get; set; }
    }

    public partial class TrialWatch
    {
        [J("isVideoEnabled")] public bool IsVideoEnabled { get; set; }
        [J("isCommentEnabled")] public bool IsCommentEnabled { get; set; }
    }

    public partial class Links
    {
        [J("feedbackPageUrl")] public Uri FeedbackPageUrl { get; set; }
        [J("contentsTreePageUrl")] public Uri ContentsTreePageUrl { get; set; }
        [J("programReportPageUrl")] public Uri ProgramReportPageUrl { get; set; }
        [J("tagReportPageUrl")] public Uri TagReportPageUrl { get; set; }
    }

    public partial class ProgramNicoad
    {
        [J("totalPoint")] public long TotalPoint { get; set; }
        [J("ranking")] public List<object> Ranking { get; set; }
    }

    public partial class Payment
    {
        [J("ticketAgencyPageUrl")] public Uri TicketAgencyPageUrl { get; set; }
    }

    public partial class ProgramPlayer
    {
        [J("embedUrl")] public Uri EmbedUrl { get; set; }
        [J("banner")] public Banner Banner { get; set; }
    }

    public partial class Banner
    {
        [J("apiUrl")] public Uri ApiUrl { get; set; }
    }

    public partial class Report
    {
        [J("imageApiUrl")] public Uri ImageApiUrl { get; set; }
    }

    public partial class Screenshot
    {
        [J("urlSet")] public UrlSet UrlSet { get; set; }
    }

    public partial class UrlSet
    {
        [J("large")] public Uri Large { get; set; }
        [J("middle")] public Uri Middle { get; set; }
        [J("small")] public Uri Small { get; set; }
        [J("micro")] public Uri Micro { get; set; }
    }

    public partial class Statistics
    {
        [J("watchCount")] public long WatchCount { get; set; }
        [J("commentCount")] public long CommentCount { get; set; }
    }

    public partial class ProgramStream
    {
        [J("maxQuality")] public Live.WatchSession.LiveQualityLimitType MaxQuality { get; set; }
    }

    public partial class Substitute
    {
        [J("topPageTitle")] public string TopPageTitle { get; set; }
        [J("topPageDescription")] public string TopPageDescription { get; set; }
    }

    public partial class ProgramSuperichiba
    {
        [J("allowAudienceToAddNeta")] public bool AllowAudienceToAddNeta { get; set; }
        [J("canSupplierUse")] public bool CanSupplierUse { get; set; }
    }

    public partial class Supplier
    {
        [J("name")] public string Name { get; set; }
        [J("pageUrl")] public Uri PageUrl { get; set; }
        [J("nicopediaArticle")] public NicopediaArticle NicopediaArticle { get; set; }
        [J("programProviderId")] 
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long? ProgramProviderId { get; set; }
        [J("icons")] public Icons Icons { get; set; }
        [J("level")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] 
        public long? Level { get; set; }
        [J("accountType")] public string AccountType { get; set; }
    }

    public partial class Icons
    {
        [J("uri50x50")] public Uri Uri50X50 { get; set; }
        [J("uri150x150")] public Uri Uri150X150 { get; set; }
    }

    public partial class NicopediaArticle
    {
        [J("pageUrl")] public Uri PageUrl { get; set; }
        [J("exists")] public bool Exists { get; set; }
    }

    public partial class ProgramTag
    {
        [J("list")] public List<ProgramTagItem> List { get; set; }
        [J("apiUrl")] public Uri ApiUrl { get; set; }
        [J("registerApiUrl")] public Uri RegisterApiUrl { get; set; }
        [J("deleteApiUrl")] public Uri DeleteApiUrl { get; set; }
        [J("apiToken")] public string ApiToken { get; set; }
        [J("isLocked")] public bool IsLocked { get; set; }
    }

    public partial class ProgramTagItem
    {
        [J("text")] public string Text { get; set; }
        [J("existsNicopediaArticle")] public bool ExistsNicopediaArticle { get; set; }
        [J("nicopediaArticlePageUrl")] public Uri NicopediaArticlePageUrl { get; set; }
        [J("type")] public string Type { get; set; }
        [J("isLocked")] public bool IsLocked { get; set; }
        [J("isDeletable")] public bool IsDeletable { get; set; }
    }

    public partial class Thumbnail
    {
        [J("small")] public Uri Small { get; set; }
        [J("large")] public Uri Large { get; set; }
        [J("huge")] public Huge Huge { get; set; }
    }

    public partial class Huge
    {
        [J("s1920x1080")] public Uri S1920X1080 { get; set; }
        [J("s1280x720")] public Uri S1280X720 { get; set; }
        [J("s640x360")] public Uri S640X360 { get; set; }
        [J("s352x198")] public Uri S352X198 { get; set; }
    }

    public partial class Twitter
    {
        [J("hashTags")] public List<string> HashTags { get; set; }
    }

    public partial class Zapping
    {
        [J("listApiUrl")] public Uri ListApiUrl { get; set; }
        [J("listUpdateIntervalMs")] public long ListUpdateIntervalMs { get; set; }
    }

    public partial class ProgramTimeshift
    {
        [J("watchLimit")] public string WatchLimit { get; set; }
        [J("publication")] public Publication Publication { get; set; }
        [J("reservation")] public Reservation Reservation { get; set; }
    }

    public partial class Publication
    {
        [J("status")] public string Status { get; set; }

        [J("expireTime")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] 
        public long? ExpireTime { get; set; }
    }

    public partial class Reservation
    {
        [J("expireTime")] public long ExpireTime { get; set; }
    }

    public partial class ProgramTimeshiftWatch
    {
        [J("condition")] public ProgramTimeshiftWatchCondition Condition { get; set; }
    }

    public partial class ProgramTimeshiftWatchCondition
    {
        [J("needReservation")] public bool NeedReservation { get; set; }
    }

    public partial class ProgramWatch
    {
        [J("condition")] public ProgramWatchCondition Condition { get; set; }
    }

    public partial class ProgramWatchCondition
    {
        [J("needLogin")] public bool NeedLogin { get; set; }
        [J("payment")] public string Payment { get; set; }
    }

    public partial class Recommend
    {
        [J("program")] public RecommendProgram Program { get; set; }
        [J("related")] public Related Related { get; set; }
        [J("isFrontendReloadEnabled")] public bool IsFrontendReloadEnabled { get; set; }
    }

    public partial class RecommendProgram
    {
        [J("data")] public ProgramData Data { get; set; }
    }

    public partial class ProgramData
    {
        [J("recommend_id")] public string RecommendId { get; set; }
        [J("recipe_id")] public string RecipeId { get; set; }
        [J("values")] public List<PurpleValue> Values { get; set; }
    }

    public partial class PurpleValue
    {
        [J("id")] public string Id { get; set; }
        [J("recommend_type")] public PurpleRecommendType RecommendType { get; set; }
        [J("content_type")] public PurpleContentType ContentType { get; set; }
        [J("content_meta")] public PurpleContentMeta ContentMeta { get; set; }
    }

    public partial class PurpleContentMeta
    {
        [J("community_text")] public string CommunityText { get; set; }
        [J("timeshift_mode")] public long TimeshiftMode { get; set; }
        [J("start_time")] public DateTimeOffset StartTime { get; set; }
        [J("member_only")] public bool MemberOnly { get; set; }
        [J("description")] public string Description { get; set; }
        [J("tags")] public string Tags { get; set; }
        [J("live_status")] public LiveStatus LiveStatus { get; set; }
        [J("user_id")] public UserId? UserId { get; set; }
        [J("provider_type")] public ProviderType ProviderType { get; set; }
        [J("timeshift_expired")] public DateTimeOffset? TimeshiftExpired { get; set; }
        [J("open_time")] public DateTimeOffset OpenTime { get; set; }
        [J("live_end_time")] public DateTimeOffset LiveEndTime { get; set; }
        [J("is_product_stream")] public bool IsProductStream { get; set; }
        [J("channel_id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] 
        public long? ChannelId { get; set; }
        [J("community_id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] 
        public long? CommunityId { get; set; }
        [J("comment_counter")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] 
        public long? CommentCounter { get; set; }
        [J("timeshift_enabled")] public bool TimeshiftEnabled { get; set; }
        [J("thumbnail_url")] public Uri ThumbnailUrl { get; set; }
        [J("view_counter")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] 
        public long? ViewCounter { get; set; }
        [J("ss_adult")] public bool SsAdult { get; set; }
        [J("category_tags")] public string CategoryTags { get; set; }
        [J("title")] public string Title { get; set; }
        [J("community_icon")] public Uri CommunityIcon { get; set; }
        [J("picture_url")] public Uri PictureUrl { get; set; }
        [J("score_timeshift_reserved")] public long ScoreTimeshiftReserved { get; set; }
        [J("content_id")] public string ContentId { get; set; }
        [J("live_screenshot_thumbnail_large")] public string LiveScreenshotThumbnailLarge { get; set; }
        [J("live_screenshot_thumbnail_middle")] public string LiveScreenshotThumbnailMiddle { get; set; }
        [J("live_screenshot_thumbnail_small")] public string LiveScreenshotThumbnailSmall { get; set; }
        [J("live_screenshot_thumbnail_micro")] public string LiveScreenshotThumbnailMicro { get; set; }
        [J("ts_screenshot_thumbnail_large")] public string TsScreenshotThumbnailLarge { get; set; }
        [J("ts_screenshot_thumbnail_middle")] public string TsScreenshotThumbnailMiddle { get; set; }
        [J("ts_screenshot_thumbnail_small")] public string TsScreenshotThumbnailSmall { get; set; }
        [J("ts_screenshot_thumbnail_micro")] public string TsScreenshotThumbnailMicro { get; set; }
        [J("user_nickname")] public string UserNickname { get; set; }
        [J("user_icon_150x150")] public Uri UserIcon150X150 { get; set; }
        [J("user_icon_50x50")] public Uri UserIcon50X50 { get; set; }
    }

    public partial class Related
    {
        [J("data")] public RelatedData Data { get; set; }
    }

    public partial class RelatedData
    {
        [J("recommend_id")] public string RecommendId { get; set; }
        [J("recipe_id")] public string RecipeId { get; set; }
        [J("values")] public List<FluffyValue> Values { get; set; }
    }

    public partial class FluffyValue
    {
        [J("id")] public string Id { get; set; }
        [J("recommend_type")] public FluffyRecommendType RecommendType { get; set; }
        [J("content_type")] public FluffyContentType ContentType { get; set; }
        [J("content_meta")] public FluffyContentMeta ContentMeta { get; set; }
    }

    public partial class FluffyContentMeta
    {
        [J("id")] public string Id { get; set; }
        [J("status")] public Status Status { get; set; }
        [J("video_watch_page_id")] public string VideoWatchPageId { get; set; }
        [J("title")] public string Title { get; set; }
        [J("description")] public string Description { get; set; }
        [J("genre")] public Genre Genre { get; set; }
        [J("view_count")] public long ViewCount { get; set; }
        [J("mylist_count")] public long MylistCount { get; set; }
        [J("length_seconds")] public long LengthSeconds { get; set; }
        [J("upload_time")] public DateTimeOffset UploadTime { get; set; }
        [J("thumbnail_url")] public ThumbnailUrl ThumbnailUrl { get; set; }
        [J("is_official")] public bool IsOfficial { get; set; }
        [J("tags")] public List<TagElement> Tags { get; set; }
        [J("threads")] public Threads Threads { get; set; }
    }

    public partial class Genre
    {
        [J("key")] public string Key { get; set; }
        [J("label")] public string Label { get; set; }
    }

    public partial class TagElement
    {
        [J("text")] public string Text { get; set; }
        [J("is_video_owner_locked")] public bool IsVideoOwnerLocked { get; set; }
        [J("is_category_tag")] public bool IsCategoryTag { get; set; }
        [J("category_key")] public string CategoryKey { get; set; }
    }

    public partial class Threads
    {
        [J("message_server_urls")] public MessageServerUrls MessageServerUrls { get; set; }
        [J("channel")] public ThreadsChannel Channel { get; set; }
        [J("default")] public Default Default { get; set; }
    }

    public partial class ThreadsChannel
    {
        [J("id")] public long Id { get; set; }
        [J("comment_count")] public long CommentCount { get; set; }
        [J("permissions")] public Permissions Permissions { get; set; }
        [J("latest_comments")] public string LatestComments { get; set; }
        [J("channel_id")] public string ChannelId { get; set; }
        [J("update_time")] public DateTimeOffset UpdateTime { get; set; }
    }

    public partial class Permissions
    {
        [J("read")] public PermissionType Read { get; set; }
        [J("write")] public PermissionType Write { get; set; }
    }

    public partial class Default
    {
        [J("id")] public long Id { get; set; }
        [J("comment_count")] public long CommentCount { get; set; }
        [J("permissions")] public Permissions Permissions { get; set; }
        [J("update_time")] public DateTimeOffset UpdateTime { get; set; }
    }

    public partial class MessageServerUrls
    {
        [J("main")] public Uri Main { get; set; }
        [J("sub")] public Uri Sub { get; set; }
    }

    public partial class ThumbnailUrl
    {
        [J("normal")] public Uri Normal { get; set; }
        [J("ogp")] public Uri Ogp { get; set; }
        [J("middle")] public Uri Middle { get; set; }
        [J("large")] public Uri Large { get; set; }
    }

    public partial class Site
    {
        [J("locale")] public string Locale { get; set; }
        [J("serverTime")] public long ServerTime { get; set; }
        [J("frontendVersion")] public string FrontendVersion { get; set; }
        [J("apiBaseUrl")] public Uri ApiBaseUrl { get; set; }
        [J("staticResourceBaseUrl")] public Uri StaticResourceBaseUrl { get; set; }
        [J("topPageUrl")] public Uri TopPageUrl { get; set; }
        [J("programCreatePageUrl")] public Uri ProgramCreatePageUrl { get; set; }
        [J("programEditPageUrl")] public Uri ProgramEditPageUrl { get; set; }
        [J("programWatchPageUrl")] public Uri ProgramWatchPageUrl { get; set; }
        [J("recentPageUrl")] public Uri RecentPageUrl { get; set; }
        [J("programArchivePageUrl")] public Uri ProgramArchivePageUrl { get; set; }
        [J("historyPageUrl")] public Uri HistoryPageUrl { get; set; }
        [J("myPageUrl")] public Uri MyPageUrl { get; set; }
        [J("rankingPageUrl")] public Uri RankingPageUrl { get; set; }
        [J("searchPageUrl")] public Uri SearchPageUrl { get; set; }
        [J("focusPageUrl")] public Uri FocusPageUrl { get; set; }
        [J("timetablePageUrl")] public Uri TimetablePageUrl { get; set; }
        [J("followedProgramsPageUrl")] public Uri FollowedProgramsPageUrl { get; set; }
        [J("frontendId")] public long FrontendId { get; set; }
        [J("familyService")] public FamilyService FamilyService { get; set; }
        [J("environments")] public Environments Environments { get; set; }
        [J("relive")] public Relive Relive { get; set; }
        [J("information")] public Information Information { get; set; }
        [J("rule")] public Rule Rule { get; set; }
        [J("spec")] public Spec Spec { get; set; }
        [J("ad")] public SiteAd Ad { get; set; }
        [J("program")] public SiteProgram Program { get; set; }
        [J("tag")] public SiteTag Tag { get; set; }
        [J("coe")] public Coe Coe { get; set; }
        [J("notify")] public Notify Notify { get; set; }
        [J("timeshift")] public Timeshift Timeshift { get; set; }
        [J("broadcast")] public Broadcast Broadcast { get; set; }
        [J("enquete")] public AutoExtend Enquete { get; set; }
        [J("trialWatch")] public AutoExtend TrialWatch { get; set; }
        [J("videoQuote")] public AutoExtend VideoQuote { get; set; }
        [J("autoExtend")] public AutoExtend AutoExtend { get; set; }
        [J("nicobus")] public Nicobus Nicobus { get; set; }
        [J("dmc")] public Dmc Dmc { get; set; }
        [J("frontendPublicApiUrl")] public Uri FrontendPublicApiUrl { get; set; }
        [J("commonResourcesBaseUrl")] public Uri CommonResourcesBaseUrl { get; set; }
        [J("gift")] public Gift Gift { get; set; }
        [J("creatorPromotionProgram")] public CreatorPromotionProgram CreatorPromotionProgram { get; set; }
        [J("stream")] public SiteStream Stream { get; set; }
        [J("performance")] public Performance Performance { get; set; }
        [J("nico")] public Nico Nico { get; set; }
        [J("akashic")] public SiteAkashic Akashic { get; set; }
        [J("device")] public Device Device { get; set; }
    }

    public partial class SiteAd
    {
        [J("adsApiBaseUrl")] public Uri AdsApiBaseUrl { get; set; }
    }

    public partial class SiteAkashic
    {
        [J("switchRenderHelpPageUrl")] public Uri SwitchRenderHelpPageUrl { get; set; }
    }

    public partial class AutoExtend
    {
        [J("usageHelpPageUrl")] public string UsageHelpPageUrl { get; set; }
    }

    public partial class Broadcast
    {
        [J("usageHelpPageUrl")] public Uri UsageHelpPageUrl { get; set; }
        [J("stableBroadcastHelpPageUrl")] public Uri StableBroadcastHelpPageUrl { get; set; }
        [J("niconicoLiveEncoder")] public Nair NiconicoLiveEncoder { get; set; }
        [J("nair")] public Nair Nair { get; set; }
        [J("broadcasterStreamHelpPageUrl")] public Uri BroadcasterStreamHelpPageUrl { get; set; }
    }

    public partial class Nair
    {
        [J("downloadPageUrl")] public Uri DownloadPageUrl { get; set; }
    }

    public partial class Coe
    {
        [J("resourcesBaseUrl")] public Uri ResourcesBaseUrl { get; set; }
        [J("coeContentBaseUrl")] public Uri CoeContentBaseUrl { get; set; }
    }

    public partial class CreatorPromotionProgram
    {
        [J("registrationHelpPageUrl")] public Uri RegistrationHelpPageUrl { get; set; }
    }

    public partial class Device
    {
        [J("watchOnPlayStation4HelpPageUrl")] public Uri WatchOnPlayStation4HelpPageUrl { get; set; }
        [J("safariCantWatchHelpPageUrl")] public Uri SafariCantWatchHelpPageUrl { get; set; }
    }

    public partial class Dmc
    {
        [J("webRtc")] public WebRtc WebRtc { get; set; }
    }

    public partial class WebRtc
    {
        [J("stunServerUrls")] public List<object> StunServerUrls { get; set; }
    }

    public partial class Environments
    {
        [J("runningMode")] public string RunningMode { get; set; }
    }

    public partial class FamilyService
    {
        [J("account")] public Account Account { get; set; }
        [J("app")] public App App { get; set; }
        [J("atsumaru")] public App Atsumaru { get; set; }
        [J("blomaga")] public App Blomaga { get; set; }
        [J("channel")] public FamilyServiceChannel Channel { get; set; }
        [J("commons")] public App Commons { get; set; }
        [J("community")] public App Community { get; set; }
        [J("denfaminicogamer")] public App Denfaminicogamer { get; set; }
        [J("dic")] public App Dic { get; set; }
        [J("help")] public Help Help { get; set; }
        [J("ichiba")] public Ichiba Ichiba { get; set; }
        [J("jk")] public App Jk { get; set; }
        [J("mastodon")] public App Mastodon { get; set; }
        [J("news")] public App News { get; set; }
        [J("nicoad")] public FamilyServiceNicoad Nicoad { get; set; }
        [J("niconico")] public Niconico Niconico { get; set; }
        [J("niconicoQ")] public App NiconicoQ { get; set; }
        [J("point")] public Point Point { get; set; }
        [J("seiga")] public Seiga Seiga { get; set; }
        [J("site")] public FamilyServiceSite Site { get; set; }
        [J("solid")] public App Solid { get; set; }
        [J("uad")] public App Uad { get; set; }
        [J("video")] public Video Video { get; set; }
        [J("faq")] public Bugreport Faq { get; set; }
        [J("bugreport")] public Bugreport Bugreport { get; set; }
        [J("rightsControlProgram")] public Bugreport RightsControlProgram { get; set; }
        [J("licenseSearch")] public Bugreport LicenseSearch { get; set; }
        [J("info")] public Info Info { get; set; }
        [J("search")] public Search Search { get; set; }
        [J("nicoex")] public Nicoex Nicoex { get; set; }
        [J("akashic")] public FamilyServiceAkashic Akashic { get; set; }
        [J("superichiba")] public FamilyServiceSuperichiba Superichiba { get; set; }
        [J("nAir")] public App NAir { get; set; }
        [J("prizeBox")] public App PrizeBox { get; set; }
        [J("emotion")] public FamilyServiceEmotion Emotion { get; set; }
    }

    public partial class Account
    {
        [J("accountRegistrationPageUrl")] public Uri AccountRegistrationPageUrl { get; set; }
        [J("loginPageUrl")] public Uri LoginPageUrl { get; set; }
        [J("logoutPageUrl")] public Uri LogoutPageUrl { get; set; }
        [J("premiumMemberRegistrationPageUrl")] public Uri PremiumMemberRegistrationPageUrl { get; set; }
        [J("trackingParams")] public TrackingParams TrackingParams { get; set; }
        [J("profileRegistrationPageUrl")] public Uri ProfileRegistrationPageUrl { get; set; }
        [J("contactsPageUrl")] public Uri ContactsPageUrl { get; set; }
        [J("verifyEmailsPageUrl")] public Uri VerifyEmailsPageUrl { get; set; }
        [J("accountSettingPageUrl")] public Uri AccountSettingPageUrl { get; set; }
        [J("currentPageUrl")] public string CurrentPageUrl { get; set; }
    }

    public partial class TrackingParams
    {
        [J("siteId")] public string SiteId { get; set; }
        [J("pageId")] public string PageId { get; set; }
        [J("mode")] public string Mode { get; set; }
        [J("programStatus")] public string ProgramStatus { get; set; }
    }

    public partial class FamilyServiceAkashic
    {
        [J("untrustedFrameUrl")] public Uri UntrustedFrameUrl { get; set; }
    }

    public partial class App
    {
        [J("topPageUrl")] public Uri TopPageUrl { get; set; }
    }

    public partial class Bugreport
    {
        [J("pageUrl")] public Uri PageUrl { get; set; }
    }

    public partial class FamilyServiceChannel
    {
        [J("topPageUrl")] public Uri TopPageUrl { get; set; }
        [J("forOrganizationAndCompanyPageUrl")] public Uri ForOrganizationAndCompanyPageUrl { get; set; }
    }

    public partial class FamilyServiceEmotion
    {
        [J("baseUrl")] public Uri BaseUrl { get; set; }
    }

    public partial class Help
    {
        [J("liveHelpPageUrl")] public Uri LiveHelpPageUrl { get; set; }
        [J("systemRequirementsPageUrl")] public Uri SystemRequirementsPageUrl { get; set; }
    }

    public partial class Ichiba
    {
        [J("configBaseUrl")] public Uri ConfigBaseUrl { get; set; }
        [J("scriptUrl")] public Uri ScriptUrl { get; set; }
        [J("topPageUrl")] public Uri TopPageUrl { get; set; }
    }

    public partial class Info
    {
        [J("warnForPhishingPageUrl")] public Uri WarnForPhishingPageUrl { get; set; }
        [J("smartphoneSdkPageUrl")] public Uri SmartphoneSdkPageUrl { get; set; }
        [J("nintendoGuidelinePageUrl")] public Uri NintendoGuidelinePageUrl { get; set; }
    }

    public partial class FamilyServiceNicoad
    {
        [J("topPageUrl")] public Uri TopPageUrl { get; set; }
        [J("apiBaseUrl")] public Uri ApiBaseUrl { get; set; }
    }

    public partial class Nicoex
    {
        [J("apiBaseUrl")] public Uri ApiBaseUrl { get; set; }
    }

    public partial class Niconico
    {
        [J("topPageUrl")] public Uri TopPageUrl { get; set; }
        [J("userPageBaseUrl")] public Uri UserPageBaseUrl { get; set; }
    }

    public partial class Point
    {
        [J("topPageUrl")] public Uri TopPageUrl { get; set; }
        [J("purchasePageUrl")] public Uri PurchasePageUrl { get; set; }
    }

    public partial class Search
    {
        [J("suggestionApiUrl")] public Uri SuggestionApiUrl { get; set; }
    }

    public partial class Seiga
    {
        [J("topPageUrl")] public Uri TopPageUrl { get; set; }
        [J("seigaPageBaseUrl")] public Uri SeigaPageBaseUrl { get; set; }
        [J("comicPageBaseUrl")] public Uri ComicPageBaseUrl { get; set; }
    }

    public partial class FamilyServiceSite
    {
        [J("serviceListPageUrl")] public Uri ServiceListPageUrl { get; set; }
        [J("salesAdvertisingPageUrl")] public Uri SalesAdvertisingPageUrl { get; set; }
        [J("liveAppDownloadPageUrl")] public Uri LiveAppDownloadPageUrl { get; set; }
    }

    public partial class FamilyServiceSuperichiba
    {
        [J("apiBaseUrl")] public Uri ApiBaseUrl { get; set; }
        [J("launchApiBaseUrl")] public Uri LaunchApiBaseUrl { get; set; }
        [J("oroshiuriIchibaBaseUrl")] public Uri OroshiuriIchibaBaseUrl { get; set; }
    }

    public partial class Video
    {
        [J("topPageUrl")] public Uri TopPageUrl { get; set; }
        [J("myPageUrl")] public Uri MyPageUrl { get; set; }
        [J("watchPageBaseUrl")] public Uri WatchPageBaseUrl { get; set; }
    }

    public partial class Gift
    {
        [J("cantOpenPageCausedAdBlockHelpPageUrl")] public Uri CantOpenPageCausedAdBlockHelpPageUrl { get; set; }
    }

    public partial class Information
    {
        [J("html5PlayerInformationPageUrl")] public Uri Html5PlayerInformationPageUrl { get; set; }
        [J("flashPlayerInstallInformationPageUrl")] public Uri FlashPlayerInstallInformationPageUrl { get; set; }
        [J("maintenanceInformationPageUrl")] public Uri MaintenanceInformationPageUrl { get; set; }
    }

    public partial class Nico
    {
        [J("webPushNotificationReceiveSettingHelpPageUrl")] public Uri WebPushNotificationReceiveSettingHelpPageUrl { get; set; }
    }

    public partial class Nicobus
    {
        [J("publicApiBaseUrl")] public Uri PublicApiBaseUrl { get; set; }
    }

    public partial class Notify
    {
        [J("unreadApiUrl")] public Uri UnreadApiUrl { get; set; }
        [J("contentApiUrl")] public Uri ContentApiUrl { get; set; }
        [J("updateUnreadIntervalMs")] public long UpdateUnreadIntervalMs { get; set; }
    }

    public partial class Performance
    {
        [J("commentRender")] public CommentRender CommentRender { get; set; }
    }

    public partial class CommentRender
    {
        [J("liteModeHelpPageUrl")] public Uri LiteModeHelpPageUrl { get; set; }
    }

    public partial class SiteProgram
    {
        [J("liveCount")] public long LiveCount { get; set; }
    }

    public partial class Relive
    {
        [J("apiBaseUrl")] public Uri ApiBaseUrl { get; set; }
        [J("webSocketUrl")] public string WebSocketUrl { get; set; }
        [J("csrfToken")] public string CsrfToken { get; set; }
        [J("audienceToken")] public string AudienceToken { get; set; }
    }

    public partial class Rule
    {
        [J("agreementPageUrl")] public Uri AgreementPageUrl { get; set; }
        [J("guidelinePageUrl")] public Uri GuidelinePageUrl { get; set; }
    }

    public partial class Spec
    {
        [J("watchUsageAndDevicePageUrl")] public Uri WatchUsageAndDevicePageUrl { get; set; }
        [J("broadcastUsageDevicePageUrl")] public Uri BroadcastUsageDevicePageUrl { get; set; }
        [J("minogashiProgramPageUrl")] public Uri MinogashiProgramPageUrl { get; set; }
        [J("cruisePageUrl")] public Uri CruisePageUrl { get; set; }
    }

    public partial class SiteStream
    {
        [J("lowLatencyHelpPageUrl")] public Uri LowLatencyHelpPageUrl { get; set; }
    }

    public partial class SiteTag
    {
        [J("revisionCheckIntervalMs")] public long RevisionCheckIntervalMs { get; set; }
        [J("registerHelpPageUrl")] public Uri RegisterHelpPageUrl { get; set; }
        [J("userRegistrableMax")] public long UserRegistrableMax { get; set; }
        [J("textMaxLength")] public long TextMaxLength { get; set; }
    }

    public partial class Timeshift
    {
        [J("reservationDetailListApiUrl")] public Uri ReservationDetailListApiUrl { get; set; }
    }

    public partial class SocialGroup
    {
        [J("type")] public ProviderType Type { get; set; }
        [J("id")] public string Id { get; set; }
        [J("broadcastHistoryPageUrl")] public Uri BroadcastHistoryPageUrl { get; set; }
        [J("description")] public string Description { get; set; }
        [J("name")] public string Name { get; set; }
        [J("socialGroupPageUrl")] public Uri SocialGroupPageUrl { get; set; }
        [J("thumbnailImageUrl")] public Uri ThumbnailImageUrl { get; set; }
        [J("thumbnailSmallImageUrl")] public Uri ThumbnailSmallImageUrl { get; set; }
        [J("companyName")] public string CompanyName { get; set; }
        [J("isPayChannel")] public bool? IsPayChannel { get; set; }
        [J("isFollowed")] public bool IsFollowed { get; set; }
        [J("isJoined")] public bool? IsJoined { get; set; }
        [J("level")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] 
        public long? Level { get; set; }
    }

    public partial class SupplierProgram
    {
        [J("onair")] public Onair Onair { get; set; }
    }

    public partial class Onair
    {
        [J("programId")] public string ProgramId { get; set; }
        [J("programUrl")] public Uri ProgramUrl { get; set; }
    }

    public partial class User
    {
        [J("isExplicitlyLoginable")] public bool IsExplicitlyLoginable { get; set; }
        [J("isMobileMailAddressRegistered")] public bool IsMobileMailAddressRegistered { get; set; }
        [J("isMailRegistered")] public bool IsMailRegistered { get; set; }
        [J("isProfileRegistered")] public bool IsProfileRegistered { get; set; }
        [J("isLoggedIn")] public bool IsLoggedIn { get; set; }
        [J("accountType")] public string AccountType { get; set; }
        [J("isOperator")] public bool IsOperator { get; set; }
        [J("isBroadcaster")] public bool IsBroadcaster { get; set; }
        [J("isTrialWatchTarget")] public bool? IsTrialWatchTarget { get; set; }

        [J("premiumOrigin")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long? PremiumOrigin { get; set; }

        [J("permissions")] public List<string> Permissions { get; set; }
        [J("nicosid")] public string Nicosid { get; set; }
        [J("superichiba")] public UserSuperichiba Superichiba { get; set; }

        [J("id")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long? Id { get; set; }

        [J("nickname")]
        public string Nickname { get; set; }

        [J("birthday")]
        public DateTimeOffset? Birthday { get; set; }
    }

    public partial class UserSuperichiba
    {
        [J("deletable")] public bool Deletable { get; set; }
        [J("hasBroadcasterRole")] public bool HasBroadcasterRole { get; set; }
    }

    public partial class UserProgramReservation
    {
        [J("isReserved")] public bool IsReserved { get; set; }
    }

    public partial class UserProgramWatch
    {
        [J("rejectedReasons")] public List<string> RejectedReasons { get; set; }
        [J("expireTime")] public object ExpireTime { get; set; }
        [J("canAutoRefresh")] public bool CanAutoRefresh { get; set; }


        public bool CanWatch => !RejectedReasons.Any();

        public bool RejectWithNotLogin => RejectedReasons.Contains(RejectedReasonHelper.NotLogin);
        public bool RejectWithProgramNotBegun => RejectedReasons.Contains(RejectedReasonHelper.NotSocialGroupMember);
        public bool RejectWithNotSocialGroupMember => RejectedReasons.Contains(RejectedReasonHelper.NotSocialGroupMember);
        public bool RejectWithNotHaveTimeshiftTicket => RejectedReasons.Contains(RejectedReasonHelper.NotHaveTimeshiftTicket);
        public bool RejectWithNotUseTimeshiftTicket => RejectedReasons.Contains(RejectedReasonHelper.NotUseTimeshiftTicket);
    }

    public static class RejectedReasonHelper
    {
        public static readonly string NotLogin = "notLogin";
        public static readonly string NoTimeshiftProgram = "noTimeshiftProgram";
        public static readonly string ProgramNotBegun = "programNotBegun";
        public static readonly string NotSocialGroupMember = "notSocialGroupMember";
        public static readonly string NotHaveTimeshiftTicket = "notHaveTimeshiftTicket";
        public static readonly string NotUseTimeshiftTicket = "notUseTimeshiftTicket";
    }

    public static class UserAccountTypeHelper
    {
        public static readonly string NotLogin = "non";
        public static readonly string NormalAccount = "standard";

    }

    public enum ProgramLiveStatus
    {
        ON_AIR,
        RELEASED,
        ENDED,
    }

    public enum RejectedReasonType
    {
        NotLogin,
        ProgramNotBegun,
        NotSocialGroupMember,
        NotHaveTimeshiftTicket
    }

    public enum TagType
    {
        Category,
    };

    public enum PurpleContentType { Live };

    public enum PurpleRecommendType { ChannelLive, HostLiveOnair, RelatedLiveOnair, RelatedLivePast, Rookie, Wakutkool, SugoiSearch, RelatedLiveReserved };

    public enum Status { MemberOnly, Public };

    public enum PermissionType
    {
        All, Author, Member, Purchaser, PurchaserOrMember
    };

    public enum FluffyContentType { Video, Seiga, Manga };

    public enum FluffyRecommendType { SugoiSearch, Wakutkool };
}
