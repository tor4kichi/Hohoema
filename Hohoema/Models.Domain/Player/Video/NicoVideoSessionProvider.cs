using Mntone.Nico2.Videos.Dmc;
using Mntone.Nico2.Videos.WatchAPI;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Hohoema.Models.Domain.Niconico.Video;
using System.Collections.Immutable;
using Windows.Foundation;
using Hohoema.Models.Domain.Player.Video.Comment;
using Uno.Extensions;

namespace Hohoema.Models.Domain.Player.Video
{

    
    public interface INicoVideoDetails
    {
        string VideoTitle { get; }
        NicoVideoTag[] Tags { get; }

        string ThumbnailUrl { get; }
        TimeSpan VideoLength { get; }

        DateTime SubmitDate { get; }
        int ViewCount { get; }
        int CommentCount { get; }
        int MylistCount { get; }
        string ProviderId { get; }
        string ProviderName { get; }
        string OwnerIconUrl { get; }
        bool IsChannelOwnedVideo { get; }

        string DescriptionHtml { get; }

        double LoudnessCorrectionValue { get; }

        bool IsSeriesVideo { get; }
        Series Series { get; }

        bool IsLikedVideo { get; }
    }


    public class DmcVideoDetails : INicoVideoDetails
    {
        private readonly DmcWatchResponse _dmcWatchRes;

        internal DmcVideoDetails(DmcWatchData dmcWatchData)
        {
            _dmcWatchRes = dmcWatchData.DmcWatchResponse;
            Tags = _dmcWatchRes.Tag.Items.Select(x => new NicoVideoTag(x.Name)).ToArray();
        }

        public string VideoTitle => _dmcWatchRes.Video.Title;

        public NicoVideoTag[] Tags { get; }

        public string ThumbnailUrl => _dmcWatchRes.Video.Thumbnail.Url.OriginalString;

        public TimeSpan VideoLength => TimeSpan.FromSeconds(_dmcWatchRes.Video.Duration);

        public DateTime SubmitDate => _dmcWatchRes.Video.RegisteredAt.DateTime;

        public int ViewCount => _dmcWatchRes.Video.Count.View;

        public int CommentCount => _dmcWatchRes.Video.Count.Comment;

        public int MylistCount => _dmcWatchRes.Video.Count.Mylist;

        public string ProviderId => _dmcWatchRes.Owner?.Id.ToString() ?? _dmcWatchRes.Channel?.Id;
        public string ProviderName => _dmcWatchRes.Owner?.Nickname ?? _dmcWatchRes.Channel?.Name;

        public string OwnerIconUrl => _dmcWatchRes.Owner?.IconUrl.OriginalString ?? _dmcWatchRes.Channel?.Thumbnail.Url.OriginalString;

        public bool IsChannelOwnedVideo => _dmcWatchRes.Channel != null;

        public string DescriptionHtml => _dmcWatchRes.Video.Description;

        public double LoudnessCorrectionValue
        {
            get
            {
                try
                {
                    return _dmcWatchRes.Media.Delivery.Movie.Audios[0].Metadata.VideoLoudnessCollection;
                }
                catch { }

                return 1.0;
            }
        }

        public bool IsSeriesVideo => _dmcWatchRes?.Series != null;
        public Series Series => _dmcWatchRes?.Series;

        public bool IsLikedVideo => _dmcWatchRes.Video.Viewer?.Like.IsLiked ?? false;
    }

    public class WatchApiVideoDetails : INicoVideoDetails
    {
        private readonly WatchApiResponse _watchApiRes;

        public WatchApiVideoDetails(WatchApiResponse watchApiRes)
        {
            _watchApiRes = watchApiRes;
            Tags = _watchApiRes.videoDetail.tagList.Select(x => new NicoVideoTag(x.tag)).ToArray();
        }


        public string VideoTitle => _watchApiRes.videoDetail.title;

        public NicoVideoTag[] Tags { get; }

        public string ThumbnailUrl => _watchApiRes.videoDetail.thumbnail;

        public TimeSpan VideoLength =>  TimeSpan.FromSeconds(_watchApiRes.videoDetail.length.Value);

        public DateTime SubmitDate => DateTime.Parse(_watchApiRes.videoDetail.postedAt);

