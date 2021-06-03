using I18NPortable;
using Hohoema.Models.Domain;
using Hohoema.Models.Helpers;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Player;
using Hohoema.Models.Domain.Player.Video;
using Hohoema.Models.Domain.Player.Video.Comment;
using Hohoema.Presentation.Services;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.VideoCache;
using Hohoema.Models.Domain.Player.Video.Cache;
using System.Collections.Immutable;
using NiconicoToolkit.Video.Watch;
using NiconicoToolkit.Video;

namespace Hohoema.Models.UseCase.NicoVideos.Player
{

    public enum PlayingOrchestrateFailedReason
    {
        Unknown,
        Deleted,
        VideoFormatNotSupported,
        NotPlayPermit_RequirePay,
        NotPlayPermit_RequireChannelMember,
        NotPlayPermit_RequirePremiumMember,
        CacheVideo_RequirePremiumMember,
    }    

    public class CachedVideoSessionProvider : INiconicoVideoSessionProvider
    {
        private readonly VideoCacheItem _videoCacheItem;
        private readonly NiconicoSession _niconicoSession;

        public CachedVideoSessionProvider(VideoCacheItem videoCacheItem, NiconicoSession niconicoSession)
        {
            _videoCacheItem = videoCacheItem;
            _niconicoSession = niconicoSession;
            AvailableQualities = new []{ new NicoVideoQualityEntity(true, _videoCacheItem.DownloadedVideoQuality, _videoCacheItem.DownloadedVideoQuality.ToString()) }.ToImmutableArray();
        }

        public string ContentId => _videoCacheItem.VideoId;

        public ImmutableArray<NicoVideoQualityEntity> AvailableQualities { get; }

        public Task<IStreamingSession> CreateVideoSessionAsync(NicoVideoQuality quality)
        {
            return Task.FromResult((IStreamingSession)new CachedVideoStreamingSession(_videoCacheItem, _niconicoSession));
        }
    }



    public sealed class VideoStreamingOriginOrchestrator : BindableBase
    {
        public class PlayingOrchestrateResult
        {
            internal PlayingOrchestrateResult(PlayingOrchestrateFailedReason playingOrchestrateFailedReason)
            {
                IsSuccess = false;
                PlayingOrchestrateFailedReason = playingOrchestrateFailedReason;
            }

            internal PlayingOrchestrateResult(Exception exception)
            {
                IsSuccess = false;
                Exception = exception;
            }

            internal PlayingOrchestrateResult(INiconicoVideoSessionProvider vss, INiconicoCommentSessionProvider cs, INicoVideoDetails videoDetails)
            {
                IsSuccess = vss != null;
                VideoSessionProvider = vss;
                CommentSessionProvider = cs;
                VideoDetails = videoDetails;
            }

            public bool IsSuccess { get; }

            

            public INiconicoVideoSessionProvider VideoSessionProvider { get; }
            public INiconicoCommentSessionProvider CommentSessionProvider { get; }

            public INicoVideoDetails VideoDetails { get; }

            public Exception Exception { get; }
            public PlayingOrchestrateFailedReason PlayingOrchestrateFailedReason { get; }
        }



        public VideoStreamingOriginOrchestrator(
            NiconicoSession niconicoSession,
            VideoCacheManager videoCacheManager,
            NicoVideoSessionProvider nicoVideoSessionProvider,
            DialogService dialogService,
            VideoCacheCommentRepository commentRepository,
            NicoVideoProvider nicoVideoProvider
            )
        {
            _niconicoSession = niconicoSession;
            _videoCacheManager = videoCacheManager;
            _nicoVideoSessionProvider = nicoVideoSessionProvider;
            _dialogService = dialogService;
            _commentRepository = commentRepository;
            _nicoVideoProvider = nicoVideoProvider;
        }

        private readonly NiconicoSession _niconicoSession;
        private readonly VideoCacheManager _videoCacheManager;
        private readonly NicoVideoSessionProvider _nicoVideoSessionProvider;
        private readonly DialogService _dialogService;
        private readonly VideoCacheCommentRepository _commentRepository;
        private readonly NicoVideoProvider _nicoVideoProvider;
        private readonly NicoVideoCacheRepository _nicoVideoRepository;





        /// <summary>
        /// 再生処理
        /// </summary>
        /// <returns></returns>
        public async Task<PlayingOrchestrateResult> CreatePlayingOrchestrateResultAsync(string videoId)
        {
#if !DEBUG
            if (_videoCacheManager.IsCacheDownloadAuthorized() && _videoCacheManager.GetVideoCacheStatus(videoId) == VideoCacheStatus.Completed)
#else
            if (_videoCacheManager.GetVideoCacheStatus(videoId) == VideoCacheStatus.Completed)
#endif
            {
                return await PreperePlayWithCache(videoId);
            }
            else
            {
                return await PreperePlayWithOnline(videoId);
            }
        }

