using I18NPortable;
using Mntone.Nico2.Videos.Comment;
using Mntone.Nico2.Videos.Dmc;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Cache;
using NicoPlayerHohoema.Models.Niconico;
using NicoPlayerHohoema.Models.Niconico.Video;
using NicoPlayerHohoema.Repository.NicoVideo;
using NicoPlayerHohoema.Services;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Playback;

namespace NicoPlayerHohoema.UseCase.NicoVideoPlayer
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
                bool canPlay = true;
                if (await IsRequireCacheDLSuspendToPlayVideoOnline(videoId))
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

                var preparePlayVideo = await _nicoVideoSessionProvider.PreparePlayVideoAsync(videoId);

                if (!preparePlayVideo.IsSuccess)
                {
                    throw new NotSupportedException("不明なエラーにより再生できません");
                }

                return new PlayingOrchestrateResult(
                    preparePlayVideo,
                    preparePlayVideo,
                    preparePlayVideo.GetVideoDetails()
                    );
            }

            /*
        try
        {


            await _session.StartPlayback(_mediaPlayer);

            _mediaPlayer.PlaybackSession.Position = initialPosition;

            quality = _session.Quality;

            if (_session is DmcVideoStreamingSession dmcSession)
            {
                var content = dmcSession.VideoContent;
                if (content != null)
                {
                    isDmcContent = true;
                }

                List<VideoContent> qualities = new List<VideoContent>();
                if (dmcSession.DmcWatchResponse?.Video?.DmcInfo?.Quality != null)
                {
                    qualities = dmcSession.DmcWatchResponse.Video.DmcInfo.Quality.Videos.ToList() ?? new List<VideoContent>();
                }
            }


            isSuccess = true;

            return new TryContentPlayingResult()
            {
                IsDmcContent = isDmcContent,

            };
            
        }
        */
        }



        async Task<bool> IsRequireCacheDLSuspendToPlayVideoOnline(string videoId)
        {
            if (!_niconicoSession.IsPremiumAccount && !_videoCacheManager.CanAddDownloadLine)
            {
                // 一般ユーザーまたは未登録ユーザーの場合
                // 視聴セッションを１つに制限するため、キャッシュダウンロードを止める必要がある
                // キャッシュ済みのアイテムを再生中の場合はダウンロード可能なので確認をスキップする
                var cachedItems = await _videoCacheManager.GetCacheRequest(videoId);
                if (cachedItems.FirstOrDefault(x => x.ToCacheState() == NicoVideoCacheState.Cached) != null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else if (!_niconicoSession.IsPremiumAccount)
            {
                // キャッシュ済みの場合はサスペンドを掛けない
                if (false == await _videoCacheManager.CheckCachedAsync(videoId))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                // プレミアムアカウントであれば常にキャッシュDLを止める必要はない
                return false;
            }
        }

        async Task<bool> ShowSuspendCacheDownloadingDialog()
        {
            var currentDownloadingItems = await _videoCacheManager.GetDownloadProgressVideosAsync();
            var downloadingItem = currentDownloadingItems.FirstOrDefault();
            var downloadingItemVideoInfo = Database.NicoVideoDb.Get(downloadingItem.RawVideoId);
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
            foreach (var comment in _comments)
            {
                if (!string.IsNullOrEmpty(comment.Mail))
                {
                    var commandActions = MailToCommandHelper.MakeCommandActions(comment.Mail.Split(' '));
                    foreach (var action in commandActions)
                    {
                        action(comment);
                    }
                }
            }

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