        public int ViewCount => _watchApiRes.videoDetail.viewCount.GetValueOrDefault();

        public int CommentCount => _watchApiRes.videoDetail.commentCount.GetValueOrDefault();

        public int MylistCount => _watchApiRes.videoDetail.mylistCount.GetValueOrDefault();

        public string ProviderId => _watchApiRes.UploaderInfo?.id ?? _watchApiRes.channelInfo?.id;

        public string ProviderName => _watchApiRes.UploaderInfo?.nickname ?? _watchApiRes.channelInfo?.name;

        public string OwnerIconUrl => _watchApiRes.UploaderInfo?.icon_url ?? _watchApiRes.channelInfo?.icon_url;

        public bool IsChannelOwnedVideo => _watchApiRes.channelInfo != null;

        public string DescriptionHtml => _watchApiRes.videoDetail.description;

        public double LoudnessCorrectionValue => 1.0;
        
        public bool IsSeriesVideo => false;

        public Series Series => null;

        public bool IsLikedVideo => false;
    }


    public enum PreparePlayVideoFailedReason
    {
        Deleted,
        VideoFormatNotSupported,
        NotPlayPermit_RequirePay,
        NotPlayPermit_RequireChannelMember,
        NotPlayPermit_RequirePremiumMember,
    }

    public class PreparePlayVideoResult : INiconicoVideoSessionProvider, INiconicoCommentSessionProvider
    {
        public Exception Exception { get; }
        public bool IsSuccess { get; }


        public string ContentId { get; private set; }
        
        public ImmutableArray<NicoVideoQualityEntity> AvailableQualities { get; }

        private readonly NicoVideoSessionOwnershipManager _ownershipManager;
        private readonly WatchApiResponse _watchApiResponse;
        private readonly DmcWatchData _dmcWatchData;

        private readonly NiconicoSession _niconicoSession;
        private readonly NicoVideoCacheRepository _nicoVideoRepository;

        PreparePlayVideoResult(string contentId, NiconicoSession niconicoSession, NicoVideoCacheRepository nicoVideoRepository)
        {
            ContentId = contentId;
            _niconicoSession = niconicoSession;
            _nicoVideoRepository = nicoVideoRepository;
        }

        public PreparePlayVideoResult(string contentId, NiconicoSession niconicoSession, NicoVideoCacheRepository nicoVideoRepository, Exception e)
            : this(contentId, niconicoSession, nicoVideoRepository)
        {
            Exception = e;
            AvailableQualities = ImmutableArray<NicoVideoQualityEntity>.Empty;
            IsSuccess = false;
        }

        public PreparePlayVideoResult(string contentId, NiconicoSession niconicoSession, NicoVideoCacheRepository nicoVideoRepository, PreparePlayVideoFailedReason failedReason)
            : this(contentId, niconicoSession, nicoVideoRepository)
        {
            AvailableQualities = ImmutableArray<NicoVideoQualityEntity>.Empty;
            IsSuccess = false;
            FailedReason = failedReason;
        }

        public PreparePlayVideoResult(string contentId, NiconicoSession niconicoSession, NicoVideoSessionOwnershipManager ownershipManager, NicoVideoCacheRepository nicoVideoRepository, WatchApiResponse watchApiResponse)
            : this(contentId, niconicoSession, nicoVideoRepository)
        {
            _ownershipManager = ownershipManager;
            _watchApiResponse = watchApiResponse;
            IsSuccess = _watchApiResponse != null;
            var quality = _watchApiResponse.VideoUrl.OriginalString.EndsWith("low") ? NicoVideoQuality.Smile_Low : NicoVideoQuality.Smile_Original;
            AvailableQualities = new[]
            {
                new NicoVideoQualityEntity(true, quality, quality.ToString())
            }
            .ToImmutableArray();

            // Note: スマイル鯖はいずれ無くなると見て対応を限定的にしてしまう
        }