        public async Task<PlayingOrchestrateResult> PreperePlayWithCache(string videoId)
        {
            INiconicoCommentSessionProvider commentSessionProvider = null;
            INicoVideoDetails nicoVideoDetails = null;
            if (!InternetConnection.IsInternet())
            {
                var cachedComment = _commentRepository.GetCached(videoId);
                if (cachedComment != null)
                {
                    commentSessionProvider = new CachedCommentsProvider(videoId, cachedComment.Comments);
                }

                var (videoRes, nicoVideo) = await _nicoVideoProvider.GetVideoInfoAsync(videoId);
                if (videoRes != null)
                {
                    var video = videoRes.Video;
                    var details= new CachedVideoDetails()
                    {
                        VideoTitle = video.Title,
                        ViewCount = (int)video.ViewCounter,
                        CommentCount = (int)videoRes.Thread.NumRes,
                        MylistCount = (int)video.MylistCounter,
                        VideoLength = TimeSpan.FromSeconds(video.LengthInSeconds),
                        SubmitDate = video.FirstRetrieve.DateTime,
                        OwnerType = nicoVideo.Owner.UserType,
                        OwnerIconUrl = nicoVideo.Owner.IconUrl,
                        ProviderId = nicoVideo.Owner.OwnerId,
                        ProviderName = nicoVideo.Owner.ScreenName,
                        Tags = videoRes.Tags.TagInfo.Select(x => new NicoVideoTag(x.Tag)).ToArray(),
                        DescriptionHtml = video.Description,
                        ThumbnailUrl = video.ThumbnailUrl.OriginalString
                    };

                    

                    nicoVideoDetails = details;
                }
            }
            else
            {
                var preparePlayVideo = await _nicoVideoSessionProvider.PreparePlayVideoAsync(videoId);
                commentSessionProvider = preparePlayVideo;
                nicoVideoDetails = preparePlayVideo?.GetVideoDetails();
            }

            // キャッシュからコメントを取得する方法が必要
            return new PlayingOrchestrateResult(
                new CachedVideoSessionProvider(_videoCacheManager.GetVideoCache(videoId), _niconicoSession),
                commentSessionProvider,
                nicoVideoDetails
                );
        }

        public async Task<PlayingOrchestrateResult> PreperePlayWithOnline(string videoId)
        {
            var preparePlayVideo = await _nicoVideoSessionProvider.PreparePlayVideoAsync(videoId);
            if (preparePlayVideo.IsSuccess)
            {
                return new PlayingOrchestrateResult(
                    preparePlayVideo,
                    preparePlayVideo,
                    preparePlayVideo.GetVideoDetails()
                    );
            }

            if (preparePlayVideo.Exception is not null and var ex)
            {
                return new PlayingOrchestrateResult(ex);
            }
            else
            {
                return new PlayingOrchestrateResult(preparePlayVideo.FailedReason switch
                {
                    PreparePlayVideoFailedReason.Deleted => PlayingOrchestrateFailedReason.Deleted,
                    PreparePlayVideoFailedReason.VideoFormatNotSupported => PlayingOrchestrateFailedReason.VideoFormatNotSupported,
                    PreparePlayVideoFailedReason.NotPlayPermit_RequirePay => PlayingOrchestrateFailedReason.NotPlayPermit_RequirePay,
                    PreparePlayVideoFailedReason.NotPlayPermit_RequireChannelMember => PlayingOrchestrateFailedReason.NotPlayPermit_RequireChannelMember,
                    PreparePlayVideoFailedReason.NotPlayPermit_RequirePremiumMember => PlayingOrchestrateFailedReason.NotPlayPermit_RequirePremiumMember,
                    _ => throw new NotSupportedException("不明な理由で再生不可"),
                }); ;
            }
        }
    }

    public sealed class CachedVideoDetails : INicoVideoDetails
    {
        public string VideoTitle { get; set; }

        public NicoVideoTag[] Tags { get; set; }

        public string ThumbnailUrl { get; set; }

        public TimeSpan VideoLength { get; set; }

        public DateTime SubmitDate { get; set; }

        public int ViewCount { get; set; }

        public int CommentCount { get; set; }

        public int MylistCount { get; set; }

        public string ProviderId { get; set; }
        public string ProviderName { get; set; }
        public OwnerType OwnerType { get; set; }
        public string OwnerIconUrl { get; set; }

        public bool IsChannelOwnedVideo { get; set; }

        public string DescriptionHtml { get; set; }

        public double LoudnessCorrectionValue { get; set; }

        public bool IsSeriesVideo => false;

        public Series Series => null;

        public bool IsLikedVideo { get; set; }
    }


    public sealed class CachedCommentsProvider : INiconicoCommentSessionProvider
    {
        private readonly IReadOnlyCollection<VideoComment> _comments;

        public CachedCommentsProvider(string videoId, IReadOnlyCollection<VideoComment> comments)
        {
            ContentId = videoId;
            _comments = comments;
        }
        public string ContentId { get; }

        public Task<ICommentSession> CreateCommentSessionAsync()
        {
            return Task.FromResult(new OfflineVideoCommentSession(ContentId, _comments) as ICommentSession);
        }
    }

    public sealed class OfflineVideoCommentSession : ICommentSession
    {
        private readonly IReadOnlyCollection<VideoComment> _comments;

        public OfflineVideoCommentSession(string videoId, IReadOnlyCollection<VideoComment> comments)
        {
            ContentId = videoId;
            _comments = comments;
        }

        public string ContentId { get; }

        public string UserId => throw new NotSupportedException();

        public bool CanPostComment => false;

        public event EventHandler<IComment> RecieveComment;

        public void Dispose()
        {
            // do nothing.
        }

        public Task<IReadOnlyCollection<IComment>> GetInitialComments()
        {
            return Task.FromResult((IReadOnlyCollection<IComment>)_comments);
        }

        public Task<CommentPostResult> PostComment(string message, TimeSpan position, string commands)
        {
            throw new NotSupportedException();
        }
    }

}
