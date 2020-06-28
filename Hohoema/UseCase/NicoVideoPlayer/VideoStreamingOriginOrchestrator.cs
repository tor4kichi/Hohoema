using I18NPortable;
using Mntone.Nico2.Videos.Comment;
using Mntone.Nico2.Videos.Dmc;
using Hohoema.Models;
using Hohoema.Models.Cache;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Video;
using Hohoema.Repository.NicoVideo;
using Hohoema.Services;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Playback;

namespace Hohoema.UseCase.NicoVideoPlayer
{
    
    public sealed class VideoStreamingOriginOrchestrator : BindableBase
    {
        public class PlayingOrchestrateResult
        {
            internal PlayingOrchestrateResult()
            {
                IsSuccess = false;
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
        }



        public VideoStreamingOriginOrchestrator(
            NiconicoSession niconicoSession,
            VideoCacheManager videoCacheManager,
            NicoVideoSessionProvider nicoVideoSessionProvider,
            DialogService dialogService,
            CommentRepository commentRepository
            )
        {
            _niconicoSession = niconicoSession;
            _videoCacheManager = videoCacheManager;
            _nicoVideoSessionProvider = nicoVideoSessionProvider;
            _dialogService = dialogService;
            _commentRepository = commentRepository;
        }

        private readonly NiconicoSession _niconicoSession;
        private readonly VideoCacheManager _videoCacheManager;
        private readonly NicoVideoSessionProvider _nicoVideoSessionProvider;
        private readonly DialogService _dialogService;
        private readonly CommentRepository _commentRepository;





        /// <summary>
        /// 再生処理
        /// </summary>
        /// <returns></returns>
        public async Task<PlayingOrchestrateResult> CreatePlayingOrchestrateResultAsync(string videoId)
        {
            var cacheVideoResult = await _videoCacheManager.TryCreateCachedVideoSessionProvider(videoId);

            if (cacheVideoResult.IsSuccess)
            {
                INiconicoCommentSessionProvider commentSessionProvider = null;
                INicoVideoDetails nicoVideoDetails = null;
                if (!Models.Helpers.InternetConnection.IsInternet())
                {
                    var cachedComment = _commentRepository.GetCached(videoId);
                    if (cachedComment != null)
                    {
                        commentSessionProvider = new CachedCommentsProvider(videoId, cachedComment.Comments);
                    }

                    var videoInfo = Database.NicoVideoDb.Get(videoId);
                    if (videoInfo != null)
                    {
                        nicoVideoDetails = new CachedVideoDetails()
                        {
                            VideoTitle = videoInfo.Title,
                            ViewCount = videoInfo.ViewCount,
                            CommentCount = videoInfo.CommentCount,
                            MylistCount = videoInfo.MylistCount,
                            VideoLength = videoInfo.Length,
                            SubmitDate = videoInfo.PostedAt,
                            Tags = videoInfo.Tags.Select(x => new NicoVideoTag(x.Id)).ToArray(),
                            ProviderId = videoInfo.Owner?.OwnerId,
                            ProviderName = videoInfo.Owner?.ScreenName,
                            OwnerIconUrl = videoInfo.Owner.IconUrl,
                            DescriptionHtml = videoInfo.DescriptionWithHtml,
                            ThumbnailUrl = videoInfo.ThumbnailUrl
                        };
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
                    cacheVideoResult.VideoSessionProvider,
                    commentSessionProvider,
                    nicoVideoDetails
                    );
            }
            else
            {
                /*
                bool canPlay = true;
                var downloadLineOwnership = await _videoCacheManager.TryRentDownloadLineAsync();
                if (downloadLineOwnership == null)
                {
                    canPlay = false;
                    if (await ShowSuspendCacheDownloadingDialog())
                    {
                        await _videoCacheManager.SuspendCacheDownload();
                        canPlay = true;
                    }
                }

                if (!canPlay)
                {
                    return new PlayingOrchestrateResult();
                }
                */

                var preparePlayVideo = await _nicoVideoSessionProvider.PreparePlayVideoAsync(videoId);

                if (!preparePlayVideo.IsSuccess)
                {
                    var progress = await _videoCacheManager.GetDownloadProgressVideosAsync();
                    if (!_niconicoSession.IsPremiumAccount && progress.Any())
                    {
                        var result = await ShowSuspendCacheDownloadingDialog();
                        if (result)
                        {
                            preparePlayVideo = await _nicoVideoSessionProvider.PreparePlayVideoAsync(videoId);
                        }
                    }
                }

                if (preparePlayVideo == null || !preparePlayVideo.IsSuccess)
                {
                    throw new NotSupportedException("不明なエラーにより再生できません");
                }

                return new PlayingOrchestrateResult(
                    preparePlayVideo,
                    preparePlayVideo,
                    preparePlayVideo.GetVideoDetails()
                    );
            }
        }

        async Task<bool> ShowSuspendCacheDownloadingDialog()
        {
            var currentDownloadingItems = await _videoCacheManager.GetDownloadProgressVideosAsync();
            var downloadingItem = currentDownloadingItems.FirstOrDefault();
            var downloadingItemVideoInfo = Database.NicoVideoDb.Get(downloadingItem.VideoId);
/*
            var totalSize = downloadingItem.DownloadOperation.Progress.TotalBytesToReceive;
            var receivedSize = downloadingItem.DownloadOperation.Progress.BytesReceived;
            var megaBytes = (totalSize - receivedSize) / 1000_000.0;
            var downloadProgressDescription = $"ダウンロード中\n{downloadingItemVideoInfo.Title}\n残り {megaBytes:0.0} MB ( {receivedSize / 1000_000.0:0.0} MB / {totalSize / 1000_000.0:0.0} MB)";
            */
            var isCancelCacheAndPlay = await _dialogService.ShowMessageDialog(
                "CancelCacheAndPlayDesc".Translate(),
                "CancelCacheAndPlayTitle".Translate(),
                "CancelCacheAndPlay".Translate(),
                "Cancel".Translate()
                );
            return isCancelCacheAndPlay;
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

        public string OwnerIconUrl { get; set; }

        public bool IsChannelOwnedVideo { get; set; }

        public string DescriptionHtml { get; set; }

        public double LoudnessCorrectionValue { get; set; }

        public bool IsSeriesVideo => false;

        public Series Series => null;
    }


    public sealed class CachedCommentsProvider : INiconicoCommentSessionProvider
    {
        private readonly IReadOnlyCollection<Comment> _comments;

        public CachedCommentsProvider(string videoId, IReadOnlyCollection<Comment> comments)
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
        private readonly IReadOnlyCollection<Comment> _comments;

        public OfflineVideoCommentSession(string videoId, IReadOnlyCollection<Comment> comments)
        {
            ContentId = videoId;
            _comments = comments;
        }

        public string ContentId { get; }

        public string UserId => throw new NotSupportedException();

        public bool CanPostComment => false;

        public event EventHandler<Comment> RecieveComment;

        public void Dispose()
        {
            // do nothing.
        }

        public Task<IReadOnlyCollection<Comment>> GetInitialComments()
        {
            return Task.FromResult(_comments);
        }

        public Task<CommentPostResult> PostComment(string message, TimeSpan position, string commands)
        {
            throw new NotSupportedException();
        }
    }

}
