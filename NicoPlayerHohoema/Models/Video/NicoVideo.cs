using FFmpegInterop;
using Mntone.Nico2;
using Mntone.Nico2.Videos.Comment;
using Mntone.Nico2.Videos.Dmc;
using Mntone.Nico2.Videos.Thumbnail;
using Mntone.Nico2.Videos.WatchAPI;
using NicoPlayerHohoema.Models.Db;
using NicoPlayerHohoema.Util;
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
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.Models
{

    public class CommentServerInfo
    {
        public string ServerUrl { get; set; }
        public int DefaultThreadId { get; set; }
        public int? CommunityThreadId { get; set; }

        public bool ThreadKeyRequired { get; set; }
    }


	public class NicoVideo : BindableBase
	{
		private NicoVideoQuality _VisitedPageType = NicoVideoQuality.Low;

        AsyncLock _InitializeLock = new AsyncLock();
		bool _IsInitialized = false;
		bool _thumbnailInitialized = false;

        AsyncLock _BufferingLock = new AsyncLock();
        IDisposable _BufferingLockReleaser = null;

        public CommentClient CommentClient { get; private set; }


        public bool IsThumbnailInitialized => _thumbnailInitialized;


        public NicoVideo(HohoemaApp app, string rawVideoid, NiconicoMediaManager manager)
		{
			HohoemaApp = app;
			RawVideoId = rawVideoid;
			_NiconicoMediaManager = manager;

			_InterfaceByQuality = new Dictionary<NicoVideoQuality, DividedQualityNicoVideo>();
            QualityDividedVideos = new ReadOnlyObservableCollection<DividedQualityNicoVideo>(_QualityDividedVideos);
            CommentClient = new CommentClient(HohoemaApp, RawVideoId);
        }

        public async Task Initialize()
        {
            using (var releaser = await _InitializeLock.LockAsync())
            {
                if (_IsInitialized) { return; }

                if (HohoemaApp.ServiceStatus >= HohoemaAppServiceLevel.OnlineWithoutLoggedIn)
                {
                    Debug.WriteLine("start initialize : " + RawVideoId);

                    await UpdateWithThumbnail();

                    _IsInitialized = true;
                }
                else if (!_thumbnailInitialized && HohoemaApp.ServiceStatus == HohoemaAppServiceLevel.Offline)
                {
                    await FillVideoInfoFromDb();
                }
            }
        }




        #region Cache Manage


        #endregion



        #region Playback


        FFmpegInteropMSS _VideoMSS;
        MediaSource _MediaSource;

        /// <summary>
		/// 動画ストリームの取得します
		/// 他にダウンロードされているアイテムは強制的に一時停止し、再生終了後に再開されます
		/// </summary>
		public async Task<NicoVideoQuality> StartPlay(NicoVideoQuality? quality, TimeSpan? initialPosition = null)
        {
            IfVideoDeletedThrowException();

            // 再生動画画質決定
            // 指定された画質＞キャッシュされた画質＞デフォルト指定画質＞再生可能な画質
            DividedQualityNicoVideo divided = null;
            if (quality.HasValue)
            {
                divided = GetDividedQualityNicoVideo(quality.Value);
            }

            if (divided == null || !divided.CanPlay)
            {
                var cachedQuality = GetAllQuality().Where(x => x.IsCached).FirstOrDefault();
                if (cachedQuality != null)
                {
                    divided = cachedQuality;
                }
                else if (quality == NicoVideoQuality.Original)
                {
                    divided = GetDividedQualityNicoVideo(NicoVideoQuality.Low);
                }
                else
                {
                    divided = GetDividedQualityNicoVideo(HohoemaApp.UserSettings.PlayerSettings.DefaultQuality);

                    if (!divided.CanPlay)
                    {
                        foreach (var dmcQuality in GetAllQuality().Where(x => x.Quality.IsDmc()))
                        {
                            if (dmcQuality.CanPlay)
                            {
                                divided = dmcQuality;
                                break;
                            }
                        }
                    }

                    if (divided == null || !divided.CanPlay)
                    {
                        divided = GetDividedQualityNicoVideo(NicoVideoQuality.Original);
                        if (!divided.CanPlay)
                        {
                            divided = GetDividedQualityNicoVideo(NicoVideoQuality.Low);
                        }
                    }
                }
            }

            if (divided == null || !divided.CanPlay)
            {
                throw new NotSupportedException("再生可能な動画品質を見つけられませんでした。");
            }

            Debug.WriteLine(divided.Quality);

            MediaSource mediaSource = null;

            // キャッシュ済みの場合は
            if (divided.HasCache)
            {
                var file = await divided.GetCacheFile();
                NicoVideoCachedStream = await file.OpenReadAsync();

                await OnCacheRequested();

                string contentType = null;

                if (NicoVideoCachedStream is IRandomAccessStreamWithContentType)
                {
                    contentType = (NicoVideoCachedStream as IRandomAccessStreamWithContentType).ContentType;
                }

                if (contentType == null) { throw new Exception("unknown movie content type"); }

                if (contentType == "video/mp4")
                {
                    mediaSource = MediaSource.CreateFromStream(NicoVideoCachedStream, contentType);
                }
                else
                {
                    _VideoMSS = FFmpegInteropMSS.CreateFFmpegInteropMSSFromStream(NicoVideoCachedStream, false, false);
                    var mss = _VideoMSS.GetMediaStreamSource();
                    mss.SetBufferedRange(TimeSpan.Zero, TimeSpan.Zero);
                    mediaSource = MediaSource.CreateFromMediaStreamSource(mss);
                }
                
            }
            else if (ProtocolType == MediaProtocolType.RTSPoverHTTP)
            {
                if (Util.InternetConnection.IsInternet())
                {
                    var videoUrl = await divided.GenerateVideoContentUrl();
                    if (videoUrl == null)
                    {
                        divided = GetDividedQualityNicoVideo(NicoVideoQuality.Low);
                        videoUrl = await divided.GenerateVideoContentUrl();
                    }

                    _VideoMSS?.Dispose();

                   
                    if (ContentType != MovieType.Mp4)
                    {
                        // Note: HTML5プレイヤー移行中のFLV動画に対するフォールバック処理
                        // サムネではContentType=FLV,SWFとなっていても、
                        // 実際に渡される動画ストリームのContentTypeがMP4となっている場合がある
                        NicoVideoCachedStream = await HttpSequencialAccessStream.CreateAsync(
                            HohoemaApp.NiconicoContext.HttpClient
                            , videoUrl
                            );

                        if (NicoVideoCachedStream is IRandomAccessStreamWithContentType)
                        {
                            var contentType = (NicoVideoCachedStream as IRandomAccessStreamWithContentType).ContentType;

                            if (contentType.EndsWith("mp4"))
                            {
                                ContentType = MovieType.Mp4;
                            }
                            else if (contentType.EndsWith("flv"))
                            {
                                ContentType = MovieType.Flv;
                            }
                            else if (contentType.EndsWith("swf"))
                            {
                                ContentType = MovieType.Swf;
                            }
                        }

                        if (ContentType != MovieType.Mp4)
                        {
                            _VideoMSS = FFmpegInteropMSS.CreateFFmpegInteropMSSFromStream(NicoVideoCachedStream, false, false);
                            var mss = _VideoMSS.GetMediaStreamSource();
                            mss.SetBufferedRange(TimeSpan.Zero, TimeSpan.Zero);
                            mediaSource = MediaSource.CreateFromMediaStreamSource(mss);
                        }
                        else
                        {
                            NicoVideoCachedStream.Dispose();
                            NicoVideoCachedStream = null;

                            mediaSource = MediaSource.CreateFromUri(videoUrl);
                        }
                    }
                    else 
                    {
                        mediaSource = MediaSource.CreateFromUri(videoUrl);
                    }
                }
            }

            if (initialPosition == null)
            {
                initialPosition = TimeSpan.Zero;
            }

            

            if (mediaSource != null)
            {
                HohoemaApp.MediaPlayer.Source = mediaSource;
                if (initialPosition.HasValue)
                {
                    HohoemaApp.MediaPlayer.PlaybackSession.Position = initialPosition.Value;
                }

                _MediaSource = mediaSource;

                var smtc = HohoemaApp.MediaPlayer.SystemMediaTransportControls;

                smtc.DisplayUpdater.Type = Windows.Media.MediaPlaybackType.Video;
                smtc.DisplayUpdater.VideoProperties.Title = Title ?? "Hohoema";
                if (ThumbnailUrl != null)
                {
                    smtc.DisplayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(this.ThumbnailUrl));
                }
                smtc.IsPlayEnabled = true;
                smtc.IsPauseEnabled = true;
                smtc.DisplayUpdater.Update();

                divided.OnPlayStarted();
            }

            return divided.Quality;
        }



        public void StopPlay()
        {
            HohoemaApp.MediaPlayer.Pause();
            HohoemaApp.MediaPlayer.Source = null;

            _VideoMSS?.Dispose();
            _VideoMSS = null;
            _MediaSource?.Dispose();
            _MediaSource = null;

            foreach (var div in GetAllQuality())
            {
                if (div.NowPlaying)
                {
                    div.OnPlayDone();
                }
            }

            Debug.WriteLine("stream dispose");
            NicoVideoCachedStream?.Dispose();
            NicoVideoCachedStream = null;

            var smtc = HohoemaApp.MediaPlayer.SystemMediaTransportControls;
            smtc.DisplayUpdater.ClearAll();
            smtc.IsEnabled = false;
            smtc.DisplayUpdater.Update();


            Db.VideoPlayHistoryDb.VideoPlayed(this.RawVideoId);
            RaisePropertyChanged(nameof(IsPlayed));
        }

        #endregion

        private async void _NiconicoMediaManager_VideoCacheStateChanged(object sender, NicoVideoCacheRequest request, NicoVideoCacheState state)
        {
            if (this.RawVideoId == request.RawVideoId)
            {
                var divided = GetDividedQualityNicoVideo(request.Quality);

//                divided.CacheState = state;

                Debug.WriteLine($"{request.RawVideoId}<{request.Quality}>: {state.ToString()}");

                // update Cached time
                await divided.GetCacheFile();

                if (state != NicoVideoCacheState.NotCacheRequested)
                {
                    var requestAt = request.RequestAt;
                    foreach (var div in GetAllQuality())
                    {
                        if (div.VideoFileCreatedAt > requestAt)
                        {
                            requestAt = div.VideoFileCreatedAt;
                        }
                    }

                    await HohoemaApp.UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => 
                    {
                        CachedAt = requestAt;
                    });
                }
            }
        }

        public DividedQualityNicoVideo GetDividedQualityNicoVideo(NicoVideoQuality quality)
        {
            DividedQualityNicoVideo qualityDividedVideo = null;

            if (_InterfaceByQuality.ContainsKey(quality))
            {
                qualityDividedVideo = _InterfaceByQuality[quality];
            }
            else 
            {
                switch (quality)
                {
                    case NicoVideoQuality.Original:
                        qualityDividedVideo = new OriginalQualityNicoVideo(this, _NiconicoMediaManager);
                        break;
                    case NicoVideoQuality.Low:
                        qualityDividedVideo = new LowQualityNicoVideo(this, _NiconicoMediaManager);
                        break;
                    case NicoVideoQuality.Dmc_High:
                        qualityDividedVideo = new DmcQualityNicoVideo(quality, this, _NiconicoMediaManager);
                        break;
                    case NicoVideoQuality.Dmc_Midium:
                        qualityDividedVideo = new DmcQualityNicoVideo(quality, this, _NiconicoMediaManager);
                        break;
                    case NicoVideoQuality.Dmc_Low:
                        qualityDividedVideo = new DmcQualityNicoVideo(quality, this, _NiconicoMediaManager);
                        break;
                    case NicoVideoQuality.Dmc_Mobile:
                        qualityDividedVideo = new DmcQualityNicoVideo(quality, this, _NiconicoMediaManager);
                        break;
                    default:
                        throw new NotSupportedException(quality.ToString());
                }

                _InterfaceByQuality.Add(quality, qualityDividedVideo);
                _QualityDividedVideos.Add(qualityDividedVideo);
            }

            return qualityDividedVideo;
        }


        public IEnumerable<DividedQualityNicoVideo> GetAllQuality()
        {
            yield return GetDividedQualityNicoVideo(NicoVideoQuality.Dmc_High);
            yield return GetDividedQualityNicoVideo(NicoVideoQuality.Dmc_Midium);
            yield return GetDividedQualityNicoVideo(NicoVideoQuality.Dmc_Low);
            yield return GetDividedQualityNicoVideo(NicoVideoQuality.Dmc_Mobile);
            yield return GetDividedQualityNicoVideo(NicoVideoQuality.Original);
            yield return GetDividedQualityNicoVideo(NicoVideoQuality.Low);
        }







        private async Task UpdateWithThumbnail()
		{
			if (_thumbnailInitialized) { return; }

			// 動画のサムネイル情報にアクセスさせて、アプリ内部DBを更新
			try
			{
				var res = await HohoemaApp.ContentFinder.GetThumbnailResponse(RawVideoId);

				this.VideoId = res.Id;
				this.VideoLength = res.Length;
				this.SizeLow = (uint)res.SizeLow;
				this.SizeHigh = (uint)res.SizeHigh;
				this.Title = res.Title;
				this.OwnerId = res.UserId;
                this.OwnerName = res.UserName;
				this.ContentType = res.MovieType;
				this.PostedAt = res.PostedAt.DateTime;
				this.Tags = res.Tags.Value.ToList();
				this.ViewCount = res.ViewCount;
				this.MylistCount = res.MylistCount;
				this.CommentCount = res.CommentCount;
				this.ThumbnailUrl = res.ThumbnailUrl.AbsoluteUri;
                this.OwnerIconUrl = res.UserIconUrl.OriginalString;
                this.OwnerUserType = res.UserType;
                this.IsChannel = res.UserType == UserType.Channel;
            }
			catch (Exception e) when (e.Message.Contains("delete"))
			{
                await HohoemaApp.UIDispatcher.RunAsync(CoreDispatcherPriority.Low, async () =>
                {
                    await DeletedTeardown();
                });
			}
			catch (Exception e) when (e.Message.Contains("community"))
			{
				this.IsCommunity = true;
			}

            _thumbnailInitialized = true;

            await HohoemaApp.UIDispatcher.RunAsync(CoreDispatcherPriority.Low, async () => 
            {
                if (!this.IsDeleted && GetAllQuality().Any(x => x.IsCacheRequested))
                {
                    // キャッシュ情報を最新の状態に更新
                    await OnCacheRequested();
                }
            }).AsTask().ConfigureAwait(false);
		}


        private async Task<DmcWatchResponse> GetDmcWatchResponse()
        {
            if (!HohoemaApp.IsLoggedIn) { return null; }

            DmcWatchResponse dmcWatchResponse = null;

            try
            {
                dmcWatchResponse = await HohoemaApp.ContentFinder.GetDmcWatchResponse(RawVideoId, HarmfulContentReactionType);
            }
            catch (AggregateException ea) when (ea.Flatten().InnerExceptions.Any(e => e is ContentZoningException))
            {
                IsBlockedHarmfulVideo = true;
            }
            catch (ContentZoningException)
            {
                IsBlockedHarmfulVideo = true;
            }

            if (dmcWatchResponse != null)
            {
                NowLowQualityOnly = dmcWatchResponse.Video.SmileInfo.IsSlowLine;
            }

            //			_VisitedPageType = watchApiRes.Vide.AbsoluteUri.EndsWith("low") ? NicoVideoQuality.Low : NicoVideoQuality.Original;

            
            if (dmcWatchResponse != null)
            {
                if (dmcWatchResponse.Video.DmcInfo?.Quality == null &&
                    dmcWatchResponse.Video.SmileInfo?.Url == null)
                {
                    throw new Exception("Dmcサーバーからの再生が出来ません。");
                }

                ProtocolType = MediaProtocolType.RTSPoverHTTP;

                if (dmcWatchResponse.Video.SmileInfo.Url != null)
                {
                    LegacyVideoUrl = new Uri(dmcWatchResponse.Video.SmileInfo.Url);
                    ProtocolType = MediaProtocolTypeHelper.ParseMediaProtocolType(LegacyVideoUrl);
                }


                DescriptionWithHtml = dmcWatchResponse.Video.Description;
                ThreadId = dmcWatchResponse.Thread.Ids.Default;
                //PrivateReasonType = watchApiRes.PrivateReason;
                VideoLength = TimeSpan.FromSeconds(dmcWatchResponse.Video.Duration);
                NowLowQualityOnly = dmcWatchResponse.Video.SmileInfo?.IsSlowLine ?? false;
                //IsOriginalQualityOnly = dmcWatchResponse.Video.SmileInfo?.QualityIds?.Count == 1;

                if (!_thumbnailInitialized)
                {
                    RawVideoId = dmcWatchResponse.Video.Id;
                    await UpdateWithThumbnail();
                }
                IsCommunity = dmcWatchResponse.Video.IsCommunityMemberOnly == "1";

                if (CommentClient.CommentServerInfo == null)
                {
                    if (dmcWatchResponse.Video.DmcInfo != null)
                    {
                        var commentServerInfo = new CommentServerInfo()
                        {
                            ServerUrl = dmcWatchResponse.Video.DmcInfo.Thread.ServerUrl,
                            DefaultThreadId = dmcWatchResponse.Video.DmcInfo.Thread.ThreadId,
                            ThreadKeyRequired = dmcWatchResponse.Video.DmcInfo?.Thread.ThreadKeyRequired ?? false
                        };

                        CommentClient.CommentServerInfo = commentServerInfo;
                    }
                    else
                    {
                        var commentServerInfo = new CommentServerInfo()
                        {
                            ServerUrl = dmcWatchResponse.Thread.ServerUrl,
                            DefaultThreadId = int.Parse(dmcWatchResponse.Thread.Ids.Default),
                            ThreadKeyRequired = dmcWatchResponse.Video.IsOfficial
                        };

                        if (int.TryParse((string)dmcWatchResponse.Thread.Ids.Community, out var comThreadId))
                        {
                            commentServerInfo.CommunityThreadId = comThreadId;
                        }

                        CommentClient.CommentServerInfo = commentServerInfo;
                    }
                }

                CommentClient.LastAccessDmcWatchResponse = dmcWatchResponse;

                foreach (var divided in GetAllQuality())
                {
                    if (divided is DmcQualityNicoVideo)
                    {
                        (divided as DmcQualityNicoVideo).DmcWatchResponse = dmcWatchResponse;
                    }
                }

                // TODO: 
                //				Tags = watchApiRes.videoDetail.tagList.Select(x => new Tag()
                //				{

                //				}).ToList();

                if (dmcWatchResponse.Owner != null)
                {
                    OwnerId = uint.Parse(dmcWatchResponse.Owner.Id);
                    OwnerIconUrl = dmcWatchResponse.Owner.IconURL;
                    this.OwnerName = dmcWatchResponse.Owner.Nickname;
                }


                this.IsDeleted = dmcWatchResponse.Video.IsDeleted;
                if (IsDeleted)
                {
                    await DeletedTeardown();
                }
            }

            return dmcWatchResponse;
        }

        private async Task<WatchApiResponse> GetWatchApiResponse(bool forceLoqQuality = false)
        {
            if (!HohoemaApp.IsLoggedIn) { return null; }


            WatchApiResponse watchApiRes = null;

            try
            {
                watchApiRes = await HohoemaApp.ContentFinder.GetWatchApiResponse(RawVideoId, forceLoqQuality, HarmfulContentReactionType);
            }
            catch (AggregateException ea) when (ea.Flatten().InnerExceptions.Any(e => e is ContentZoningException))
            {
                IsBlockedHarmfulVideo = true;
            }
            catch (ContentZoningException)
            {
                IsBlockedHarmfulVideo = true;
            }
            catch { }

            if (!forceLoqQuality && watchApiRes != null)
            {
                NowLowQualityOnly = watchApiRes.VideoUrl.AbsoluteUri.EndsWith("low");
            }

            _VisitedPageType = watchApiRes.VideoUrl.AbsoluteUri.EndsWith("low") ? NicoVideoQuality.Low : NicoVideoQuality.Original;

            if (watchApiRes != null)
            {
                LegacyVideoUrl = watchApiRes.VideoUrl;

                ProtocolType = MediaProtocolTypeHelper.ParseMediaProtocolType(watchApiRes.VideoUrl);

                DescriptionWithHtml = watchApiRes.videoDetail.description;
                ThreadId = watchApiRes.ThreadId.ToString();
                PrivateReasonType = watchApiRes.PrivateReason;
                VideoLength = watchApiRes.Length;

                if (!_thumbnailInitialized)
                {
                    RawVideoId = watchApiRes.videoDetail.id;
                    await UpdateWithThumbnail();
                }
                IsCommunity = watchApiRes.flashvars.is_community_video == "1";

                var commentServerInfo = new CommentServerInfo()
                {
                    ServerUrl = watchApiRes.CommentServerUrl.OriginalString,
                    DefaultThreadId = (int)watchApiRes.ThreadId,
                    ThreadKeyRequired = watchApiRes.IsKeyRequired
                };

                CommentClient.CommentServerInfo = commentServerInfo;

                // TODO: 
                //				Tags = watchApiRes.videoDetail.tagList.Select(x => new Tag()
                //				{

                //				}).ToList();

                if (watchApiRes.UploaderInfo != null)
                {
                    OwnerId = uint.Parse(watchApiRes.UploaderInfo.id);
                }


                this.IsDeleted = watchApiRes.IsDeleted;
                if (IsDeleted)
                {
                    await DeletedTeardown();
                }
            }

            return watchApiRes;
        }
        
        // 動画情報のキャッシュまたはオンラインからの取得と更新

        public async Task VisitWatchPage(NicoVideoQuality quality)
		{
            try
            {
                if (quality == NicoVideoQuality.Low)
                {
                    await GetWatchApiResponse(true);
                }
                else
                {
                    await GetDmcWatchResponse();
                }
            }
            catch
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                await GetWatchApiResponse(quality == NicoVideoQuality.Low);
            }
        }


        internal async Task<Uri> GetVideoStreamUriAsync(NicoVideoQuality quality)
        {
            await VisitWatchPage(quality);

            var divided = GetDividedQualityNicoVideo(quality);
            if (divided.CanPlay)
            {                
                var videoUri = await divided.GenerateVideoContentUrl();

                if (divided.IsCacheRequested)
                {
                    await OnCacheRequested();
                }

                return videoUri;
            }
            else
            {
                return null;
            }

            /*
            if (ContentType == MovieType.Mp4)
            {
                var res = await GetDmcWatchResponse();
                
            }
            else
            {
                WatchApiResponse res;
                if (quality == NicoVideoQuality.Original)
                {
                    res = await GetWatchApiResponse();
                    
                    // ないです				
                    if (_VisitedPageType == NicoVideoQuality.Low)
                    {
                        throw new Exception("cant download original quality video.");
                    }
                }
                else
                {
                    // 低画質動画キャッシュ済みの場合
                    if (LowQuality.IsCached)
                    {
                        res = await GetWatchApiResponse();
                    }
                    // 低画質の視聴ページへアクセスしたWatchApiResponseを取得する
                    else if (CachedWatchApiResponse != null
                        && _VisitedPageType == NicoVideoQuality.Low
                        )
                    {
                        // すでに
                        res = await GetWatchApiResponse();
                    }
                    else
                    {
                        // まだなので、低画質を指定してアクセスする
                        res = await GetWatchApiResponse(forceLoqQuality: true);
                    }
                }

                if (res == null)
                {
                    throw new Exception("");
                }
            }
            */

            // キャッシュリクエストされている場合このタイミングでコメントを取得

        }




        public bool CanGetVideoStream()
		{
			return ProtocolType == MediaProtocolType.RTSPoverHTTP;
		}

		

        public async Task RestoreCache(NicoVideoQuality quality, string filepath)
        {
            var divided = GetDividedQualityNicoVideo(quality);

            await FillVideoInfoFromDb();

            await divided.RestoreCache(filepath);

            await divided.RequestCache();

            if (divided.VideoFileCreatedAt > this.CachedAt)
            {
                this.CachedAt = divided.VideoFileCreatedAt;
            }
        }

        public Task RequestCache(NicoVideoQuality? quality = null)
		{
            if (!quality.HasValue)
            {
//                quality = HohoemaApp.UserSettings.PlayerSettings.DefaultQuality;

                foreach (var div in GetAllQuality().Where(x => x.Quality.IsLegacy()))
                {
                    if (div.CanDownload)
                    {
                        quality = div.Quality;
                        break;
                    }
                }
            }

            // Dmcサーバーからのキャッシュ化は未対応
            if (quality.Value.IsDmc()) { return Task.CompletedTask; }

            var divided = GetDividedQualityNicoVideo(quality.Value);

            _NiconicoMediaManager.VideoCacheStateChanged -= _NiconicoMediaManager_VideoCacheStateChanged;
            _NiconicoMediaManager.VideoCacheStateChanged += _NiconicoMediaManager_VideoCacheStateChanged;

            return divided.RequestCache();
		}


		public async Task CancelCacheRequest(NicoVideoQuality? quality = null)
		{
            if (quality.HasValue)
            {
                var divided = GetDividedQualityNicoVideo(quality.Value);
                await divided.DeleteCache();
            }
            else
            {
                foreach (var divided in GetAllQuality())
                {
                    await divided.DeleteCache();
                }
            }

            // 全てのキャッシュリクエストが取り消されていた場合
            // 動画情報とコメント情報をDBから削除する
			if (GetAllQuality().All(x => !x.IsCacheRequested))
			{
				var info = await VideoInfoDb.GetEnsureNicoVideoInfoAsync(RawVideoId);
                if (!info.IsDeleted)
                {
                    await VideoInfoDb.RemoveAsync(info);
                }

                CommentDb.Remove(RawVideoId);

                _NiconicoMediaManager.VideoCacheStateChanged -= _NiconicoMediaManager_VideoCacheStateChanged;
            }
        }





		

		/// <summary>
		/// キャッシュ要求の基本処理を実行します
		/// DividedQualityNicoVideoから呼び出されます
		/// </summary>
		/// <returns></returns>
		internal async Task OnCacheRequested()
		{
			IfVideoDeletedThrowException();

            if (CommentClient != null)
            {
                await CommentClient.GetComments();
            }


            var info = await VideoInfoDb.GetEnsureNicoVideoInfoAsync(RawVideoId);
            
            info.VideoId = this.VideoId;
			info.Length = this.VideoLength;
			info.LowSize = (uint)this.SizeLow;
			info.HighSize = (uint)this.SizeHigh;
			info.Title = this.Title;
			info.UserId = this.OwnerId;
			info.MovieType = this.ContentType;
			info.PostedAt = this.PostedAt;
			info.SetTags(this.Tags);
			info.ViewCount = this.ViewCount;
			info.MylistCount = this.MylistCount;
			info.CommentCount = this.CommentCount;
			info.ThumbnailUrl = this.ThumbnailUrl;
            
            await VideoInfoDb.UpdateAsync(info);
		}



		/// <summary>
		/// 動画削除済みの場合の処理
		/// </summary>
		private async Task DeletedTeardown()
		{
            this.IsDeleted = true;

            if (this.GetAllQuality().Any(x => x.IsCacheRequested))
            {
                await _NiconicoMediaManager.NotifyCacheForceDeleted(this);
            }

            await FillVideoInfoFromDb();

            // キャッシュした動画データと動画コメントの削除
            await CancelCacheRequest();
        }


        private async Task FillVideoInfoFromDb()
        {
            var videoInfo = await VideoInfoDb.GetAsync(RawVideoId);

            if (videoInfo == null) { return; }

            //this.RawVideoId = videoInfo.RawVideoId;
            this.VideoId = videoInfo.VideoId;
            this.VideoLength = videoInfo.Length;
            this.SizeLow = (uint)videoInfo.LowSize;
            this.SizeHigh = (uint)videoInfo.HighSize;
            this.Title = videoInfo.Title;
            this.OwnerId = videoInfo.UserId;
            this.ContentType = videoInfo.MovieType;
            this.PostedAt = videoInfo.PostedAt;
            this.Tags = videoInfo.GetTags();
            this.ViewCount = videoInfo.ViewCount;
            this.MylistCount = videoInfo.MylistCount;
            this.CommentCount = videoInfo.CommentCount;
            this.ThumbnailUrl = videoInfo.ThumbnailUrl;
            this.DescriptionWithHtml = videoInfo.DescriptionWithHtml;

        }







        private void IfVideoDeletedThrowException()
		{
			if (IsDeleted) { throw new Exception("video is deleted"); }
		}


		public NGResult CheckNGVideoOwner()
		{
			return HohoemaApp.UserSettings?.NGSettings.IsNgVideoOwnerId(this.OwnerId.ToString());
		}

        public NGResult CheckNGVideoTitle()
        {
            return HohoemaApp.UserSettings?.NGSettings.IsNGVideoTitle(this.Title);
        }

        public NGResult CheckNGVideo()
        {
            return HohoemaApp.UserSettings?.NGSettings.IsNgVideo(this);
        }


        // Initializeが呼ばれるまで有効
        public void PreSetTitle(string title)
		{
			if (_IsInitialized) { return; }

			Title = title;
		}

		public void PreSetPostAt(DateTime dateTime)
		{
			if (_IsInitialized) { return; }

			PostedAt = dateTime;
		}

		public void PreSetVideoLength(TimeSpan length)
		{
			if (_IsInitialized) { return; }

			VideoLength = length;
		}

		public void PreSetCommentCount(uint count)
		{
			if (_IsInitialized) { return; }

			CommentCount = count;
		}
		public void PreSetViewCount(uint count)
		{
			if (_IsInitialized) { return; }

			ViewCount = count;
		}
		public void PreSetMylistCount(uint count)
		{
			if (_IsInitialized) { return; }

			MylistCount = count;
		}

		public void PreSetThumbnailUrl(string thumbnailUrl)
		{
			if (_IsInitialized) { return; }

			ThumbnailUrl = thumbnailUrl;
		}


		public string RawVideoId { get; private set; }
		public string VideoId { get; private set; }

		public bool IsDeleted { get; private set; }
        public bool IsChannel { get; private set; }
		public bool IsCommunity { get; private set; }

        public bool IsDmc => ContentType == MovieType.Mp4;

        public MovieType ContentType { get; private set; }
		public string Title { get; private set; }
		public TimeSpan VideoLength { get; private set; }
		public DateTime PostedAt { get; private set; }
		public uint OwnerId { get; private set; }
        public string OwnerName { get; private set; }
        public bool IsOriginalQualityOnly => SizeLow == 0;
		public List<ThumbnailTag> Tags { get; private set; }
		public uint SizeLow { get; private set; }
		public uint SizeHigh { get; private set; }
		public uint ViewCount { get; internal set; }
		public uint CommentCount { get; internal set; }
		public uint MylistCount { get; internal set; }
		public string ThumbnailUrl { get; internal set; }

        public UserType OwnerUserType { get; private set; }
        public string OwnerIconUrl { get; private set; }
        public bool IsPlayed => VideoPlayHistoryDb.Get(this.RawVideoId).PlayCount > 0;

		public Uri LegacyVideoUrl { get; private set; }
		public string ThreadId { get; private set; }
		public string DescriptionWithHtml { get; private set; }
		public bool NowLowQualityOnly { get; private set; }
		public MediaProtocolType ProtocolType { get; private set; }
		public PrivateReasonType PrivateReasonType { get; private set; }

        private DateTime _CachedAt;
        public DateTime CachedAt
        {
            get { return _CachedAt; }
            set { SetProperty(ref _CachedAt, value); }
        }


		public bool IsNeedPayment { get; private set; }


		public bool IsRequireConfirmDelete { get; private set; }

		public HohoemaApp HohoemaApp { get; private set; }
        NiconicoMediaManager _NiconicoMediaManager;

		public bool LastAccessIsLowQuality { get; private set; }


		// 有害動画への対応
		public bool IsBlockedHarmfulVideo { get; private set; }

		public HarmfulContentReactionType HarmfulContentReactionType { get; set; }
		
		public IRandomAccessStream NicoVideoCachedStream { get; private set; }


		private Dictionary<NicoVideoQuality, DividedQualityNicoVideo> _InterfaceByQuality;


        private ObservableCollection<DividedQualityNicoVideo> _QualityDividedVideos = new ObservableCollection<DividedQualityNicoVideo>();
        public ReadOnlyObservableCollection<DividedQualityNicoVideo> QualityDividedVideos { get; private set; }


        private DividedQualityNicoVideo _OriginalQuality;
		public DividedQualityNicoVideo OriginalQuality
		{
			get
			{
				return _OriginalQuality ??
					(_OriginalQuality = GetDividedQualityNicoVideo(NicoVideoQuality.Original));
			}
		}


		private DividedQualityNicoVideo _LowQuality;
		public DividedQualityNicoVideo LowQuality
		{
			get
			{
				return _LowQuality ??
					(_LowQuality = GetDividedQualityNicoVideo(NicoVideoQuality.Low));
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
        public HohoemaApp HohoemaApp { get; }
        public CommentServerInfo CommentServerInfo { get; set; }

        private CommentResponse CachedCommentResponse { get; set; }

        internal DmcWatchResponse LastAccessDmcWatchResponse { get; set; }

        private CommentSubmitInfo SubmitInfo { get; set; }


        public CommentClient(HohoemaApp hohoemaApp, string rawVideoid)
        {
            RawVideoId = rawVideoid;
            HohoemaApp = hohoemaApp;
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
                    return await this.HohoemaApp.NiconicoContext.Video
                        .GetCommentAsync(
                            (int)HohoemaApp.LoginUserId,
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
                            return await this.HohoemaApp.NiconicoContext.Video
                                .GetCommentAsync(
                                    (int)HohoemaApp.LoginUserId,
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
                SubmitInfo.CommentCount = int.Parse(commentRes.Thread.CommentCount) + 1;
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

            var res = await HohoemaApp.NiconicoContext.Video.GetNMSGCommentAsync(LastAccessDmcWatchResponse);

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
                    response = await HohoemaApp.NiconicoContext.Video.PostCommentAsync(
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
                    var videoInfo = await HohoemaApp.NiconicoContext.Search.GetVideoInfoAsync(RawVideoId);
                    SubmitInfo.CommentCount = int.Parse(videoInfo.Thread.num_res);
                    Debug.WriteLine("コメ数再取得: " + SubmitInfo.CommentCount);
                }
                catch
                {
                }
            }

            return response;
        }

    }
}