        public PreparePlayVideoResult(string contentId, NiconicoSession niconicoSession, NicoVideoSessionOwnershipManager ownershipManager, NicoVideoCacheRepository nicoVideoRepository, DmcWatchData dmcWatchData)
            : this(contentId, niconicoSession, nicoVideoRepository)
        {
            _ownershipManager = ownershipManager;
            _dmcWatchData = dmcWatchData;
            IsSuccess = _dmcWatchData != null;
            if (_dmcWatchData?.DmcWatchResponse.Media.Delivery is not null and var delivery)
            {
                AvailableQualities = delivery.Movie.Videos
                    .Select(x => new NicoVideoQualityEntity(x.IsAvailable, QualityIdToNicoVideoQuality(x.Id), x.Id, x.Metadata.Bitrate, x.Metadata.Resolution.Width, x.Metadata.Resolution.Height))
                    .ToImmutableArray();
            }
            else //if (_dmcWatchData.DmcWatchResponse.Media.DeliveryLegacy != null)
            {
                throw new NotSupportedException("DmcWatchResponse.Media.DeliveryLegacy not supported");
                /*
                var video = _dmcWatchData.DmcWatchResponse.Video;
                var smileInfo = _dmcWatchData.DmcWatchResponse.Video.SmileInfo;
                var quality = smileInfo.Url.EndsWith("low") ? NicoVideoQuality.Smile_Low : NicoVideoQuality.Smile_Original;

                AvailableQualities = new[]
                {
                    new NicoVideoQualityEntity(false, NicoVideoQuality.Smile_Low, "", null, video.Width, video.Height),
                    new NicoVideoQualityEntity(false, NicoVideoQuality.Smile_Original, "", null, video.Width, video.Height),
                }
                .ToImmutableArray();
                */
            }
        }

        public INicoVideoDetails GetVideoDetails()
        {
            if (_dmcWatchData != null)
            {
                return new DmcVideoDetails(_dmcWatchData);
            }
            else if (_watchApiResponse != null)
            {
                return new WatchApiVideoDetails(_watchApiResponse);
            }
            else { throw new ArgumentNullException(); }
        }

        public bool IsForCacheDownload { get; set; }
        public PreparePlayVideoFailedReason? FailedReason { get; }

        public async Task<List<NicoVideo>> GetRelatedVideos()
        {
            if (_dmcWatchData?.DmcWatchResponse != null)
            {
                // TODO: 動画プレイリスト情報の取得をProvider.NicoVideoProviderへ移す
                var res = await _niconicoSession.Context.Video.GetVideoPlaylistAsync(_dmcWatchData.DmcWatchResponse.Video.Id, "");

                if (res.Status == "ok")
                {
                    return res.Data.Items
                        .Select(x =>
                        {
                            var videoData = _nicoVideoRepository.Get(x.Id);
                            videoData.Title = x.Title;
                            videoData.Length = TimeSpan.FromSeconds(x.LengthSeconds);
                            videoData.PostedAt = DateTime.Parse(x.FirstRetrieve);
                            videoData.ThumbnailUrl = x.ThumbnailURL;
                            videoData.ViewCount = x.ViewCounter;
                            videoData.MylistCount = x.MylistCounter;
                            videoData.CommentCount = x.NumRes ?? 0;

                            _nicoVideoRepository.AddOrUpdate(videoData);

                            return videoData;
                        })
                        .ToList();
                }
            }

            return new List<NicoVideo>();
        }


        public bool CanPlayQuality(string qualityId)
        {
            return true;
        }


        

