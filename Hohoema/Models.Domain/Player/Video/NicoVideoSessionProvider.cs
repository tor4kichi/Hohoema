using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Player.Video.Comment;
using NiconicoToolkit.Video.Watch;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Uno.Extensions;
using Windows.Foundation;

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
        private readonly DmcWatchApiData _dmcWatchRes;

        internal DmcVideoDetails(DmcWatchApiData dmcWatchData)
        {
            _dmcWatchRes = dmcWatchData;
            Tags = _dmcWatchRes.Tag.Items.Select(x => new NicoVideoTag(x.Name)).ToArray();
        }

        public string VideoTitle => _dmcWatchRes.Video.Title;

        public NicoVideoTag[] Tags { get; }

        public string ThumbnailUrl => _dmcWatchRes.Video.Thumbnail.LargeUrl.OriginalString;

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
                    return _dmcWatchRes.Media.Delivery.Movie.Audios[0].Metadata.LoudnessCollection[0].Value;
                }
                catch { }

                return 1.0;
            }
        }

       
        public bool IsSeriesVideo => _dmcWatchRes?.Series != null;
        public Series Series => _dmcWatchRes?.Series;

        public bool IsLikedVideo => _dmcWatchRes.Video.Viewer?.Like.IsLiked ?? false;
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
        private readonly DmcWatchApiData _dmcWatchData;

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

        public PreparePlayVideoResult(string contentId, NiconicoSession niconicoSession, PreparePlayVideoFailedReason failedReason)
            : this(contentId, niconicoSession)
        {
            AvailableQualities = ImmutableArray<NicoVideoQualityEntity>.Empty;
            IsSuccess = false;
            FailedReason = failedReason;
        }

        public PreparePlayVideoResult(string contentId, NiconicoSession niconicoSession, NicoVideoSessionOwnershipManager ownershipManager, DmcWatchApiData dmcWatchData)
            : this(contentId, niconicoSession)
        {
            _ownershipManager = ownershipManager;
            _dmcWatchData = dmcWatchData;
            IsSuccess = _dmcWatchData != null;
            if (_dmcWatchData?.Media.Delivery is not null and var delivery)
            {
                AvailableQualities = delivery.Movie.Videos
                    .Select(x => new NicoVideoQualityEntity(x.IsAvailable, QualityIdToNicoVideoQuality(x.Id), x.Id, (int)x.Metadata.Bitrate, (int)x.Metadata.Resolution.Width, (int)x.Metadata.Resolution.Height))
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
            else { throw new ArgumentNullException(); }
        }

        public bool IsForCacheDownload { get; set; }
        public PreparePlayVideoFailedReason? FailedReason { get; }


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
            if (_dmcWatchData != null)
            {
                if (_dmcWatchData.Media.Delivery is not null and var delivery)
                {
                    var qualityEntity = AvailableQualities.Where(x => x.IsAvailable).FirstOrDefault(x => x.Quality == quality);
                    if (qualityEntity == null)
                    {
                        qualityEntity = AvailableQualities.Where(x => x.IsAvailable).First();
                    }

                    var ownership = await _ownershipManager.TryRentVideoSessionOwnershipAsync(_dmcWatchData.Video.Id, !IsForCacheDownload);
                    if (ownership != null)
                    {
                        streamingSession = new DmcVideoStreamingSession(qualityEntity.QualityId, _dmcWatchData, _niconicoSession, ownership);
                    }
                    
                }
                else if (_dmcWatchData.Media.DeliveryLegacy != null)
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
            else
            {
                throw new NotSupportedException();
            }
        }

        Task<ICommentSession> CreateCommentSession(string contentId, DmcWatchApiData watchData)
        {
            var commentClient = new CommentClient(_niconicoSession, contentId);
            var dmcRes = watchData;
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

            return Task.FromResult(new VideoCommentService(commentClient, _niconicoSession.UserIdString) as ICommentSession);
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
        public event TypedEventHandler<NicoVideoSessionOwnershipManager, SessionOwnershipRemoveRequestedEventArgs> AvairableOwnership;

        // ダウンロードライン数（再生中DLも含める）
        // 未登録ユーザー = 1
        // 通常会員       = 1
        // プレミアム会員 = 1
        public const int MaxDownloadLineCount = 1;
        public const int MaxDownloadLineCount_Premium = 1;
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
                _ownershipManager.Return(this);
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

        private void Return(VideoSessionOwnership ownership)
        {
            _VideoSessions.Remove(ownership);

            AvairableOwnership?.Invoke(this, new SessionOwnershipRemoveRequestedEventArgs(ownership.VideoId));
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

        readonly private NicoVideoProvider _nicoVideoProvider;
        readonly private NiconicoSession _niconicoSession;
        private readonly NicoVideoSessionOwnershipManager _nicoVideoSessionOwnershipManager;

        public async Task<PreparePlayVideoResult> PreparePlayVideoAsync(string rawVideoId, bool noHistory = false)
        {
            if (!Helpers.InternetConnection.IsInternet()) { return null; }

            try
            {
                var dmcRes = await _nicoVideoProvider.GetWatchPageResponseAsync(rawVideoId, noHistory);
                if (dmcRes.WatchApiResponse is null)
                {
                    throw new NotSupportedException("視聴不可：視聴ページの取得または解析に失敗");
                }
                else if (dmcRes.WatchApiResponse.WatchApiData.Video.IsDeleted)
                {
                    return new PreparePlayVideoResult(rawVideoId, _niconicoSession, PreparePlayVideoFailedReason.Deleted);
                }
                else if (dmcRes.WatchApiResponse.WatchApiData.Media.Delivery == null)
                {
                    var preview = dmcRes.WatchApiResponse.WatchApiData.Payment.Preview;
                    if (preview.Premium.IsEnabled)
                    {
                        return new PreparePlayVideoResult(rawVideoId, _niconicoSession, PreparePlayVideoFailedReason.NotPlayPermit_RequirePremiumMember);

                    }
                    else if (preview.Ppv.IsEnabled)
                    {
                        return new PreparePlayVideoResult(rawVideoId, _niconicoSession, PreparePlayVideoFailedReason.NotPlayPermit_RequirePay);
                    }
                    else if (preview.Admission.IsEnabled)
                    {
                        return new PreparePlayVideoResult(rawVideoId, _niconicoSession, PreparePlayVideoFailedReason.NotPlayPermit_RequireChannelMember);
                    }
                    else
                    {
                        throw new NotSupportedException("視聴不可：不明な理由で視聴不可");
                    }
                }


                return new PreparePlayVideoResult(rawVideoId, _niconicoSession, _nicoVideoSessionOwnershipManager, dmcRes.WatchApiResponse.WatchApiData)
                {
                    IsForCacheDownload = noHistory
                };
            }
            catch (Exception e)
            {
                return new PreparePlayVideoResult(rawVideoId, _niconicoSession, e);
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

                return new PreparePlayVideoResult(rawVideoId, _niconicoSession,  _nicoVideoSessionOwnershipManager, watchApiRes)
                {
                    IsForCacheDownload = isForCacheDownload
                };
            }
            catch (Exception e)
            {
                return new PreparePlayVideoResult(rawVideoId, _niconicoSession, e);
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
        public static NicoVideoQuality ToNicoVideoQuality(this DmcWatchApiData dmcWatchData, string qualityId)
        {
            var dmcVideoContent = dmcWatchData?.Media.Delivery.Movie.Videos.FirstOrDefault(x => x.Id == qualityId);
            if (dmcVideoContent != null)
            {
                var qualities = dmcWatchData.Media.Delivery.Movie.Videos;

                var index = qualities.IndexOf(dmcVideoContent);

                // DmcInfo.Quality の要素数は動画によって1～5個まで様々である
                // また並びは常に先頭が最高画質、最後尾は最低画質（Mobile）となっている
                // Mobileは常に生成される
                // なのでDmcInfo.Quality[0] は動画ごとによって Dmc_SuperHigh だったり Dmc_Midium であったりまちまち
                // この差を吸収するため、
                // indexを Dmc_Mobile(6)~Dmc_SuperHigh(2) の空間に変換する
                // (qualities.Count - index - 1) によってDmc_Mobileの場合が 0 になる
                var nicoVideoQualityIndex = (int)NicoVideoQuality.Mobile - (qualities.Length - index - 1);
                var quality = (NicoVideoQuality)nicoVideoQualityIndex;

                return quality;
            }
            else
            {
                throw new NotSupportedException(qualityId);
            }
        }
    }
}
