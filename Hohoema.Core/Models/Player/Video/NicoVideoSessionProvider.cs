#nullable enable
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Player.Comment;
using Hohoema.Models.Player.Video.Comment;
using NiconicoToolkit.Video;
using NiconicoToolkit.Video.Watch;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using static NiconicoToolkit.Video.Watch.NicoVideoWatchApiResponse;

namespace Hohoema.Models.Player.Video;



public interface INicoVideoDetails : IVideoDetail
{
    new string Title { get; }
    NicoVideoTag[] Tags { get; }

    TimeSpan VideoLength { get; }

    DateTime SubmitDate { get; }

    string OwnerIconUrl { get; }

    bool IsChannelOwnedVideo { get; }

    string DescriptionHtml { get; }

    double LoudnessCorrectionValue { get; }

    bool IsSeriesVideo { get; }
    WatchApiSeries Series { get; }

    bool IsLikedVideo { get; }
}


public class DmcVideoDetails : INicoVideoDetails
{
    private readonly Response _res;

    internal DmcVideoDetails(Response dmcWatchData)
    {
        _res = dmcWatchData;
        Tags = _res.Tag.Items.Select(x => new NicoVideoTag(x.Name)).ToArray();
    }


    public VideoId VideoId => _res.Video.Id;

    public string Title => _res.Video.Title;

    public NicoVideoTag[] Tags { get; }

    public string ThumbnailUrl => _res.Video.Thumbnail.LargeUrl ?? _res.Video.Thumbnail.MiddleUrl ?? _res.Video.Thumbnail.Url;

    public TimeSpan VideoLength => TimeSpan.FromSeconds(_res.Video.Duration);

    public DateTime SubmitDate => _res.Video.RegisteredAt;

    public int ViewCount => _res.Video.Count.View;

    public int CommentCount => _res.Video.Count.Comment;

    public int MylistCount => _res.Video.Count.Mylist;

    public string ProviderId => _res.Owner?.Id.ToString() ?? _res.Channel?.Id;
    public string ProviderName => _res.Owner?.Nickname ?? _res.Channel?.Name;

    public string OwnerIconUrl => _res.Owner?.IconUrl ?? _res.Channel?.Thumbnail.Url.OriginalString;

    public bool IsChannelOwnedVideo => _res.Channel != null;

    public string DescriptionHtml => _res.Video.Description;

    public double LoudnessCorrectionValue
    {
        get
        {
            try
            {
                if (_res.Media.Delivery != null) 
                {
                    return _res.Media.Delivery.Movie.Audios[0].LoudnessCollection[0].Value.Value;
                }
                else if (_res.Media.Domand != null)
                {
                    return _res.Media.Domand.Audios.FirstOrDefault(x => x.IsAvailable ?? false)?.LoudnessCollection[0].Value ?? 1;
                }
            }
            catch { }

            return 1.0;
        }
    }


    public bool IsSeriesVideo => _res?.Series != null;
    public WatchApiSeries Series => _res?.Series;

    public bool IsLikedVideo => _res.Video.Viewer?.Like.IsLiked ?? false;

    string IVideoDetail.Description => DescriptionHtml;

    bool IVideoDetail.IsDeleted => _res.Video.IsDeleted.Value;

    VideoPermission IVideoDetail.Permission => throw new NotSupportedException();

    TimeSpan IVideoContent.Length => VideoLength;

    DateTime IVideoContent.PostedAt => SubmitDate;


    OwnerType IVideoContentProvider.ProviderType => _res.Channel != null ? OwnerType.Channel : OwnerType.User;

    string IVideoDetail.ProviderIconUrl => OwnerIconUrl;

    bool IEquatable<IVideoContent>.Equals(IVideoContent other)
    {
        return VideoId == other.VideoId;
    }
}

public enum PreparePlayVideoFailedReason
{
    Deleted,
    VideoFormatNotSupported,
    NotPlayPermit_RequirePay,
    NotPlayPermit_RequireChannelMember,
    NotPlayPermit_RequirePremiumMember,
}

