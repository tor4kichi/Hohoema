using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.Video.Watch
{
    public partial class DmcWatchApiData
    {
        [JsonPropertyName("ads")]
        public object Ads { get; set; }

        [JsonPropertyName("category")]
        public object Category { get; set; }

        [JsonPropertyName("channel")]
        public Channel Channel { get; set; }

        [JsonPropertyName("client")]
        public Client Client { get; set; }

        [JsonPropertyName("comment")]
        public Comment Comment { get; set; }

        [JsonPropertyName("community")]
        public object Community { get; set; }

        [JsonPropertyName("easyComment")]
        public EasyComment EasyComment { get; set; }

        [JsonPropertyName("external")]
        public External External { get; set; }

        [JsonPropertyName("genre")]
        public WelcomeGenre Genre { get; set; }

        [JsonPropertyName("marquee")]
        public Marquee Marquee { get; set; }

        [JsonPropertyName("media")]
        public Media Media { get; set; }

        [JsonPropertyName("okReason")]
        public string OkReason { get; set; }

        [JsonPropertyName("owner")]
        public VideoOwner Owner { get; set; }

        [JsonPropertyName("payment")]
        public Payment Payment { get; set; }

        [JsonPropertyName("pcWatchPage")]
        public PcWatchPage PcWatchPage { get; set; }

        [JsonPropertyName("player")]
        public Player Player { get; set; }

        [JsonPropertyName("ppv")]
        public Ppv Ppv { get; set; }

        [JsonPropertyName("ranking")]
        public Ranking Ranking { get; set; }

        [JsonPropertyName("series")]
        public WatchApiSeries Series { get; set; }

        [JsonPropertyName("smartphone")]
        public object Smartphone { get; set; }

        [JsonPropertyName("system")]
        public SystemClass System { get; set; }

        [JsonPropertyName("tag")]
        public Tag Tag { get; set; }

        [JsonPropertyName("video")]
        public WatchApiVideo Video { get; set; }

        [JsonPropertyName("videoAds")]
        public VideoAds VideoAds { get; set; }

        [JsonPropertyName("videoLive")]
        public object VideoLive { get; set; }

        [JsonPropertyName("viewer")]
        public WelcomeViewer Viewer { get; set; }

        [JsonPropertyName("waku")]
        public Waku Waku { get; set; }
    }

    public class Ads
    {
        [JsonPropertyName("isAvailable")]
        public bool IsAvailable { get; set; }
    }

    public partial class Channel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("isOfficialAnime")]
        public bool IsOfficialAnime { get; set; }

        [JsonPropertyName("isDisplayAdBanner")]
        public bool IsDisplayAdBanner { get; set; }

        [JsonPropertyName("thumbnail")]
        public ChannelThumbnail Thumbnail { get; set; }

        [JsonPropertyName("viewer")]
        public ChannelViewer Viewer { get; set; }
    }

    public partial class ChannelThumbnail
    {
        [JsonPropertyName("url")]
        public Uri Url { get; set; }

        [JsonPropertyName("smallUrl")]
        public Uri SmallUrl { get; set; }
    }

    public partial class ChannelViewer
    {
        [JsonPropertyName("follow")]
        public Follow Follow { get; set; }
    }

    public partial class Follow
    {
        [JsonPropertyName("isFollowed")]
        public bool IsFollowed { get; set; }

        [JsonPropertyName("isBookmarked")]
        public bool IsBookmarked { get; set; }

        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("tokenTimestamp")]
        public int TokenTimestamp { get; set; }
    }

    public partial class Client
    {
        [JsonPropertyName("nicosid")]
        public string Nicosid { get; set; }

        [JsonPropertyName("watchId")]
        public string WatchId { get; set; }

        [JsonPropertyName("watchTrackId")]
        public string WatchTrackId { get; set; }
    }

    public partial class Comment
    {
        [JsonPropertyName("server")]
        public Server Server { get; set; }

        [JsonPropertyName("keys")]
        public Keys Keys { get; set; }

        [JsonPropertyName("layers")]
        public Layer[] Layers { get; set; }

        [JsonPropertyName("threads")]
        public Thread[] Threads { get; set; }

        [JsonPropertyName("ng")]
        public Ng Ng { get; set; }

        [JsonPropertyName("isAttentionRequired")]
        public bool IsAttentionRequired { get; set; }
    }

    public partial class Keys
    {
        [JsonPropertyName("userKey")]
        public string UserKey { get; set; }
    }

    public partial class Layer
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("isTranslucent")]
        public bool IsTranslucent { get; set; }

        [JsonPropertyName("threadIds")]
        public ThreadId[] ThreadIds { get; set; }
    }

    public partial class ThreadId
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("fork")]
        public int Fork { get; set; }
    }

    public partial class Ng
    {
        [JsonPropertyName("ngScore")]
        public NgScore NgScore { get; set; }

        [JsonPropertyName("channel")]
        public object[] Channel { get; set; }

        [JsonPropertyName("owner")]
        public object[] Owner { get; set; }

        [JsonPropertyName("viewer")]
        public NgViewer Viewer { get; set; }
    }

    public partial class NgScore
    {
        [JsonPropertyName("isDisabled")]
        public bool IsDisabled { get; set; }
    }

    public partial class NgViewer
    {
        [JsonPropertyName("revision")]
        public int Revision { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("items")]
        public object[] Items { get; set; }
    }

    public partial class Server
    {
        [JsonPropertyName("url")]
        public Uri Url { get; set; }
    }

    public partial class Thread
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("fork")]
        public int Fork { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        [JsonPropertyName("isDefaultPostTarget")]
        public bool IsDefaultPostTarget { get; set; }

        [JsonPropertyName("isEasyCommentPostTarget")]
        public bool IsEasyCommentPostTarget { get; set; }

        [JsonPropertyName("isLeafRequired")]
        public bool IsLeafRequired { get; set; }

        [JsonPropertyName("isOwnerThread")]
        public bool IsOwnerThread { get; set; }

        [JsonPropertyName("isThreadkeyRequired")]
        public bool IsThreadkeyRequired { get; set; }

        [JsonPropertyName("threadkey")]
        public object Threadkey { get; set; }

        [JsonPropertyName("is184Forced")]
        public bool Is184Forced { get; set; }

        [JsonPropertyName("hasNicoscript")]
        public bool HasNicoscript { get; set; }

        [JsonPropertyName("label")]
        public string Label { get; set; }

        [JsonPropertyName("postkeyStatus")]
        public int PostkeyStatus { get; set; }

        [JsonPropertyName("server")]
        public Uri Server { get; set; }
    }

    public partial class EasyComment
    {
        [JsonPropertyName("phrases")]
        public Phrase[] Phrases { get; set; }
    }

    public partial class Phrase
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("nicodic")]
        public Nicodic Nicodic { get; set; }
    }

    public partial class Nicodic
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("viewTitle")]
        public string ViewTitle { get; set; }

        [JsonPropertyName("summary")]
        public string Summary { get; set; }

        [JsonPropertyName("link")]
        public Uri Link { get; set; }
    }

    public partial class External
    {
        [JsonPropertyName("commons")]
        public Commons Commons { get; set; }

        [JsonPropertyName("ichiba")]
        public Ichiba Ichiba { get; set; }
    }

    public partial class Commons
    {
        [JsonPropertyName("hasContentTree")]
        public bool HasContentTree { get; set; }
    }

    public partial class Ichiba
    {
        [JsonPropertyName("isEnabled")]
        public bool IsEnabled { get; set; }
    }

    public partial class WelcomeGenre
    {
        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonPropertyName("label")]
        public string Label { get; set; }

        [JsonPropertyName("isImmoral")]
        public bool IsImmoral { get; set; }

        [JsonPropertyName("isDisabled")]
        public bool IsDisabled { get; set; }

        [JsonPropertyName("isNotSet")]
        public bool IsNotSet { get; set; }
    }

    public partial class Marquee
    {
        [JsonPropertyName("isDisabled")]
        public bool IsDisabled { get; set; }

        [JsonPropertyName("tagRelatedLead")]
        public object TagRelatedLead { get; set; }
    }

    public partial class Media
    {
        [JsonPropertyName("delivery")]
        public Delivery Delivery { get; set; }

        [JsonPropertyName("deliveryLegacy")]
        public object DeliveryLegacy { get; set; }
    }

    public partial class Delivery
    {
        [JsonPropertyName("recipeId")]
        public string RecipeId { get; set; }

        [JsonPropertyName("encryption")]
        public Encryption Encryption { get; set; }

        [JsonPropertyName("movie")]
        public Movie Movie { get; set; }

        [JsonPropertyName("storyboard")]
        public object Storyboard { get; set; }

        [JsonPropertyName("trackingId")]
        public string TrackingId { get; set; }
    }


    public class Encryption
    {
        [JsonPropertyName("encryptedKey")]
        public string EncryptedKey { get; set; }

        [JsonPropertyName("keyUri")]
        public string KeyUri { get; set; }
    }

    public partial class Movie
    {
        [JsonPropertyName("contentId")]
        public string ContentId { get; set; }

        [JsonPropertyName("audios")]
        public AudioContent[] Audios { get; set; }

        [JsonPropertyName("videos")]
        public VideoContent[] Videos { get; set; }

        [JsonPropertyName("session")]
        public Session Session { get; set; }
    }

    public partial class AudioContent
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("isAvailable")]
        public bool IsAvailable { get; set; }

        [JsonPropertyName("metadata")]
        public AudioMetadata Metadata { get; set; }
    }

    public partial class AudioMetadata
    {
        [JsonPropertyName("bitrate")]
        public long Bitrate { get; set; }

        [JsonPropertyName("samplingRate")]
        public long SamplingRate { get; set; }

        [JsonPropertyName("loudness")]
        public Loudness Loudness { get; set; }

        [JsonPropertyName("levelIndex")]
        public long LevelIndex { get; set; }

        [JsonPropertyName("loudnessCollection")]
        public LoudnessCollection[] LoudnessCollection { get; set; }
    }

    public partial class Loudness
    {
        [JsonPropertyName("integratedLoudness")]
        public double IntegratedLoudness { get; set; }

        [JsonPropertyName("truePeak")]
        public double TruePeak { get; set; }
    }

    public partial class LoudnessCollection
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("value")]
        public double Value { get; set; }
    }

    public partial class Session
    {
        [JsonPropertyName("recipeId")]
        public string RecipeId { get; set; }

        [JsonPropertyName("playerId")]
        public string PlayerId { get; set; }

        [JsonPropertyName("videos")]
        public string[] Videos { get; set; }

        [JsonPropertyName("audios")]
        public string[] Audios { get; set; }

        [JsonPropertyName("movies")]
        public object[] Movies { get; set; }

        [JsonPropertyName("protocols")]
        public string[] Protocols { get; set; }

        [JsonPropertyName("authTypes")]
        public AuthTypes AuthTypes { get; set; }

        [JsonPropertyName("serviceUserId")]
        public string ServiceUserId { get; set; }

        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("signature")]
        public string Signature { get; set; }

        [JsonPropertyName("contentId")]
        public string ContentId { get; set; }

        [JsonPropertyName("heartbeatLifetime")]
        public long HeartbeatLifetime { get; set; }

        [JsonPropertyName("contentKeyTimeout")]
        public long ContentKeyTimeout { get; set; }

        [JsonPropertyName("priority")]
        public double Priority { get; set; }

        [JsonPropertyName("transferPresets")]
        public object[] TransferPresets { get; set; }

        [JsonPropertyName("urls")]
        public Url[] Urls { get; set; }
    }

    public partial class AuthTypes
    {
        [JsonPropertyName("http")]
        public string Http { get; set; }

        [JsonPropertyName("hls")]
        public string Hls { get; set; }
    }

    public partial class Url
    {
        [JsonPropertyName("url")]
        public Uri UrlUrl { get; set; }

        [JsonPropertyName("isWellKnownPort")]
        public bool IsWellKnownPort { get; set; }

        [JsonPropertyName("isSsl")]
        public bool IsSsl { get; set; }

        public string UrlUnsafe => "https://api.dmc.nico/api/sessions";
    }

    public partial class VideoContent
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("isAvailable")]
        public bool IsAvailable { get; set; }

        [JsonPropertyName("metadata")]
        public VideoMetadata Metadata { get; set; }
    }

    public partial class VideoMetadata
    {
        [JsonPropertyName("label")]
        public string Label { get; set; }

        [JsonPropertyName("bitrate")]
        public long Bitrate { get; set; }

        [JsonPropertyName("resolution")]
        public Resolution Resolution { get; set; }

        [JsonPropertyName("levelIndex")]
        public long LevelIndex { get; set; }

        [JsonPropertyName("recommendedHighestAudioLevelIndex")]
        public long RecommendedHighestAudioLevelIndex { get; set; }
    }

    public partial class Resolution
    {
        [JsonPropertyName("width")]
        public long Width { get; set; }

        [JsonPropertyName("height")]
        public long Height { get; set; }
    }

    public partial class VideoOwner
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }

        [JsonPropertyName("iconUrl")]
        public Uri IconUrl { get; set; }

        [JsonPropertyName("channel")]
        public VideoOwnerChannel Channel { get; set; }

        [JsonPropertyName("live")]
        public object Live { get; set; }

        [JsonPropertyName("isVideosPublic")]
        public bool IsVideosPublic { get; set; }

        [JsonPropertyName("isMylistsPublic")]
        public bool IsMylistsPublic { get; set; }

        [JsonPropertyName("videoLiveNotice")]
        public object VideoLiveNotice { get; set; }

        [JsonPropertyName("viewer")]
        public OwnerViewer Viewer { get; set; }
    }

    public partial class VideoOwnerChannel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("url")]
        public Uri Url { get; set; }
    }

    public partial class OwnerViewer
    {
        [JsonPropertyName("isFollowing")]
        public bool IsFollowing { get; set; }
    }

    public partial class Payment
    {
        [JsonPropertyName("video")]
        public PaymentVideo Video { get; set; }

        [JsonPropertyName("preview")]
        public Preview Preview { get; set; }
    }

    public partial class Preview
    {
        [JsonPropertyName("ppv")]
        public Ichiba Ppv { get; set; }

        [JsonPropertyName("admission")]
        public Ichiba Admission { get; set; }

        [JsonPropertyName("premium")]
        public Ichiba Premium { get; set; }
    }

    public partial class PaymentVideo
    {
        [JsonPropertyName("isPpv")]
        public bool IsPpv { get; set; }

        [JsonPropertyName("isAdmission")]
        public bool IsAdmission { get; set; }

        [JsonPropertyName("isPremium")]
        public bool IsPremium { get; set; }

        [JsonPropertyName("watchableUserType")]
        public string WatchableUserType { get; set; }

        [JsonPropertyName("commentableUserType")]
        public string CommentableUserType { get; set; }
    }

    public partial class PcWatchPage
    {
        [JsonPropertyName("tagRelatedBanner")]
        public object TagRelatedBanner { get; set; }

        [JsonPropertyName("videoEnd")]
        public VideoEnd VideoEnd { get; set; }

        [JsonPropertyName("showOwnerMenu")]
        public bool ShowOwnerMenu { get; set; }

        [JsonPropertyName("showOwnerThreadCoEditingLink")]
        public bool ShowOwnerThreadCoEditingLink { get; set; }

        [JsonPropertyName("showMymemoryEditingLink")]
        public bool ShowMymemoryEditingLink { get; set; }
    }

    public partial class VideoEnd
    {
        [JsonPropertyName("bannerIn")]
        public object BannerIn { get; set; }

        [JsonPropertyName("overlay")]
        public object Overlay { get; set; }
    }

    public partial class Player
    {
        [JsonPropertyName("initialPlayback")]
        public object InitialPlayback { get; set; }

        [JsonPropertyName("comment")]
        public PlayerComment Comment { get; set; }

        [JsonPropertyName("layerMode")]
        public int LayerMode { get; set; }
    }

    public partial class PlayerComment
    {
        [JsonPropertyName("isDefaultInvisible")]
        public bool IsDefaultInvisible { get; set; }
    }

    public partial class Ppv
    {
        [JsonPropertyName("accessFrom")]
        public object AccessFrom { get; set; }
    }

    public partial class Ranking
    {
        [JsonPropertyName("genre")]
        public RankingGenre Genre { get; set; }

        [JsonPropertyName("popularTag")]
        public PopularTag[] PopularTag { get; set; }
    }

    public partial class RankingGenre
    {
        [JsonPropertyName("rank")]
        public int Rank { get; set; }

        [JsonPropertyName("genre")]
        public string Genre { get; set; }

        [JsonPropertyName("dateTime")]
        public DateTimeOffset DateTime { get; set; }
    }

    public partial class PopularTag
    {
        [JsonPropertyName("tag")]
        public string Tag { get; set; }

        [JsonPropertyName("regularizedTag")]
        public string RegularizedTag { get; set; }

        [JsonPropertyName("rank")]
        public int Rank { get; set; }

        [JsonPropertyName("genre")]
        public string Genre { get; set; }

        [JsonPropertyName("dateTime")]
        public DateTimeOffset DateTime { get; set; }
    }

    public partial class WatchApiSeries
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("thumbnailUrl")]
        public Uri ThumbnailUrl { get; set; }

        [JsonPropertyName("video")]
        public SeriesVideo Video { get; set; }
    }

    public partial class SeriesVideo
    {
        [JsonPropertyName("prev")]
        public NvapiVideoItem Prev { get; set; }

        [JsonPropertyName("next")]
        public NvapiVideoItem Next { get; set; }

        [JsonPropertyName("first")]
        public NvapiVideoItem First { get; set; }
    }

    public partial class SystemClass
    {
        [JsonPropertyName("serverTime")]
        public DateTimeOffset ServerTime { get; set; }

        [JsonPropertyName("isPeakTime")]
        public bool IsPeakTime { get; set; }
    }

    public partial class Tag
    {
        [JsonPropertyName("items")]
        public TagItem[] Items { get; set; }

        [JsonPropertyName("hasR18Tag")]
        public bool HasR18Tag { get; set; }

        [JsonPropertyName("isPublishedNicoscript")]
        public bool IsPublishedNicoscript { get; set; }

        [JsonPropertyName("edit")]
        public TagEdit Edit { get; set; }

        [JsonPropertyName("viewer")]
        public TagEdit Viewer { get; set; }
    }

    public partial class TagEdit
    {
        [JsonPropertyName("isEditable")]
        public bool IsEditable { get; set; }

        [JsonPropertyName("uneditableReason")]
        public string UneditableReason { get; set; }

        [JsonPropertyName("editKey")]
        public string EditKey { get; set; }
    }

    public partial class TagItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("isCategory")]
        public bool IsCategory { get; set; }

        [JsonPropertyName("isCategoryCandidate")]
        public bool IsCategoryCandidate { get; set; }

        [JsonPropertyName("isNicodicArticleExists")]
        public bool IsNicodicArticleExists { get; set; }

        [JsonPropertyName("isLocked")]
        public bool IsLocked { get; set; }
    }

    public partial class WatchApiVideo
    {
        [JsonPropertyName("id")]
        public VideoId Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("count")]
        public Count Count { get; set; }

        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonPropertyName("thumbnail")]
        public VideoThumbnail Thumbnail { get; set; }

        [JsonPropertyName("rating")]
        public Rating Rating { get; set; }

        [JsonPropertyName("registeredAt")]
        public DateTimeOffset RegisteredAt { get; set; }

        [JsonPropertyName("isPrivate")]
        public bool IsPrivate { get; set; }

        [JsonPropertyName("isDeleted")]
        public bool IsDeleted { get; set; }

        [JsonPropertyName("isNoBanner")]
        public bool IsNoBanner { get; set; }

        [JsonPropertyName("isAuthenticationRequired")]
        public bool IsAuthenticationRequired { get; set; }

        [JsonPropertyName("isEmbedPlayerAllowed")]
        public bool IsEmbedPlayerAllowed { get; set; }

        [JsonPropertyName("viewer")]
        public VideoViewer Viewer { get; set; }

        [JsonPropertyName("watchableUserTypeForPayment")]
        public string WatchableUserTypeForPayment { get; set; }

        [JsonPropertyName("commentableUserTypeForPayment")]
        public string CommentableUserTypeForPayment { get; set; }

        [JsonPropertyName("9d091f87")]
        public bool The9D091F87 { get; set; }
    }

    public partial class Rating
    {
        [JsonPropertyName("isAdult")]
        public bool IsAdult { get; set; }
    }

    public partial class VideoThumbnail
    {
        [JsonPropertyName("url")]
        public Uri Url { get; set; }

        [JsonPropertyName("middleUrl")]
        public Uri MiddleUrl { get; set; }

        [JsonPropertyName("largeUrl")]
        public Uri LargeUrl { get; set; }

        [JsonPropertyName("player")]
        public Uri Player { get; set; }

        [JsonPropertyName("ogp")]
        public Uri Ogp { get; set; }
    }

    public partial class VideoViewer
    {
        [JsonPropertyName("isOwner")]
        public bool IsOwner { get; set; }

        [JsonPropertyName("like")]
        public Like Like { get; set; }
    }

    public partial class Like
    {
        [JsonPropertyName("isLiked")]
        public bool IsLiked { get; set; }

        [JsonPropertyName("count")]
        public object Count { get; set; }
    }

    public partial class VideoAds
    {
        [JsonPropertyName("additionalParams")]
        public AdditionalParams AdditionalParams { get; set; }

        [JsonPropertyName("items")]
        public object[] Items { get; set; }

        [JsonPropertyName("reason")]
        public object Reason { get; set; }
    }

    public partial class AdditionalParams
    {
        [JsonPropertyName("videoId")]
        public string VideoId { get; set; }

        [JsonPropertyName("videoDuration")]
        public int VideoDuration { get; set; }

        [JsonPropertyName("isAdultRatingNG")]
        public bool IsAdultRatingNg { get; set; }

        [JsonPropertyName("isAuthenticationRequired")]
        public bool IsAuthenticationRequired { get; set; }

        [JsonPropertyName("isR18")]
        public bool IsR18 { get; set; }

        [JsonPropertyName("nicosid")]
        public string Nicosid { get; set; }

        [JsonPropertyName("lang")]
        public string Lang { get; set; }

        [JsonPropertyName("watchTrackId")]
        public string WatchTrackId { get; set; }

        [JsonPropertyName("channelId")]
        public string ChannelId { get; set; }

        [JsonPropertyName("genre")]
        public string Genre { get; set; }

        [JsonPropertyName("gender")]
        [JsonConverter(typeof(LongToStringConverter))]
        public long Gender { get; set; }

        [JsonPropertyName("age")]
        public int Age { get; set; }
    }

    public partial class WelcomeViewer
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }

        [JsonPropertyName("isPremium")]
        public bool IsPremium { get; set; }

        [JsonPropertyName("existence")]
        public Existence Existence { get; set; }
    }

    public partial class Existence
    {
        [JsonPropertyName("age")]
        public int Age { get; set; }

        [JsonPropertyName("prefecture")]
        public string Prefecture { get; set; }

        [JsonPropertyName("sex")]
        public string Sex { get; set; }
    }

    public partial class Waku
    {
        [JsonPropertyName("information")]
        public object Information { get; set; }

        [JsonPropertyName("bgImages")]
        public object[] BgImages { get; set; }

        [JsonPropertyName("addContents")]
        public object AddContents { get; set; }

        [JsonPropertyName("addVideo")]
        public object AddVideo { get; set; }

        [JsonPropertyName("tagRelatedBanner")]
        public TagRelatedBanner TagRelatedBanner { get; set; }

        [JsonPropertyName("tagRelatedMarquee")]
        public TagRelatedMarquee TagRelatedMarquee { get; set; }
    }

    public partial class TagRelatedBanner
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("imageUrl")]
        public Uri ImageUrl { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("isEvent")]
        public bool IsEvent { get; set; }

        [JsonPropertyName("linkUrl")]
        public Uri LinkUrl { get; set; }

        [JsonPropertyName("isNewWindow")]
        public bool IsNewWindow { get; set; }
    }

    public partial class TagRelatedMarquee
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("linkUrl")]
        public Uri LinkUrl { get; set; }

        [JsonPropertyName("isNewWindow")]
        public bool IsNewWindow { get; set; }
    }
}
