using FFmpegInterop;
using Mntone.Nico2;
using Mntone.Nico2.Videos.Comment;
using Mntone.Nico2.Videos.Dmc;
using Mntone.Nico2.Videos.Thumbnail;
using Mntone.Nico2.Videos.WatchAPI;
using NicoPlayerHohoema.Models.Db;
using NicoPlayerHohoema.Helpers;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using System.Collections.Immutable;

namespace NicoPlayerHohoema.Models
{

    public class CommentServerInfo
    {
        public int ViewerUserId { get; set; }
        public string VideoId { get; set; }
        public string ServerUrl { get; set; }
        public int DefaultThreadId { get; set; }
        public int? CommunityThreadId { get; set; }

        public bool ThreadKeyRequired { get; set; }
    }


	public class NicoVideo
	{
        public CommentClient CommentClient { get; private set; }

        public string RawVideoId { get; private set; }

        NiconicoContentProvider _ContentProvider;
        VideoCacheManager CacheManager;
        NiconicoContext _Context;


        private object _LastAccessResponse;
        public WatchApiResponse LastAccessWatchApiResponse => _LastAccessResponse as WatchApiResponse;
        public DmcWatchData LastAccessDmcWatchResponse => _LastAccessResponse as DmcWatchData;

        public NicoVideo(string rawVideoid, NiconicoContentProvider contentProvider, NiconicoContext context, VideoCacheManager manager)
		{
            RawVideoId = rawVideoid;
            _ContentProvider = contentProvider;
            _Context = context;
			CacheManager = manager;

            CommentClient = new CommentClient(_Context, RawVideoId);            
        }

        #region Playback