        /// <summary>
        /// 動画ストリームの取得します
        /// </summary>
        /// <exception cref="NotSupportedException" />
        public async Task<IStreamingSession> CreateVideoSessionAsync(NicoVideoQuality quality = NicoVideoQuality.Unknown)
        {
            IStreamingSession streamingSession = null;
            if (_watchApiResponse != null)
            {
                var ownership = await _ownershipManager.TryRentVideoSessionOwnershipAsync(_watchApiResponse.videoDetail.id, !IsForCacheDownload);
                if (ownership != null)
                {
                    streamingSession = new SmileVideoStreamingSession(
                        _watchApiResponse.VideoUrl,
                        _niconicoSession,
                        ownership
                        );
                }
            }
            else if (_dmcWatchData != null)
            {
                if (_dmcWatchData.DmcWatchResponse.Media.Delivery is not null and var delivery)
                {
                    var qualityEntity = AvailableQualities.Where(x => x.IsAvailable).FirstOrDefault(x => x.Quality == quality);
                    if (qualityEntity == null)
                    {
                        qualityEntity = AvailableQualities.Where(x => x.IsAvailable).First();
                    }

                    var ownership = await _ownershipManager.TryRentVideoSessionOwnershipAsync(_dmcWatchData.DmcWatchResponse.Video.Id, !IsForCacheDownload);
                    if (ownership != null)
                    {
                        streamingSession = new DmcVideoStreamingSession(qualityEntity.QualityId, _dmcWatchData, _niconicoSession, ownership);
                    }
                    
                }
                else if (_dmcWatchData.DmcWatchResponse.Media.DeliveryLegacy != null)
                {
                    throw new NotSupportedException("DmcWatchResponse.Media.DeliveryLegacy is not supported");
                    /*
                    var ownership = await _ownershipManager.TryRentVideoSessionOwnershipAsync(_dmcWatchData.DmcWatchResponse.Video.Id, !IsForCacheDownload);
                    if (ownership != null)
                    {
                        streamingSession = new SmileVideoStreamingSession(
                            new Uri(_dmcWatchData.DmcWatchResponse.Video.SmileInfo.Url), _niconicoSession, ownership);
                    }
                    */
                }
                else
                {
                    throw new NotSupportedException("動画ページ情報から動画ファイルURLを検出できませんでした");
                }
            }
            else
            {
                throw new NotSupportedException("動画の再生準備に失敗（動画ページの解析でエラーが発生）");
            }

            return streamingSession;
        }