public class PreparePlayVideoResult : INiconicoVideoSessionProvider, INiconicoCommentSessionProvider<IVideoComment>
{
    public Exception Exception { get; }
    public bool IsSuccess { get; }


    public VideoId ContentId { get; private set; }

    public ImmutableArray<NicoVideoQualityEntity> AvailableQualities { get; }

    private readonly NicoVideoSessionOwnershipManager _ownershipManager;
    private readonly Response _dmcWatchData;
    private readonly bool _isForceDmc;
    private readonly NiconicoSession _niconicoSession;

    private PreparePlayVideoResult(VideoId contentId, NiconicoSession niconicoSession)
    {
        ContentId = contentId;
        _niconicoSession = niconicoSession;
    }

    public PreparePlayVideoResult(VideoId contentId, NiconicoSession niconicoSession, Exception e)
        : this(contentId, niconicoSession)
    {
        Exception = e;
        AvailableQualities = ImmutableArray<NicoVideoQualityEntity>.Empty;
        IsSuccess = false;
    }

    public PreparePlayVideoResult(VideoId contentId, NiconicoSession niconicoSession, PreparePlayVideoFailedReason failedReason, Response dmcWatchData = null)
        : this(contentId, niconicoSession)
    {
        AvailableQualities = ImmutableArray<NicoVideoQualityEntity>.Empty;
        IsSuccess = false;
        FailedReason = failedReason;
        _dmcWatchData = dmcWatchData;
    }

    public PreparePlayVideoResult(VideoId contentId, NiconicoSession niconicoSession, NicoVideoSessionOwnershipManager ownershipManager, Response dmcWatchData, bool isForceDmc)
        : this(contentId, niconicoSession)
    {
        _ownershipManager = ownershipManager;
        _dmcWatchData = dmcWatchData;
        _isForceDmc = isForceDmc;
        IsSuccess = _dmcWatchData != null;

        if (_isForceDmc && _dmcWatchData?.Media.Delivery is not null)
        {
            AvailableQualities = _dmcWatchData.Media.Delivery.Movie.Videos
                    .Select(x => new NicoVideoQualityEntity(x.IsAvailable.Value, QualityIdToNicoVideoQuality(x.Id), x.Id, (int)x.BitRate.Value, (int)x.Width, (int)x.Height) { Label = x.Label })
                    .ToImmutableArray();
        }
        else if (_dmcWatchData?.Media.Domand is { } domand)
        {
            AvailableQualities = domand.Videos
                    .Select(x => new NicoVideoQualityEntity(x.IsAvailable ?? false, QualityIdToNicoVideoQuality(x.Id), x.Id, x.BitRate, x.Width, x.Height) { Label = x.Label })
                    .ToImmutableArray();
        }
        else if (_dmcWatchData?.Media.Delivery is { } delivery)
        {
            AvailableQualities = delivery.Movie.Videos
                    .Select(x => new NicoVideoQualityEntity(x.IsAvailable.Value, QualityIdToNicoVideoQuality(x.Id), x.Id, (int)x.BitRate.Value, (int)x.Width, (int)x.Height) { Label = x.Label })
                    .ToImmutableArray();
        }        
        else
        {
            throw new NotSupportedException("DmcWatchResponse.Media.DeliveryLegacy not supported");
        }
    }