        /// <summary>
		/// 動画ストリームの取得します
		/// 他にダウンロードされているアイテムは強制的に一時停止し、再生終了後に再開されます
		/// </summary>
        /// <exception cref="NotSupportedException" />
		public async Task<IVideoStreamingSession> CreateVideoStreamingSession(NicoVideoQuality quality, bool forceDownload = false)
        {
            // オンラインの場合は削除済みかを確認する
            object watchRes = null;

            if (Helpers.InternetConnection.IsInternet())
            {
                // 動画視聴ページへアクセス
                // 動画再生準備およびコメント取得準備が行われる
                watchRes = await VisitWatchPage(quality);

                // 動画情報ページアクセスだけでも内部の動画情報データは更新される
                var videoInfo = Database.NicoVideoDb.Get(RawVideoId);

                // 動作互換性のためにサムネから拾うように戻す？
                // var videoInfo = await _ContentProvider.GetNicoVideoInfo(RawVideoId);

                if (videoInfo.IsDeleted)
                {
                    // ニコニコサーバー側で動画削除済みの場合は再生不可
                    // （NiconnicoContentProvider側で動画削除動作を実施している）
                    throw new NotSupportedException($"動画は {videoInfo.PrivateReasonType.ToCulturelizeString()} のため視聴できません");
                }

            }

            if (!forceDownload)
            {
                // キャッシュ済みアイテムを問い合わせ
                var cacheRequests = await CacheManager.GetCacheRequest(RawVideoId);

                NicoVideoCacheRequest playCandidateRequest = null;
                var req = cacheRequests.FirstOrDefault(x => x.Quality == quality);

                if (req is NicoVideoCacheInfo || req is NicoVideoCacheProgress)
                {
                    playCandidateRequest = req;
                }

                if (req == null)
                {
                    var playableReq = cacheRequests.Where(x => x is NicoVideoCacheInfo || x is NicoVideoCacheProgress);
                    if (playableReq.Any())
                    {
                        // 画質指定がない、または指定画質のキャッシュがない場合には
                        // キャッシュが存在する画質（高画質優先）を取り出す
                        playCandidateRequest = playableReq.OrderBy(x => x.Quality).FirstOrDefault();
                    }
                }

                if (playCandidateRequest is NicoVideoCacheInfo)
                {
                    var playCandidateCache = playCandidateRequest as NicoVideoCacheInfo;
                    try
                    {
                        var file = await StorageFile.GetFileFromPathAsync(playCandidateCache.FilePath);
                        return new LocalVideoStreamingSession(file, playCandidateCache.Quality, _Context);
                    }
                    catch
                    {
                        Debug.WriteLine("動画視聴時にキャッシュが見つかったが、キャッシュファイルを利用した再生セッションの作成に失敗、オンライン再生を試行します");
                    }
                }
                else if (playCandidateRequest is NicoVideoCacheProgress)
                {
                    /*
                    if (Helpers.ApiContractHelper.IsFallCreatorsUpdateAvailable)
                    {
                        var playCandidateCacheProgress = playCandidateRequest as NicoVideoCacheProgress;
                        var op = playCandidateCacheProgress.DownloadOperation;
                        var refStream = op.GetResultRandomAccessStreamReference();
                        return new DownloadProgressVideoStreamingSession(refStream, playCandidateCacheProgress.Quality);
                    }
                    */
                }
            }

            // キャッシュがない場合はオンラインで再生
            if (watchRes is WatchApiResponse)
            {
                var res = watchRes as WatchApiResponse;
                if (res.flashvars.movie_type == "swf")
                {
                    throw new NotSupportedException("SWF形式の動画はサポートしていません");
                }

                if (res.VideoUrl.OriginalString.StartsWith("rtmp"))
                {
                    throw new NotSupportedException("RTMP形式の動画はサポートしていません");
                }

                if (res.IsDeleted)
                {
                    throw new NotSupportedException("動画は削除されています");
                }

                return new SmileVideoStreamingSession(
                    res.VideoUrl,
                    _Context
                    );
            }
            else if (watchRes is DmcWatchData)
            {
                var res = watchRes as DmcWatchData;

                if (res.DmcWatchResponse.Video.IsDeleted)
                {
                    throw new NotSupportedException("動画は削除されています");
                }

                if (res.DmcWatchResponse.Video.DmcInfo != null)
                {
                    if (res.DmcWatchResponse.Video?.DmcInfo?.Quality == null)
                    {
                        throw new NotSupportedException("動画の視聴権がありません");
                    }

                    if (quality.IsLegacy() && res.DmcWatchResponse.Video.SmileInfo != null)
                    {
                        return new SmileVideoStreamingSession(
                            new Uri(res.DmcWatchResponse.Video.SmileInfo.Url),
                            _Context
                            );
                    }
                    else
                    {
                        return new DmcVideoStreamingSession(
                            res,
                            quality.IsDmc() ? quality : NicoVideoQuality.Dmc_High,
                            _Context
                            );
                    }
                }
                else if (res.DmcWatchResponse.Video.SmileInfo != null)
                {
                    return new SmileVideoStreamingSession(
                        new Uri(res.DmcWatchResponse.Video.SmileInfo.Url),
                        _Context
                        );
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
        }

        #endregion

        private async Task<DmcWatchData> GetDmcWatchResponse()
        {
            return await _ContentProvider.GetDmcWatchResponse(RawVideoId);
        }

        private async Task<WatchApiResponse> GetWatchApiResponse(bool forceLoqQuality = false)
        {
            return await _ContentProvider.GetWatchApiResponse(RawVideoId, forceLoqQuality);
        }


        public async Task<object> VisitWatchPage(NicoVideoQuality quality)
		{
            if (!Helpers.InternetConnection.IsInternet()) { return null; }
            
            object res = null;
            try
            {
                if (quality.IsLegacy())
                {
                    res = await GetWatchApiResponse(true);
                }
                else
                {
                    var dmcRes = await GetDmcWatchResponse();
                    res = dmcRes;
                }
            }
            catch
            {
                if (quality.IsLegacy()) { throw; }
                await Task.Delay(TimeSpan.FromSeconds(1));
                res = await GetWatchApiResponse(quality == NicoVideoQuality.Smile_Low);
            }

            _LastAccessResponse = res;

            if (res is WatchApiResponse)
            {
                var watchApiRes = res as WatchApiResponse;
                CommentClient = new CommentClient(_Context, new CommentServerInfo()
                {
                    ServerUrl = watchApiRes.CommentServerUrl.OriginalString,
                    VideoId = RawVideoId,
                    DefaultThreadId = (int)watchApiRes.ThreadId,
                    CommunityThreadId = (int)watchApiRes.OptionalThreadId,
                    ViewerUserId = watchApiRes.viewerInfo.id,
                    ThreadKeyRequired = watchApiRes.IsKeyRequired
                });

                return res;
            }
            else if (res is DmcWatchData)
            {
                var watchdata = res as DmcWatchData;
                var dmcRes = watchdata.DmcWatchResponse;
                CommentClient = new CommentClient(_Context, new CommentServerInfo()
                {
                    ServerUrl = dmcRes.Thread.ServerUrl,
                    VideoId = RawVideoId,
                    DefaultThreadId = int.Parse(dmcRes.Thread.Ids.Default),
                    ViewerUserId = dmcRes.Viewer.Id,
                    ThreadKeyRequired = dmcRes.Video.IsOfficial
                })
                {
                    LastAccessDmcWatchResponse = dmcRes
                };

                if (int.TryParse(dmcRes.Thread.Ids.Community, out var communityThreadId))
                {
                    CommentClient.CommentServerInfo.CommunityThreadId = communityThreadId;
                }

                return res;
            }
            else
            {
                return null;
            }
        }
	}

    

    public abstract class VideoStreamingSession : IVideoStreamingSession, IDisposable
    {
        // Note: 再生中のハートビート管理を含めた管理
        // MediaSourceをMediaPlayerに設定する役割
        


        public abstract NicoVideoQuality Quality { get; }

        protected NiconicoContext _Context;
        FFmpegInteropMSS _VideoMSS;
        MediaSource _MediaSource;

        MediaPlayer _PlayingMediaPlayer;



        public VideoStreamingSession(NiconicoContext context)
        {
            _Context = context;
        }

        public async Task StartPlayback(MediaPlayer player)
        {
            var videoUri = await GetVideoContentUri();

            // Note: HTML5プレイヤー移行中のFLV動画に対するフォールバック処理
            // サムネではContentType=FLV,SWFとなっていても、
            // 実際に渡される動画ストリームのContentTypeがMP4となっている場合がある

            var videoContentType = MovieType.Mp4;
            MediaSource mediaSource = null;
            
            if (!videoUri.IsFile)
            {
                // オンラインからの再生


                var tempStream = await HttpSequencialAccessStream.CreateAsync(
                    _Context.HttpClient
                    , videoUri
                    );
                if (tempStream is IRandomAccessStreamWithContentType)
                {
                    var contentType = (tempStream as IRandomAccessStreamWithContentType).ContentType;

                    if (contentType.EndsWith("mp4"))
                    {
                        videoContentType = MovieType.Mp4;
                    }
                    else if (contentType.EndsWith("flv"))
                    {
                        videoContentType = MovieType.Flv;
                    }
                    else if (contentType.EndsWith("swf"))
                    {
                        videoContentType = MovieType.Swf;
                    }
                    else
                    {
                        throw new NotSupportedException($"{contentType} is not supported video format.");
                    }
                }

                if (videoContentType != MovieType.Mp4)
                {
                    _VideoMSS = FFmpegInteropMSS.CreateFFmpegInteropMSSFromStream(tempStream, false, false);
                    var mss = _VideoMSS.GetMediaStreamSource();
                    mss.SetBufferedRange(TimeSpan.Zero, TimeSpan.Zero);
                    mediaSource = MediaSource.CreateFromMediaStreamSource(mss);
                }
                else
                {
                    tempStream.Dispose();
                    tempStream = null;

                    mediaSource = MediaSource.CreateFromUri(videoUri);
                }
            }
            else
            {
                // ローカル再生時


                var file = await StorageFile.GetFileFromPathAsync(videoUri.OriginalString);
                var stream = await file.OpenReadAsync();
                var contentType = stream.ContentType;

                if (contentType == null) { throw new NotSupportedException("can not play video file. " + videoUri.OriginalString); }

                if (contentType == "video/mp4")
                {
                    mediaSource = MediaSource.CreateFromStream(stream, contentType);
                }
                else
                {
                    _VideoMSS = FFmpegInteropMSS.CreateFFmpegInteropMSSFromStream(stream, false, false);
                    var mss = _VideoMSS.GetMediaStreamSource();
                    mss.SetBufferedRange(TimeSpan.Zero, TimeSpan.Zero);
                    mediaSource = MediaSource.CreateFromMediaStreamSource(mss);
                }
            }

            
            if (mediaSource != null)
            {
                player.Source = mediaSource;
                _MediaSource = mediaSource;
                _PlayingMediaPlayer = player;

                OnStartStreaming();
            }
            else
            {
                throw new NotSupportedException("can not play video. Video URI: " + videoUri);
            }
        }


        public async Task<Uri> GetDownloadUrlAndSetupDonwloadSession()
        {
            var videoUri = await GetVideoContentUri();

            if (videoUri != null)
            {
                OnStartStreaming();

                return videoUri;
            }
            else
            {
                return null;
            }
        }

        protected abstract Task<Uri> GetVideoContentUri();

        protected virtual void OnStartStreaming() { }
        protected virtual void OnStopStreaming() { }

        public void Dispose()
        {
            OnStopStreaming();

            if (_PlayingMediaPlayer != null)
            {
                _PlayingMediaPlayer.Pause();
                _PlayingMediaPlayer.Source = null;
                _PlayingMediaPlayer = null;

                _VideoMSS?.Dispose();
                _MediaSource?.Dispose();
            }
        }
    }

    public class SmileVideoStreamingSession : VideoStreamingSession
    {
        // Note: 再生中のハートビート管理を含めた管理
        // MediaSourceをMediaPlayerに設定する役割

        public override NicoVideoQuality Quality { get; }

        public Uri VideoUrl { get; }

        public SmileVideoStreamingSession(Uri videoUrl, NiconicoContext context)
            : base(context)
        {
            VideoUrl = videoUrl;
            if (VideoUrl.OriginalString.EndsWith("low"))
            {
                Quality = NicoVideoQuality.Smile_Low;
            }
            else
            {
                Quality = NicoVideoQuality.Smile_Original;
            }
        }

        protected override Task<Uri> GetVideoContentUri()
        {
            return Task.FromResult(VideoUrl);
        }

    }

    public class DmcVideoStreamingSession : VideoStreamingSession
    {
        // Note: 再生中のハートビート管理を含めた管理
        // MediaSourceをMediaPlayerに設定する役割


        NicoVideoQuality _Quality;
        public override NicoVideoQuality Quality => _Quality;

        public DmcWatchResponse DmcWatchResponse { get; private set; }

        private DispatcherTimer _DmcSessionHeartbeatTimer;

        private static AsyncLock DmcSessionHeartbeatLock = new AsyncLock();

        private int _HeartbeatCount = 0;
        private bool IsFirstHeartbeat => _HeartbeatCount == 0;

        public NicoVideoQuality RequestedQuality { get; }

        VideoContent _VideoContent;

        private VideoContent ResetActualQuality()
        {
            if (DmcWatchResponse?.Video.DmcInfo?.Quality?.Videos == null)
            {
                return null;
            }

            var videos = DmcWatchResponse.Video.DmcInfo.Quality.Videos;

            int qulity_position = 0;
            switch (RequestedQuality)
            {
                case NicoVideoQuality.Dmc_High:
                    // 4 -> 0
                    // 3 -> x
                    // 2 -> x
                    // 1 -> x
                    qulity_position = 4;
                    break;
                case NicoVideoQuality.Dmc_Midium:
                    // 4 -> 1
                    // 3 -> 0
                    // 2 -> x
                    // 1 -> x
                    qulity_position = 3;
                    break;
                case NicoVideoQuality.Dmc_Low:
                    // 4 -> 2
                    // 3 -> 1
                    // 2 -> 0
                    // 1 -> x
                    qulity_position = 2;
                    break;
                case NicoVideoQuality.Dmc_Mobile:
                    // 4 -> 3
                    // 3 -> 2
                    // 2 -> 1
                    // 1 -> 0
                    qulity_position = 1;
                    break;
                default:
                    throw new Exception();
            }

            VideoContent result = null;

            var pos = videos.Count - qulity_position;
            if (videos.Count >= qulity_position)
            {
                result = videos.ElementAtOrDefault(pos) ?? null;
            }
            
            if (result == null || !result.Available)
            {
                result = videos.FirstOrDefault(x => x.Available);
            }

            

            return result;
        }

        DmcWatchData _DmcWatchData;
        static DmcSessionResponse _DmcSessionResponse;

        public DmcVideoStreamingSession(DmcWatchData res, NicoVideoQuality requestQuality, NiconicoContext context)
            : base(context)
        {
            RequestedQuality = requestQuality;
            _DmcWatchData = res;
            DmcWatchResponse = res.DmcWatchResponse;

            _VideoContent = ResetActualQuality();

            if (_VideoContent != null)
            {
                if (_VideoContent.Bitrate >= 1400_000)
                {
                    _Quality = NicoVideoQuality.Dmc_High;
                }
                else if (_VideoContent.Bitrate >= 1000_000)
                {
                    _Quality = NicoVideoQuality.Dmc_Midium;
                }
                else if (_VideoContent.Bitrate >= 600_000)
                {
                    _Quality = NicoVideoQuality.Dmc_Low;
                }
                else
                {
                    _Quality = NicoVideoQuality.Dmc_Mobile;
                }

                Debug.WriteLine($"bitrate={_VideoContent.Bitrate}, id={_VideoContent.Id}, w={_VideoContent.Resolution.Width}, h={_VideoContent.Resolution.Height}");
                Debug.WriteLine($"quality={_Quality}");
            }
        }

        protected override async Task<Uri> GetVideoContentUri()
        {
            if (DmcWatchResponse == null) { return null; }

            if (DmcWatchResponse.Video.DmcInfo == null) { return null; }

            if (_VideoContent == null)
            {
                return null;
            }

            try
            {
                // 直前に同一動画を見ていた場合には、動画ページに再アクセスする
                DmcSessionResponse clearPreviousSession = null;
                if (_DmcSessionResponse != null)
                {
                    if (_DmcSessionResponse.Data.Session.RecipeId.EndsWith(DmcWatchResponse.Video.Id))
                    {
                        clearPreviousSession = _DmcSessionResponse;
                        _DmcSessionResponse = null;
                        DmcWatchResponse = await _Context.Video.GetDmcWatchJsonAsync(DmcWatchResponse.Video.Id, _DmcWatchData.DmcWatchEnvironment.PlaylistToken);
                    }
                }

                _DmcSessionResponse = await _Context.Video.GetDmcSessionResponse(DmcWatchResponse, _VideoContent);

                if (_DmcSessionResponse == null) { return null; }

                if (clearPreviousSession != null)
                {
                    await _Context.Video.DmcSessionExitHeartbeatAsync(DmcWatchResponse, clearPreviousSession);
                }

                return new Uri(_DmcSessionResponse.Data.Session.ContentUri);
            }
            catch
            {
                return null;
            }
        }

        protected override void OnStartStreaming()
        {
            if (DmcWatchResponse != null && _DmcSessionResponse != null)
            {
                Debug.WriteLine($"{DmcWatchResponse.Video.Title} のハートビートを開始しました");
                _DmcSessionHeartbeatTimer = new DispatcherTimer();
                _DmcSessionHeartbeatTimer.Interval = TimeSpan.FromSeconds(30);
                _DmcSessionHeartbeatTimer.Tick += _DmcSessionHeartbeatTimer_Tick;

                _DmcSessionHeartbeatTimer.Start();
            }
        }

        private async void _DmcSessionHeartbeatTimer_Tick(object sender, object e)
        {
            using (var releaser = await DmcSessionHeartbeatLock.LockAsync())
            {
                Debug.WriteLine($"{DmcWatchResponse.Video.Title} のハートビート {_HeartbeatCount + 1}回目");

                if (IsFirstHeartbeat)
                {
                    await _Context.Video.DmcSessionFirstHeartbeatAsync(DmcWatchResponse, _DmcSessionResponse);
                    Debug.WriteLine($"{DmcWatchResponse.Video.Title} の初回ハートビート実行");
                    await Task.Delay(2);
                }

                await _Context.Video.DmcSessionHeartbeatAsync(DmcWatchResponse, _DmcSessionResponse);
                Debug.WriteLine($"{DmcWatchResponse.Video.Title} のハートビート実行");

                _HeartbeatCount++;
            }
        }

        protected override async void OnStopStreaming()
        {
            if (_DmcSessionHeartbeatTimer != null)
            {
                _DmcSessionHeartbeatTimer.Stop();
                _DmcSessionHeartbeatTimer = null;
                Debug.WriteLine($"{DmcWatchResponse.Video.Title} のハートビートを終了しました");
            }

            if (_DmcSessionResponse != null)
            {
                await _Context.Video.DmcSessionLeaveAsync(DmcWatchResponse, _DmcSessionResponse);
            }

        }
    }


    public class LocalVideoStreamingSession : VideoStreamingSession
    {
        // Note: 再生中のハートビート管理を含めた管理
        // MediaSourceをMediaPlayerに設定する役割



        public override NicoVideoQuality Quality { get; }

        public StorageFile File { get; }

        public LocalVideoStreamingSession(StorageFile file, NicoVideoQuality requestQuality, NiconicoContext context)
            : base(context)
        {
            File = file;
            Quality = requestQuality;
        }

        protected override Task<Uri> GetVideoContentUri()
        {
            return Task.FromResult(new Uri(File.Path));
        }
    }

    public class DownloadProgressVideoStreamingSession : IVideoStreamingSession, IDisposable
    {
        // Note: 再生中のハートビート管理を含めた管理
        // MediaSourceをMediaPlayerに設定する役割



        public NicoVideoQuality Quality { get; }

        public IRandomAccessStreamReference StreamRef { get; }
        public IRandomAccessStream _Stream;
        FFmpegInteropMSS _VideoMSS;
        MediaSource _MediaSource;

        MediaPlayer _PlayingMediaPlayer;

        public DownloadProgressVideoStreamingSession(IRandomAccessStreamReference streamRef, NicoVideoQuality requestQuality)
        {
            StreamRef = streamRef;
            Quality = requestQuality;
        }


        public void Dispose()
        {
            _MediaSource.Dispose();
            _MediaSource = null;
            _VideoMSS.Dispose();
            _VideoMSS = null;
            _Stream.Dispose();
            _Stream = null;

            _PlayingMediaPlayer = null;
        }

        public Task<Uri> GetDownloadUrlAndSetupDonwloadSession()
        {
            throw new NotSupportedException();
        }

        public async Task StartPlayback(MediaPlayer player)
        {
            string contentType = string.Empty;

            var stream = await StreamRef.OpenReadAsync();
            if (!stream.ContentType.EndsWith("mp4"))
            {
                _VideoMSS = FFmpegInteropMSS.CreateFFmpegInteropMSSFromStream(stream, false, false);
                var mss = _VideoMSS.GetMediaStreamSource();
                mss.SetBufferedRange(TimeSpan.Zero, TimeSpan.Zero);
                _MediaSource = MediaSource.CreateFromMediaStreamSource(mss);
            }
            else
            {
                _MediaSource = MediaSource.CreateFromStream(stream, stream.ContentType);
            }


            if (_MediaSource != null)
            {
                player.Source = _MediaSource;
                _Stream = stream;
                _PlayingMediaPlayer = player;
            }
            else
            {
                throw new NotSupportedException("can not play video. vide source from download progress stream.");
            }

        }
    }


    public class CommentSubmitInfo
    {
        public string Ticket { get; set; }
        public int CommentCount { get; set; }
    }



    public class CommentClient
    {
        public string RawVideoId { get; }
        public NiconicoContext Context { get; }
        public CommentServerInfo CommentServerInfo { get; private set; }

        private CommentResponse CachedCommentResponse { get; set; }

        internal DmcWatchResponse LastAccessDmcWatchResponse { get; set; }

        private CommentSubmitInfo SubmitInfo { get; set; }


        public CommentClient(NiconicoContext context, string rawVideoid)
        {
            RawVideoId = rawVideoid;
            Context = context;
        }

        public CommentClient(NiconicoContext context, CommentServerInfo serverInfo)
        {
            RawVideoId = serverInfo.VideoId;
            Context = context;
            CommentServerInfo = serverInfo;
        }

        public List<Chat> GetCommentsFromLocal()
        {
            var j = CommentDb.Get(RawVideoId);
            return j?.GetComments();
        }

        // コメントのキャッシュまたはオンラインからの取得と更新
        public async Task<List<Chat>> GetComments()
        {
            if (CommentServerInfo == null) { return new List<Chat>(); }

            CommentResponse commentRes = null;
            try
            {
                
                commentRes = await ConnectionRetryUtil.TaskWithRetry(async () =>
                {
                    return await this.Context.Video
                        .GetCommentAsync(
                            (int)CommentServerInfo.ViewerUserId,
                            CommentServerInfo.ServerUrl,
                            CommentServerInfo.DefaultThreadId,
                            CommentServerInfo.ThreadKeyRequired
                        );
                });

            }
            catch
            {
                
            }


            if (commentRes?.Chat.Count == 0)
            {
                try
                {
                    if (CommentServerInfo.CommunityThreadId.HasValue)
                    {
                        commentRes = await ConnectionRetryUtil.TaskWithRetry(async () =>
                        {
                            return await Context.Video
                                .GetCommentAsync(
                                    (int)CommentServerInfo.ViewerUserId,
                                    CommentServerInfo.ServerUrl,
                                    CommentServerInfo.CommunityThreadId.Value,
                                    CommentServerInfo.ThreadKeyRequired
                                );
                        });
                    }
                }
                catch { }
            }

            if (commentRes != null)
            {
                CachedCommentResponse = commentRes;
                CommentDb.AddOrUpdate(RawVideoId, commentRes);
            }

            if (commentRes != null && SubmitInfo == null)
            {
                SubmitInfo = new CommentSubmitInfo();
                SubmitInfo.Ticket = commentRes.Thread.Ticket;
                if (int.TryParse(commentRes.Thread.CommentCount, out int count))
                {
                    SubmitInfo.CommentCount = count + 1;
                }
            }

            return commentRes?.Chat;


        }


        public bool CanGetCommentsFromNMSG 
        {
            get
            {
                return LastAccessDmcWatchResponse != null &&
                    LastAccessDmcWatchResponse.Video.DmcInfo != null;
            }
        }

        public async Task<NMSG_Response> GetCommentsFromNMSG()
        {
            if (LastAccessDmcWatchResponse == null) { return null; }

            var res = await Context.Video.GetNMSGCommentAsync(LastAccessDmcWatchResponse);

            if (res != null && SubmitInfo == null)
            {
                SubmitInfo = new CommentSubmitInfo();
                SubmitInfo.Ticket = res.Thread.Ticket;
                SubmitInfo.CommentCount = LastAccessDmcWatchResponse.Thread.CommentCount + 1;
            }

            return res;
        }

        public async Task<PostCommentResponse> SubmitComment(string comment, TimeSpan position, string commands)
        {
            if (CommentServerInfo == null) { return null; }
            if (SubmitInfo == null) { return null; }

            if (CommentServerInfo == null)
            {
                throw new Exception("コメント投稿には事前に動画ページへのアクセスとコメント情報取得が必要です");
            }

            PostCommentResponse response = null;
            foreach (var cnt in Enumerable.Range(0, 2))
            {
                try
                {
                    response = await Context.Video.PostCommentAsync(
                        CommentServerInfo.ServerUrl,
                        CommentServerInfo.DefaultThreadId.ToString(),
                        SubmitInfo.Ticket,
                        SubmitInfo.CommentCount,
                        comment,
                        position,
                        commands
                        );
                }
                catch
                {
                    // コメントデータを再取得してもう一度？
                    return null;
                }

                if (response.Chat_result.Status == ChatResult.Success)
                {
                    SubmitInfo.CommentCount++;
                    break;
                }

                Debug.WriteLine("コメ投稿失敗: コメ数 " + SubmitInfo.CommentCount);

                await Task.Delay(1000);

                try
                {
                    var videoInfo = await Context.Search.GetVideoInfoAsync(RawVideoId);
                    SubmitInfo.CommentCount = int.Parse(videoInfo.Thread.num_res);
                    Debug.WriteLine("コメ数再取得: " + SubmitInfo.CommentCount);
                }
                catch
                {
                }
            }

            return response;
        }

        public bool IsAllowAnnonimityComment
        {
            get
            {
                if (LastAccessDmcWatchResponse == null) { return false; }

                if (LastAccessDmcWatchResponse.Channel != null) { return false; }

                if (LastAccessDmcWatchResponse.Community != null) { return false; }

                return true;
            }
        }

        public bool CanSubmitComment
        {
            get
            {
                if (!Helpers.InternetConnection.IsInternet()) { return false; }

                if (CommentServerInfo == null) { return false; }
                if (SubmitInfo == null) { return false; }

                return true;
            }
        }
        
    }
}