        public Task<ICommentSession> CreateCommentSessionAsync()
        {
            if (_dmcWatchData != null)
            {
                return CreateCommentSession(ContentId, _dmcWatchData);
            }
            else if (_watchApiResponse != null)
            {
                return CreateCommentSession(ContentId, _watchApiResponse);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        Task<ICommentSession> CreateCommentSession(string contentId, WatchApiResponse watchApiRes)
        {
            var commentClient = new CommentClient(_niconicoSession, contentId);
            commentClient.CommentServerInfo = new CommentServerInfo()
            {
                ServerUrl = watchApiRes.CommentServerUrl.OriginalString,
                VideoId = contentId,
                DefaultThreadId = (int)watchApiRes.ThreadId,
                CommunityThreadId = (int)watchApiRes.OptionalThreadId,
                ViewerUserId = watchApiRes.viewerInfo.id,
                ThreadKeyRequired = watchApiRes.IsKeyRequired
            };
            commentClient.VideoOwnerId = watchApiRes.UploaderInfo?.id;

            return Task.FromResult(new VideoCommentService(commentClient) as ICommentSession);
        }

        Task<ICommentSession> CreateCommentSession(string contentId, DmcWatchData watchData)
        {
            var commentClient = new CommentClient(_niconicoSession, contentId);
            var dmcRes = watchData.DmcWatchResponse;
            commentClient.CommentServerInfo = new CommentServerInfo()
            {
                ServerUrl = dmcRes.Comment.Threads[0].Server.OriginalString,
                VideoId = contentId,
                DefaultThreadId = dmcRes.Comment.Threads[0].Id,
                ViewerUserId = dmcRes.Viewer?.Id ?? 0,
                ThreadKeyRequired = dmcRes.Comment.Threads[0].IsThreadkeyRequired
            };

            // チャンネル動画ではOnwerはnullになる
            commentClient.VideoOwnerId = dmcRes.Owner?.Id.ToString();

            commentClient.DmcWatch = dmcRes;

            var communityThread = dmcRes.Comment.Threads.FirstOrDefault(x => x.Label == "community");
            if (communityThread != null)
            {
                commentClient.CommentServerInfo.CommunityThreadId = communityThread.Id;
            }

            return Task.FromResult(new VideoCommentService(commentClient) as ICommentSession);
        }


        public NicoVideoQuality QualityIdToNicoVideoQuality(string qualityId)
        {
            return _dmcWatchData?.ToNicoVideoQuality(qualityId) ?? NicoVideoQuality.Unknown;
        }
    }

    public sealed class SessionOwnershipRentFailedEventArgs
    {
        Deferral _deferral;
        public SessionOwnershipRentFailedEventArgs(DeferralCompletedHandler deferralCompleted)
        {
            _deferralCompleted = deferralCompleted;
        }

        internal bool IsUseDeferral => _deferral != null;
        private readonly DeferralCompletedHandler _deferralCompleted;

        public bool IsHandled { get; set; }

        public Deferral GetDeferral()
        {
            return _deferral ??= new Deferral(_deferralCompleted);
        }
    }


    public sealed class SessionOwnershipRemoveRequestedEventArgs
    {
        public SessionOwnershipRemoveRequestedEventArgs(string videoId)
        {
            VideoId = videoId;
        }

        public string VideoId { get; }
    }


    public class NicoVideoSessionOwnershipManager
    {
        public NicoVideoSessionOwnershipManager(NiconicoSession niconicoSession)
        {
            _niconicoSession = niconicoSession;
        }

        List<VideoSessionOwnership> _VideoSessions = new List<VideoSessionOwnership>();

        public event TypedEventHandler<NicoVideoSessionOwnershipManager, SessionOwnershipRentFailedEventArgs> RentFailed;
        public event TypedEventHandler<NicoVideoSessionOwnershipManager, SessionOwnershipRemoveRequestedEventArgs> OwnershipRemoveRequested;

        // ダウンロードライン数（再生中DLも含める）
        // 未登録ユーザー = 1
        // 通常会員       = 1
        // プレミアム会員 = 3
        public const int MaxDownloadLineCount = 1;
        public const int MaxDownloadLineCount_Premium = 3;
        private readonly NiconicoSession _niconicoSession;

        public int DownloadSessionCount => _VideoSessions.Count;

        public int AvairableDownloadLineCount => _niconicoSession.IsPremiumAccount 
            ? MaxDownloadLineCount_Premium - DownloadSessionCount
            : MaxDownloadLineCount - DownloadSessionCount 
            ;
        public bool CanAddDownloadLine()
        {
            return AvairableDownloadLineCount >= 1;
        }

        public class VideoSessionOwnership : IDisposable
        {
            private readonly NicoVideoSessionOwnershipManager _ownershipManager;

            bool _isDisposed;
            internal VideoSessionOwnership(string videoId, NicoVideoSessionOwnershipManager ownershipManager)
            {
                VideoId = videoId;
                _ownershipManager = ownershipManager;
            }

            public string VideoId { get; }

            public void Dispose()
            {
                if (_isDisposed) { return; }

                _isDisposed = true;
                _ownershipManager.RemoveVideoSessionOwnership(this);
            }

            public event EventHandler ReturnOwnershipRequested;

            internal void TriggerStopOwnership()
            {
                ReturnOwnershipRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        public async Task<VideoSessionOwnership> TryRentVideoSessionOwnershipAsync(string videoId, bool isPriorityRent)
        {
            if (CanAddDownloadLine())
            {
                var ownership = new VideoSessionOwnership(videoId, this);
                _VideoSessions.Add(ownership);
                return ownership;
            }

            var handlers = RentFailed;
            if (handlers != null)
            {
                TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
                var args = new SessionOwnershipRentFailedEventArgs(() => taskCompletionSource.SetResult(true));
                handlers.Invoke(this, args);

                await Task.Delay(10);
                if (args.IsUseDeferral)
                {
                    await taskCompletionSource.Task;
                }

                if (!args.IsHandled) 
                {
                    return null;
                }

                if (isPriorityRent)
                {
                    var session = _VideoSessions.First();
                    session.TriggerStopOwnership();
                    (session as IDisposable).Dispose();

                    await Task.Delay(10);
                }

                if (CanAddDownloadLine())
                {
                    var ownership = new VideoSessionOwnership(videoId, this);
                    _VideoSessions.Add(ownership);
                    return ownership;
                }
            }
            else
            {
                if (isPriorityRent)
                {
                    var session = _VideoSessions.First();
                    session.TriggerStopOwnership();
                    (session as IDisposable).Dispose();

                    await Task.Delay(10);
                }

                if (CanAddDownloadLine())
                {
                    var ownership = new VideoSessionOwnership(videoId, this);
                    _VideoSessions.Add(ownership);
                    return ownership;
                }
            }

            return null;
        }

        private void RemoveVideoSessionOwnership(VideoSessionOwnership ownership)
        {
            _VideoSessions.Remove(ownership);

            OwnershipRemoveRequested?.Invoke(this, new SessionOwnershipRemoveRequestedEventArgs(ownership.VideoId));
        }
    }


    public class NicoVideoSessionProvider
	{
        public NicoVideoSessionProvider(
            NicoVideoProvider nicoVideoProvider, 
            NicoVideoCacheRepository nicoVideoRepository,
            NiconicoSession niconicoSession,
            NicoVideoSessionOwnershipManager nicoVideoSessionOwnershipManager
            )
		{
            _nicoVideoProvider = nicoVideoProvider;
            _nicoVideoRepository = nicoVideoRepository;
            _niconicoSession = niconicoSession;
            _nicoVideoSessionOwnershipManager = nicoVideoSessionOwnershipManager;
        }

        readonly private NicoVideoProvider _nicoVideoProvider;
        private readonly NicoVideoCacheRepository _nicoVideoRepository;
        readonly private NiconicoSession _niconicoSession;
        private readonly NicoVideoSessionOwnershipManager _nicoVideoSessionOwnershipManager;

        public async Task<PreparePlayVideoResult> PreparePlayVideoAsync(string rawVideoId, bool isForCacheDownload = false)
        {
            if (!Helpers.InternetConnection.IsInternet()) { return null; }

            try
            {
                var dmcRes = await _nicoVideoProvider.GetDmcWatchResponse(rawVideoId);
                if (dmcRes is null)
                {
                    throw new NotSupportedException("視聴不可：視聴ページの取得または解析に失敗");
                }
                else if (dmcRes.DmcWatchResponse.Video.IsDeleted)
                {
                    return new PreparePlayVideoResult(rawVideoId, _niconicoSession, _nicoVideoRepository, PreparePlayVideoFailedReason.Deleted);
                }
                else if (dmcRes.DmcWatchResponse.Media.Delivery == null)
                {
                    var preview = dmcRes?.DmcWatchResponse.Payment.Preview;
                    if (preview.Premium.IsEnabled)
                    {
                        return new PreparePlayVideoResult(rawVideoId, _niconicoSession, _nicoVideoRepository, PreparePlayVideoFailedReason.NotPlayPermit_RequirePremiumMember);

                    }
                    else if (preview.Ppv.IsEnabled)
                    {
                        return new PreparePlayVideoResult(rawVideoId, _niconicoSession, _nicoVideoRepository, PreparePlayVideoFailedReason.NotPlayPermit_RequirePay);
                    }
                    else if (preview.Admission.IsEnabled)
                    {
                        return new PreparePlayVideoResult(rawVideoId, _niconicoSession, _nicoVideoRepository, PreparePlayVideoFailedReason.NotPlayPermit_RequireChannelMember);
                    }
                    else
                    {
                        throw new NotSupportedException("視聴不可：不明な理由で視聴不可");
                    }
                }


                return new PreparePlayVideoResult(rawVideoId, _niconicoSession, _nicoVideoSessionOwnershipManager, _nicoVideoRepository, dmcRes)
                {
                    IsForCacheDownload = isForCacheDownload
                };
            }
            catch (Exception e)
            {
                return new PreparePlayVideoResult(rawVideoId, _niconicoSession, _nicoVideoRepository, e);
            }

            /*
            try
            {
                var watchApiRes = await _nicoVideoProvider.GetWatchApiResponse(rawVideoId);
                if (watchApiRes.IsDeleted)
                {
                    throw new NotSupportedException("動画は削除されています");
                }
                if (watchApiRes.IsKeyRequired)
                {
                    throw new NotSupportedException("再生には視聴権が必要です。");
                }
                if (watchApiRes.flashvars.movie_type == "swf")
                {
                    throw new NotSupportedException("SWF形式の動画はサポートしていません");
                }

                if (watchApiRes.VideoUrl.OriginalString.StartsWith("rtmp"))
                {
                    throw new NotSupportedException("RTMP形式の動画はサポートしていません");
                }

                return new PreparePlayVideoResult(rawVideoId, _niconicoSession,  _nicoVideoSessionOwnershipManager, _nicoVideoRepository, watchApiRes)
                {
                    IsForCacheDownload = isForCacheDownload
                };
            }
            catch (Exception e)
            {
                return new PreparePlayVideoResult(rawVideoId, _niconicoSession, _nicoVideoRepository, e);
            }
            */
        }



        #region Playback


        

        #endregion


        static readonly Regex NiconicoContentUrlRegex = new Regex(@"https?:\/\/[a-z]+\.nicovideo\.jp\/([a-z]+)\/([a-z][a-z][0-9]+|[0-9]+)");

        static readonly Regex GeneralUrlRegex = new Regex(@"https?:\/\/([a-zA-Z0-9.\/?=_-]*)");

        static public VideoRelatedInfomation GetVideoRelatedInfomationWithVideoDescription(string rawVideoId, string descriptionHtml)
        {
            if (string.IsNullOrEmpty(descriptionHtml)) { return null; }

            VideoRelatedInfomation info = new VideoRelatedInfomation();
            var niconicoContentMatchs = NiconicoContentUrlRegex.Matches(descriptionHtml);
            foreach (var match in niconicoContentMatchs.Cast<Match>())
            {
                var contentType = match.Groups[1].Value;
                var contentId = match.Groups[2].Value;

                // TODO: 
                info.NiconicoContentIds.Add(new NiconicoContent()
                {
                    Type = contentType,
                    Id = contentId
                });
            }

            return info;
        }

        static public IList<Uri> GetGeneralUrlsWithVideoDescription(string rawVideoId, string descriptionHtml)
        {
            if (string.IsNullOrEmpty(descriptionHtml)) { return null; }

            List<Uri> uris = new List<Uri>();
            var generalUrlMatchs = GeneralUrlRegex.Matches(descriptionHtml);

            foreach (var match in generalUrlMatchs.Cast<Match>().Where(x => !NiconicoContentUrlRegex.IsMatch(x.Value)))
            {
                var url = match.Groups[1].Value;
                uris.Add(new Uri(url));
            }

            return uris;
        }


    }


    // 動画情報
    public class VideoRelatedInfomation
    {
        public IList<NiconicoContent> NiconicoContentIds { get; } = new List<NiconicoContent>();

        public IEnumerable<string> GetVideoIds()
        {
            return NiconicoContentIds.Where(x => x.Type == "watch" &&
                (x.Id.StartsWith("sm") || x.Id.StartsWith("so") || x.Id.StartsWith("nm"))
                )
                .Select(x => x.Id);
        }

        public IEnumerable<string> GetMylistIds()
        {
            return NiconicoContentIds.Where(x => x.Type == "mylist")
                .Select(x => x.Id);
        }
    }

    public class NiconicoContent
    {
        public string Type { get; set; }
        public string Id { get; set; }
    }




    public static class DmcWatchSessionExtension
    {
        public static NicoVideoQuality ToNicoVideoQuality(this DmcWatchData dmcWatchData, string qualityId)
        {
            var dmcVideoContent = dmcWatchData?.DmcWatchResponse.Media.Delivery.Movie.Videos.FirstOrDefault(x => x.Id == qualityId);
            if (dmcVideoContent != null)
            {
                var qualities = dmcWatchData.DmcWatchResponse.Media.Delivery.Movie.Videos;

                var index = qualities.IndexOf(dmcVideoContent);

                // DmcInfo.Quality の要素数は動画によって1～5個まで様々である
                // また並びは常に先頭が最高画質、最後尾は最低画質（Mobile）となっている
                // Mobileは常に生成される
                // なのでDmcInfo.Quality[0] は動画ごとによって Dmc_SuperHigh だったり Dmc_Midium であったりまちまち
                // この差を吸収するため、
                // indexを Dmc_Mobile(6)~Dmc_SuperHigh(2) の空間に変換する
                // (qualities.Count - index - 1) によってDmc_Mobileの場合が 0 になる
                var nicoVideoQualityIndex = (int)NicoVideoQuality.Dmc_Mobile - (qualities.Length - index - 1);
                var quality = (NicoVideoQuality)nicoVideoQualityIndex;
                if (!quality.IsDmc())
                {
                    throw new NotSupportedException(qualityId);
                }

                return quality;
            }
            else
            {
                if (Enum.TryParse<NicoVideoQuality>(qualityId, out var smileQuality)
                    && smileQuality.IsLegacy()
                    )
                {
                    return smileQuality;
                }
                else
                {
                    throw new NotSupportedException(qualityId);
                }
            }
        }
    }
}