    public INicoVideoDetails GetVideoDetails()
    {
        return _dmcWatchData != null ? (INicoVideoDetails)new DmcVideoDetails(_dmcWatchData) : throw new ArgumentNullException();
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
    public async Task<IStreamingSession?> CreateVideoSessionAsync(NicoVideoQualityEntity qualityEntity)
    {
        IStreamingSession? streamingSession = null;
        if (_dmcWatchData != null)
        {
            if (_dmcWatchData.Media.Domand is { } domand)
            {
                NicoVideoSessionOwnershipManager.VideoSessionOwnership ownership = await _ownershipManager.TryRentVideoSessionOwnershipAsync(_dmcWatchData.Video.Id, !IsForCacheDownload);
                if (ownership != null)
                {
                    var domandSession = new DomandStreamingSession(_dmcWatchData, domand, _niconicoSession, ownership);
                    if (qualityEntity != null)
                    {
                        domandSession.SetQuality(qualityEntity.Quality);
                    }
                    streamingSession = domandSession;
                }
            }
            else if (_dmcWatchData.Media.Delivery is not null and var delivery)
            {
                throw new NotSupportedException("DmcWatchResponse.Media.Delivery is not supported");
                //NicoVideoSessionOwnershipManager.VideoSessionOwnership ownership = await _ownershipManager.TryRentVideoSessionOwnershipAsync(_dmcWatchData.Video.Id, !IsForCacheDownload);
                //if (ownership != null)
                //{
                //    streamingSession = new DmcVideoStreamingSession(qualityEntity.QualityId, _dmcWatchData, _niconicoSession, ownership);
                //}

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






    public Task<ICommentSession<IVideoComment>> CreateCommentSessionAsync()
    {
        return _dmcWatchData != null ? CreateCommentSession(ContentId, _dmcWatchData) : throw new NotSupportedException();
    }

    private Task<ICommentSession<IVideoComment>> CreateCommentSession(string contentId, Response watchData)
    {
        CommentClient commentClient = new(_niconicoSession, contentId);
        Response dmcRes = watchData;
        //commentClient.CommentServerInfo = new CommentServerInfo()
        //{
        //    ServerUrl = dmcRes.Comment.Threads[0].Server.OriginalString,
        //    VideoId = contentId,
        //    DefaultThreadId = dmcRes.Comment.Threads[0].Id,
        //    ViewerUserId = dmcRes.Viewer?.Id ?? 0,
        //    ThreadKeyRequired = dmcRes.Comment.Threads[0].IsThreadkeyRequired
        //};

        // チャンネル動画ではOnwerはnullになる
        //commentClient.VideoOwnerId = dmcRes.Owner?.Id.ToString();

        commentClient._watchApiData = dmcRes;

        //var communityThread = dmcRes.Comment.Threads.FirstOrDefault(x => x.Label == "community");
        //if (communityThread != null)
        //{
        //    commentClient.CommentServerInfo.CommunityThreadId = communityThread.Id;
        //}

        return Task.FromResult(new VideoCommentService(commentClient, _niconicoSession.UserId) as ICommentSession<IVideoComment>);
    }


    public NicoVideoQuality QualityIdToNicoVideoQuality(string qualityId)
    {
        return _dmcWatchData?.ToNicoVideoQuality(qualityId) ?? NicoVideoQuality.Unknown;
    }
}

public sealed class SessionOwnershipRentFailedEventArgs
{
    private Deferral _deferral;
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
    public SessionOwnershipRemoveRequestedEventArgs(VideoId videoId)
    {
        VideoId = videoId;
    }

    public VideoId VideoId { get; }
}


public class NicoVideoSessionOwnershipManager
{
    public NicoVideoSessionOwnershipManager(NiconicoSession niconicoSession)
    {
        _niconicoSession = niconicoSession;
    }

    private readonly List<VideoSessionOwnership> _VideoSessions = new();

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
        private bool _isDisposed;
        internal VideoSessionOwnership(VideoId videoId, NicoVideoSessionOwnershipManager ownershipManager)
        {
            VideoId = videoId;
            _ownershipManager = ownershipManager;
        }

        public VideoId VideoId { get; }

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

    public async Task<VideoSessionOwnership> TryRentVideoSessionOwnershipAsync(VideoId videoId, bool isPriorityRent)
    {
        if (CanAddDownloadLine())
        {
            VideoSessionOwnership ownership = new(videoId, this);
            _VideoSessions.Add(ownership);
            return ownership;
        }

        TypedEventHandler<NicoVideoSessionOwnershipManager, SessionOwnershipRentFailedEventArgs> handlers = RentFailed;
        if (handlers != null)
        {
            TaskCompletionSource<bool> taskCompletionSource = new();
            SessionOwnershipRentFailedEventArgs args = new(() => taskCompletionSource.SetResult(true));
            handlers.Invoke(this, args);

            await Task.Delay(10);
            if (args.IsUseDeferral)
            {
                _ = await taskCompletionSource.Task;
            }

            if (!args.IsHandled)
            {
                return null;
            }

            if (isPriorityRent)
            {
                VideoSessionOwnership session = _VideoSessions.First();
                session.TriggerStopOwnership();
                (session as IDisposable).Dispose();

                await Task.Delay(10);
            }

            if (CanAddDownloadLine())
            {
                VideoSessionOwnership ownership = new(videoId, this);
                _VideoSessions.Add(ownership);
                return ownership;
            }
        }
        else
        {
            if (isPriorityRent)
            {
                VideoSessionOwnership session = _VideoSessions.First();
                session.TriggerStopOwnership();
                (session as IDisposable).Dispose();

                await Task.Delay(10);
            }

            if (CanAddDownloadLine())
            {
                VideoSessionOwnership ownership = new(videoId, this);
                _VideoSessions.Add(ownership);
                return ownership;
            }
        }

        return null;
    }

    private void Return(VideoSessionOwnership ownership)
    {
        _ = _VideoSessions.Remove(ownership);

        AvairableOwnership?.Invoke(this, new SessionOwnershipRemoveRequestedEventArgs(ownership.VideoId));
    }
}


public class NicoVideoSessionProvider
{
    public NicoVideoSessionProvider(
        NicoVideoProvider nicoVideoProvider,
        NiconicoSession niconicoSession,
        NicoVideoSessionOwnershipManager nicoVideoSessionOwnershipManager,
        PlayerSettings playerSettings        
        )
    {
        _nicoVideoProvider = nicoVideoProvider;
        _niconicoSession = niconicoSession;
        _nicoVideoSessionOwnershipManager = nicoVideoSessionOwnershipManager;
        _playerSettings = playerSettings;
    }

    private readonly NicoVideoProvider _nicoVideoProvider;
    private readonly NiconicoSession _niconicoSession;
    private readonly NicoVideoSessionOwnershipManager _nicoVideoSessionOwnershipManager;
    private readonly PlayerSettings _playerSettings;

    public async Task<PreparePlayVideoResult> PreparePlayVideoAsync(VideoId rawVideoId, bool noHistory = false)
    {
        if (!Helpers.InternetConnection.IsInternet()) { return null; }

        try
        {
            var dmcRes = await _nicoVideoProvider.GetWatchPageResponseAsync(rawVideoId, noHistory);
            if (dmcRes.Data.Response.Video is null)
            {
                throw new NotSupportedException("視聴不可：視聴ページの取得または解析に失敗");
            }
            else if (dmcRes.Data.Response.Video.IsDeleted.Value)
            {
                return new PreparePlayVideoResult(rawVideoId, _niconicoSession, PreparePlayVideoFailedReason.Deleted, dmcRes.Data.Response);
            }
            else if (dmcRes.Data.Response.Media.Domand == null)
            {
                Preview preview = dmcRes.Data.Response.Payment.Preview;
                if (preview.Premium.IsEnabled.Value)
                {
                    return new PreparePlayVideoResult(rawVideoId, _niconicoSession, PreparePlayVideoFailedReason.NotPlayPermit_RequirePremiumMember, dmcRes.Data.Response);

                }
                else
                {
                    return preview.Ppv.IsEnabled.Value
                        ? new PreparePlayVideoResult(rawVideoId, _niconicoSession, PreparePlayVideoFailedReason.NotPlayPermit_RequirePay, dmcRes.Data.Response)
                        : preview.Admission.IsEnabled.Value
                                            ? new PreparePlayVideoResult(rawVideoId, _niconicoSession, PreparePlayVideoFailedReason.NotPlayPermit_RequireChannelMember, dmcRes.Data.Response)
                                            : throw new NotSupportedException("視聴不可：不明な理由で視聴不可");
                }
            }

            return new PreparePlayVideoResult(rawVideoId, _niconicoSession, _nicoVideoSessionOwnershipManager, dmcRes.Data.Response, _playerSettings.ForceUsingDmcVideoOrigin)
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


    private static readonly Regex NiconicoContentUrlRegex = new(@"https?:\/\/[a-z]+\.nicovideo\.jp\/([a-z]+)\/([a-z][a-z][0-9]+|[0-9]+)");
    private static readonly Regex GeneralUrlRegex = new(@"https?:\/\/([a-zA-Z0-9.\/?=_-]*)");

    public static VideoRelatedInfomation GetVideoRelatedInfomationWithVideoDescription(string rawVideoId, string descriptionHtml)
    {
        if (string.IsNullOrEmpty(descriptionHtml)) { return null; }

        VideoRelatedInfomation info = new();
        MatchCollection niconicoContentMatchs = NiconicoContentUrlRegex.Matches(descriptionHtml);
        foreach (Match match in niconicoContentMatchs.Cast<Match>())
        {
            string contentType = match.Groups[1].Value;
            string contentId = match.Groups[2].Value;

            // TODO: 
            info.NiconicoContentIds.Add(new NiconicoContent()
            {
                Type = contentType,
                Id = contentId
            });
        }

        return info;
    }

    public static IList<Uri> GetGeneralUrlsWithVideoDescription(string rawVideoId, string descriptionHtml)
    {
        if (string.IsNullOrEmpty(descriptionHtml)) { return null; }

        List<Uri> uris = new();
        MatchCollection generalUrlMatchs = GeneralUrlRegex.Matches(descriptionHtml);

        foreach (Match match in generalUrlMatchs.Cast<Match>().Where(x => !NiconicoContentUrlRegex.IsMatch(x.Value)))
        {
            string url = match.Groups[1].Value;
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
    public static NicoVideoQuality ToNicoVideoQuality(this Response dmcWatchData, string qualityId)
    {
        if (dmcWatchData.Media.Domand is { } domand
            && domand.Videos.FirstOrDefault(x => x.Id == qualityId) is { }  videoQuality
            )
        {
            return videoQuality.QualityLevel switch
            {
                0 => NicoVideoQuality.Mobile,
                1 => NicoVideoQuality.Low,
                2 => NicoVideoQuality.Midium,
                3 => NicoVideoQuality.High,
                4 => NicoVideoQuality.SuperHigh,
                _ => NicoVideoQuality.Unknown,
            };
        }

        NicoVideoWatchApiResponse.Video dmcVideoContent = dmcWatchData?.Media.Delivery.Movie.Videos.FirstOrDefault(x => x.Id == qualityId);
        if (dmcVideoContent != null)
        {
            NicoVideoWatchApiResponse.Video[] qualities = dmcWatchData.Media.Delivery.Movie.Videos;
            int index = Array.IndexOf(qualities, dmcVideoContent);

            // DmcInfo.Quality の要素数は動画によって1～5個まで様々である
            // また並びは常に先頭が最高画質、最後尾は最低画質（Mobile）となっている
            // Mobileは常に生成される
            // なのでDmcInfo.Quality[0] は動画ごとによって Dmc_SuperHigh だったり Dmc_Midium であったりまちまち
            // この差を吸収するため、
            // indexを Dmc_Mobile(6)~Dmc_SuperHigh(2) の空間に変換する
            // (qualities.Count - index - 1) によってDmc_Mobileの場合が 0 になる
            int nicoVideoQualityIndex = (int)NicoVideoQuality.Mobile - (qualities.Length - index - 1);
            NicoVideoQuality quality = (NicoVideoQuality)nicoVideoQualityIndex;

            return quality;
        }
        else
        {
            throw new NotSupportedException(qualityId);
        }
    }
}
