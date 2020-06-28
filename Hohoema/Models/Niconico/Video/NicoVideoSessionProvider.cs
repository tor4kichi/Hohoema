using Mntone.Nico2;
using Mntone.Nico2.Videos.Dmc;
using Mntone.Nico2.Videos.WatchAPI;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Hohoema.Models.Cache;
using Hohoema.Services.Helpers;
using Hohoema.Models.Niconico.Video;
using Mntone.Nico2.Videos.Comment;
using Hohoema.Models.Niconico;
using Unity;
using Hohoema.Models.Provider;
using Prism.Unity;
using System.Collections.Immutable;
using Windows.Foundation;

namespace Hohoema.Models
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
    }


    public class DmcVideoDetails : INicoVideoDetails
    {
        private readonly DmcWatchResponse _dmcWatchRes;

        internal DmcVideoDetails(DmcWatchData dmcWatchData)
        {
            _dmcWatchRes = dmcWatchData.DmcWatchResponse;
            Tags = _dmcWatchRes.Tags.Select(x => new NicoVideoTag(x.Name)).ToArray();
        }

        public string VideoTitle => _dmcWatchRes.Video.Title;

        public NicoVideoTag[] Tags { get; }

        public string ThumbnailUrl => _dmcWatchRes.Video.ThumbnailURL;

        public TimeSpan VideoLength => TimeSpan.FromSeconds(_dmcWatchRes.Video.Duration);

        public DateTime SubmitDate => DateTime.Parse(_dmcWatchRes.Video.PostedDateTime);

        public int ViewCount => _dmcWatchRes.Video.ViewCount;

        public int CommentCount => _dmcWatchRes.Thread.CommentCount;

        public int MylistCount => _dmcWatchRes.Video.MylistCount;

        public string ProviderId => _dmcWatchRes.Owner?.Id ?? _dmcWatchRes.Channel?.GlobalId;
        public string ProviderName => _dmcWatchRes.Owner?.Nickname ?? _dmcWatchRes.Channel?.Name;

        public string OwnerIconUrl => _dmcWatchRes.Owner?.IconURL ?? _dmcWatchRes.Channel?.IconURL;

        public bool IsChannelOwnedVideo => _dmcWatchRes.Channel != null;

        public string DescriptionHtml => _dmcWatchRes.Video.Description;

        public double LoudnessCorrectionValue
        {
            get
            {
                var audio = _dmcWatchRes.Video.DmcInfo?.Quality.Audios.FirstOrDefault()?.LoudnessCorrectionValue.FirstOrDefault();
                if (audio != null)
                {
                    return audio.Value;
                }

                if (_dmcWatchRes.Video.SmileInfo != null)
                {
                    return _dmcWatchRes.Video.SmileInfo.LoudnessCorrectionValue?.First(x => x.Type == "video").Value ?? 1.0;
                }

                return 1.0;
            }
        }

        public bool IsSeriesVideo => _dmcWatchRes?.Series != null;
        public Series Series => _dmcWatchRes?.Series;
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

        PreparePlayVideoResult(string contentId, NiconicoSession niconicoSession)
        {
            ContentId = contentId;
            _niconicoSession = niconicoSession;
        }

        public PreparePlayVideoResult(string contentId, NiconicoSession niconicoSession, Exception e)
            : this(contentId, niconicoSession)
        {
            Exception = e;
            AvailableQualities = ImmutableArray<NicoVideoQualityEntity>.Empty;
            IsSuccess = false;
        }

        public PreparePlayVideoResult(string contentId, NiconicoSession niconicoSession, NicoVideoSessionOwnershipManager ownershipManager, WatchApiResponse watchApiResponse)
            : this(contentId, niconicoSession)
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

        public PreparePlayVideoResult(string contentId, NiconicoSession niconicoSession, NicoVideoSessionOwnershipManager ownershipManager, DmcWatchData dmcWatchData)
            : this(contentId, niconicoSession)
        {
            _ownershipManager = ownershipManager;
            _dmcWatchData = dmcWatchData;
            IsSuccess = _dmcWatchData != null;
            if (_dmcWatchData?.DmcWatchResponse.Video.DmcInfo != null)
            {
                AvailableQualities = _dmcWatchData.DmcWatchResponse.Video.DmcInfo.Quality.Videos
                    .Select(x => new NicoVideoQualityEntity(x.Available, QualityIdToNicoVideoQuality(x.Id), x.Id, x.Bitrate, x.Resolution?.Width, x.Resolution?.Height))
                    .ToImmutableArray();
            }
            else if (_dmcWatchData.DmcWatchResponse.Video.SmileInfo != null)
            {
                var video = _dmcWatchData.DmcWatchResponse.Video;
                var smileInfo = _dmcWatchData.DmcWatchResponse.Video.SmileInfo;
                var quality = smileInfo.Url.EndsWith("low") ? NicoVideoQuality.Smile_Low : NicoVideoQuality.Smile_Original;

                AvailableQualities = new[]
                {
                    new NicoVideoQualityEntity(false, NicoVideoQuality.Smile_Low, "", null, video.Width, video.Height),
                    new NicoVideoQualityEntity(false, NicoVideoQuality.Smile_Original, "", null, video.Width, video.Height),
                }
                .ToImmutableArray();
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


        public async Task<List<Database.NicoVideo>> GetRelatedVideos()
        {
            if (_dmcWatchData?.DmcWatchResponse.Playlist != null)
            {
                // TODO: 動画プレイリスト情報の取得をProvider.NicoVideoProviderへ移す
                var res = await _niconicoSession.Context.Video.GetVideoPlaylistAsync(_dmcWatchData.DmcWatchResponse.Video.Id, _dmcWatchData?.DmcWatchResponse.Playlist.Referer);

                if (res.Status == "ok")
                {
                    return res.Data.Items
                        .Select(x =>
                        {
                            var videoData = Database.NicoVideoDb.Get(x.Id);
                            videoData.Title = x.Title;
                            videoData.Length = TimeSpan.FromSeconds(x.LengthSeconds);
                            videoData.PostedAt = DateTime.Parse(x.FirstRetrieve);
                            videoData.ThumbnailUrl = x.ThumbnailURL;
                            videoData.ViewCount = x.ViewCounter;
                            videoData.MylistCount = x.MylistCounter;
                            videoData.CommentCount = x.NumRes ?? 0;

                            Database.NicoVideoDb.AddOrUpdate(videoData);

                            return videoData;
                        })
                        .ToList();
                }
            }

            return new List<Database.NicoVideo>();
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
                if (_dmcWatchData.DmcWatchResponse.Video.DmcInfo != null)
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
                else if (_dmcWatchData.DmcWatchResponse.Video.SmileInfo != null)
                {
                    var ownership = await _ownershipManager.TryRentVideoSessionOwnershipAsync(_dmcWatchData.DmcWatchResponse.Video.Id, !IsForCacheDownload);
                    if (ownership != null)
                    {
                        streamingSession = new SmileVideoStreamingSession(
                            new Uri(_dmcWatchData.DmcWatchResponse.Video.SmileInfo.Url), _niconicoSession, ownership);
                    }
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
                ServerUrl = dmcRes.Thread.ServerUrl,
                VideoId = contentId,
                DefaultThreadId = int.Parse(dmcRes.Thread.Ids.Default),
                ViewerUserId = dmcRes.Viewer.Id,
                ThreadKeyRequired = dmcRes.Video.IsOfficial
            };

            // チャンネル動画ではOnwerはnullになる
            commentClient.VideoOwnerId = dmcRes.Owner?.Id;

            commentClient.DmcWatch = dmcRes;

            if (int.TryParse(dmcRes.Thread.Ids.Community, out var communityThreadId))
            {
                commentClient.CommentServerInfo.CommunityThreadId = communityThreadId;
                Debug.WriteLine("dmcRes.Video.DmcInfo.Thread.PostkeyAvailable: " + dmcRes.Video.DmcInfo?.Thread?.PostkeyAvailable);
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
            NiconicoSession niconicoSession,
            NicoVideoSessionOwnershipManager nicoVideoSessionOwnershipManager
            )
		{
            _nicoVideoProvider = nicoVideoProvider;
            _niconicoSession = niconicoSession;
            _nicoVideoSessionOwnershipManager = nicoVideoSessionOwnershipManager;
        }

        readonly private Provider.NicoVideoProvider _nicoVideoProvider;
        readonly private NiconicoSession _niconicoSession;
        private readonly NicoVideoSessionOwnershipManager _nicoVideoSessionOwnershipManager;

        public async Task<PreparePlayVideoResult> PreparePlayVideoAsync(string rawVideoId, bool isForCacheDownload = false)
        {
            if (!Helpers.InternetConnection.IsInternet()) { return null; }

            var dmcRes = await _nicoVideoProvider.GetDmcWatchResponse(rawVideoId);
            if (dmcRes?.DmcWatchResponse.Video.IsDeleted ?? false)
            {
                throw new NotSupportedException("動画は削除されています");
            }
            if (dmcRes?.DmcWatchResponse.Video.DmcInfo != null)
            {
                if (dmcRes.DmcWatchResponse.Video?.DmcInfo?.Quality == null)
                {
                    throw new NotSupportedException("動画の視聴権がありません");
                }
            }
            
            if (dmcRes != null)
            {
                return new PreparePlayVideoResult(rawVideoId, _niconicoSession, _nicoVideoSessionOwnershipManager, dmcRes)
                {
                    IsForCacheDownload = isForCacheDownload
                };
            }

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

                return new PreparePlayVideoResult(rawVideoId, _niconicoSession, _nicoVideoSessionOwnershipManager, watchApiRes)
                {
                    IsForCacheDownload = isForCacheDownload
                };
            }
            catch (Exception e)
            {
                return new PreparePlayVideoResult(rawVideoId, _niconicoSession, e);
            }
        }



        #region Playback


        

        #endregion


        static readonly Regex NiconicoContentUrlRegex = new Regex(@"https?:\/\/[a-z]+\.nicovideo\.jp\/([a-z]+)\/([a-z][a-z][0-9]+|[0-9]+)");

        static readonly Regex GeneralUrlRegex = new Regex(@"https?:\/\/([a-zA-Z0-9.\/?=_-]*)");

        public static VideoRelatedInfomation GetVideoRelatedInfomationWithVideoDescription(string rawVideoId)
        {
            var nicoVideo = Database.NicoVideoDb.Get(rawVideoId);

            if (string.IsNullOrEmpty(nicoVideo.DescriptionWithHtml)) { return null; }

            VideoRelatedInfomation info = new VideoRelatedInfomation();
            var niconicoContentMatchs = NiconicoContentUrlRegex.Matches(nicoVideo.DescriptionWithHtml);
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

        public static IList<Uri> GetGeneralUrlsWithVideoDescription(string rawVideoId)
        {
            var nicoVideo = Database.NicoVideoDb.Get(rawVideoId);

            if (string.IsNullOrEmpty(nicoVideo.DescriptionWithHtml)) { return null; }

            List<Uri> uris = new List<Uri>();
            var generalUrlMatchs = GeneralUrlRegex.Matches(nicoVideo.DescriptionWithHtml);

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
            var dmcVideoContent = dmcWatchData?.DmcWatchResponse.Video.DmcInfo.Quality.Videos.FirstOrDefault(x => x.Id == qualityId);
            if (dmcVideoContent != null)
            {
                var qualities = dmcWatchData.DmcWatchResponse.Video.DmcInfo.Quality.Videos;

                var index = qualities.IndexOf(dmcVideoContent);

                // DmcInfo.Quality の要素数は動画によって1～5個まで様々である
                // また並びは常に先頭が最高画質、最後尾は最低画質（Mobile）となっている
                // Mobileは常に生成される
                // なのでDmcInfo.Quality[0] は動画ごとによって Dmc_SuperHigh だったり Dmc_Midium であったりまちまち
                // この差を吸収するため、
                // indexを Dmc_Mobile(6)~Dmc_SuperHigh(2) の空間に変換する
                // (qualities.Count - index - 1) によってDmc_Mobileの場合が 0 になる
                var nicoVideoQualityIndex = (int)NicoVideoQuality.Dmc_Mobile - (qualities.Count - index - 1);
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
