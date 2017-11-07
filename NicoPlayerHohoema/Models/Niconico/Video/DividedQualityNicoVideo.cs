using Mntone.Nico2.Videos.Dmc;
using NicoPlayerHohoema.Helpers;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;

namespace NicoPlayerHohoema.Models
{
    public abstract class DividedQualityNicoVideo : BindableBase
    {
        public static string MakeVideoFileName(string title, string videoid)
        {
            return $"{title.ToSafeFilePath()} - [{videoid}]";
        }

        // Note: ThumbnailResponseが初期化されていないと利用できない
        private static readonly AsyncLock _Lock = new AsyncLock();


        public NicoVideoQuality Quality { get; private set; }
        public NicoVideo NicoVideo { get; private set; }
        public VideoCacheManager NiconicoMediaManager { get; private set; }
        public VideoDownloadManager VideoDownloadManager { get; private set; }
        public HohoemaApp HohoemaApp { get; private set; }
                

        public string RawVideoId
		{
			get
			{
				return NicoVideo.RawVideoId;
			}
		}


		private NicoVideoCacheState _CacheState;
		public NicoVideoCacheState CacheState
		{
			get { return _CacheState; }
			internal set { SetProperty(ref _CacheState, value); }
		}

        private uint _CacheTotalSize;
        public uint CacheTotalSize
        {
            get { return _CacheTotalSize; }
            internal set { SetProperty(ref _CacheTotalSize, value); }
        }

        private uint _CacheProgressSize;
        public uint CacheProgressSize
        {
            get { return _CacheProgressSize; }
            internal set { SetProperty(ref _CacheProgressSize, value); }
        }


        public bool IsReadyOfflinePlay { get; private set; }


	
		public DividedQualityNicoVideo(NicoVideoQuality quality)
		{
			Quality = quality;
			NicoVideo = nicoVideo;
            HohoemaApp = nicoVideo.HohoemaApp;
            NiconicoMediaManager = mediaManager;
            VideoDownloadManager = mediaManager.VideoDownloadManager;
		}



		public abstract string VideoFileName { get; }


		public abstract bool CanDownload { get; }
		
		public DateTime VideoFileCreatedAt { get; private set; }

        public string CacheFilePath { get; internal set; }


        public bool CanPlay
		{
			get
			{
				return CacheState == NicoVideoCacheState.Cached 
					|| CanDownload;
			}
		}

		public bool CanRequestCache
		{
			get
			{
				// ダウンロード可能か
				if (!CanDownload)
				{
					return false;
				}

				// キャッシュが有効か
				if (!NicoVideo.HohoemaApp.UserSettings.CacheSettings.IsEnableCache)
				{
					return false;
				}

				return true;
			}
		}

        public bool IsNotCacheRequested => !IsCacheRequested || CacheState == NicoVideoCacheState.NotCacheRequested;
        public bool NowCachePending => IsCacheRequested && CacheState == NicoVideoCacheState.Pending;
        public bool NowCacheDonwloading => IsCacheRequested && CacheState == NicoVideoCacheState.Downloading;
        public bool IsCached => IsCacheRequested && CacheState == NicoVideoCacheState.Cached;

        public bool HasCache => IsCached || NowCacheDonwloading;



        private bool _IsCacheRequested;
		public bool IsCacheRequested
		{
			get { return _IsCacheRequested; }
			internal set { SetProperty(ref _IsCacheRequested, value); }
		}



		public async Task<bool> ExistVideo()
		{
			var cacheFolder = await NicoVideo.HohoemaApp.GetVideoCacheFolder();
			if (cacheFolder != null)
			{
				return await cacheFolder.ExistFile(VideoFileName);
			}
			else
			{
				return false;
			}
		}


        internal async Task RestoreCache(string path)
        {
            CacheFilePath = path;
            IsCacheRequested = true;
            CacheState = NicoVideoCacheState.Cached;


            var file = await GetCacheFile();
            if (file != null)
            {
                var prop = await file.GetBasicPropertiesAsync();
                VideoFileCreatedAt = file.DateCreated.DateTime;
                /*
                if (VideoSize != 0 && (uint)prop.Size != VideoSize)
                {
                    await RequestCache(forceUpdate: true);
                    Debug.WriteLine($"{RawVideoId}<{Quality}> のキャッシュがキャッシュサイズと異なっているため、キャッシュを削除して再ダウンロード");
                }
                */
            }
        }


        internal Task RestoreDownload(DownloadOperation operation)
        {
            VideoDownloadManager.DownloadStarted += VideoDownloadManager_DownloadStarted;
            IsCacheRequested = true;
            CacheState = NicoVideoCacheState.Downloading;

            return GetCacheFile();
        }




        public async Task DeleteCache()
		{
			Debug.Write($"{NicoVideo.Title}:{Quality.ToString()}のキャッシュを削除開始...");

			await DeleteCacheFile();

			Debug.WriteLine($".完了");
		}


        public async Task RestoreRequestCache(NicoVideoCacheRequest req)
        {
            VideoFileCreatedAt = req.RequestAt;

            await RequestCache();
        }

        public async Task RequestCache(bool forceUpdate = false)
		{
            if (!CanDownload) { return; }

            var isCacheFileExist = false;
            using (var releaser = await _Lock.LockAsync())
            {
                isCacheFileExist = !forceUpdate && (IsCached || NowCacheDonwloading);
            }

            if (isCacheFileExist)
            {
                // update cached time
                await GetCacheFile();
                return;
            }

            using (var releaser = await _Lock.LockAsync())
            {
                VideoDownloadManager.DownloadStarted += VideoDownloadManager_DownloadStarted;
                IsCacheRequested = true;
                CacheState = NicoVideoCacheState.Pending;

                VideoFileCreatedAt = DateTime.Now;

                await VideoDownloadManager.AddCacheRequest(NicoVideo.RawVideoId, Quality, forceUpdate);

                await NicoVideo.OnCacheRequested();
            }
		}

        private async void VideoDownloadManager_DownloadStarted(object sender, NicoVideoCacheRequest request, DownloadOperation op)
        {
            using (var releaser = await _Lock.LockAsync())
            {
                VideoDownloadManager.DownloadStarted -= VideoDownloadManager_DownloadStarted;

                if (request.RawVideoId == this.RawVideoId && request.Quality == this.Quality)
                {
                    CacheState = NicoVideoCacheState.Downloading;
                    CacheTotalSize = (uint)op.Progress.TotalBytesToReceive;
                    VideoDownloadManager.DownloadProgress += VideoDownloadManager_DownloadProgress;
                    VideoDownloadManager.DownloadCanceled += VideoDownloadManager_DownloadCanceled;
                    VideoDownloadManager.DownloadCompleted += VideoDownloadManager_DownloadCompleted;
                }
            }
        }

        private async void VideoDownloadManager_DownloadCompleted(object sender, NicoVideoCacheRequest request, string filePath)
        {
            if (request.RawVideoId == this.RawVideoId && request.Quality == this.Quality)
            {
                await RestoreCache(filePath).ConfigureAwait(false);

                using (var releaser = await _Lock.LockAsync())
                {
                    VideoDownloadManager.DownloadProgress -= VideoDownloadManager_DownloadProgress;
                    VideoDownloadManager.DownloadCanceled -= VideoDownloadManager_DownloadCanceled;
                    VideoDownloadManager.DownloadCompleted -= VideoDownloadManager_DownloadCompleted;
                }
            }
        }

        private async void VideoDownloadManager_DownloadCanceled(object sender, NicoVideoCacheRequest request)
        {
            if (request.RawVideoId == this.RawVideoId && request.Quality == this.Quality)
            {
                using (var releaser = await _Lock.LockAsync())
                {
                    if (CacheState == NicoVideoCacheState.Downloading)
                    {
                        CacheState = NicoVideoCacheState.Pending;
                    }

                    VideoDownloadManager.DownloadProgress -= VideoDownloadManager_DownloadProgress;
                    VideoDownloadManager.DownloadCanceled -= VideoDownloadManager_DownloadCanceled;
                    VideoDownloadManager.DownloadCompleted -= VideoDownloadManager_DownloadCompleted;
                }
            }
        }

        private async void VideoDownloadManager_DownloadProgress(object sender, NicoVideoCacheRequest request, DownloadOperation op)
        {
            if (request.RawVideoId == this.RawVideoId && request.Quality == this.Quality)
            {
                using (var releaser = await _Lock.LockAsync())
                {
                    CacheState = NicoVideoCacheState.Downloading;
                    CacheTotalSize = (uint)op.Progress.TotalBytesToReceive;
                    CacheProgressSize = (uint)op.Progress.BytesReceived;
                }
            }
        }

        protected async Task<bool> DeleteCacheFile()
        {
            using (var releaser = await _Lock.LockAsync())
            {
                if (NowPlaying && IsCached)
                {
                    _DeleteRequestedOnPlaying = true;
                    return false;
                }

            }

            var result = await VideoDownloadManager.RemoveCacheRequest(NicoVideo.RawVideoId, Quality);

            var file = await GetCacheFile();
            if (file != null)
            {
                await file.DeleteAsync();
            }

            using (var releaser = await _Lock.LockAsync())
            {
                CacheState = NicoVideoCacheState.NotCacheRequested;
                CacheFilePath = null;
                VideoFileCreatedAt = default(DateTime);
                IsCacheRequested = false;

                return result;
            }            
        }

		protected string VideoFileNameBase
		{
			get
			{
				return $"{MakeVideoFileName(NicoVideo.Title, NicoVideo.RawVideoId)}";
			}
		}

		public async Task<StorageFile> GetCacheFile()
		{
            using (var releaser = await _Lock.LockAsync())
            {

                if (IsCached)
                {
                    if (string.IsNullOrEmpty(CacheFilePath))
                    {
                        throw new Exception();
                    }

                    var file = await StorageFile.GetFileFromPathAsync(CacheFilePath);
                    VideoFileCreatedAt = file.DateCreated.DateTime;

                    return file;

                }
                else if (NowCacheDonwloading)
                {
                    var downloadOp = await VideoDownloadManager.GetDownloadingVideoOperation(this.RawVideoId, this.Quality);
                    if (downloadOp != null)
                    {
                        VideoFileCreatedAt = downloadOp.ResultFile.DateCreated.DateTime;
                        return downloadOp.ResultFile as StorageFile;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
        }


        public abstract Task<Uri> GenerateVideoContentUrl();
        public float GetDonwloadProgressParcentage()
        {
            var currentBytes = this.CacheProgressSize;
            var total = this.CacheTotalSize;

            if (total == 0) { return 0.0f; }

            var parcent = (float)Math.Round(((double)currentBytes / total) * 100.0, 1);

            return parcent;
        }
    
        public bool NowPlaying { get; private set; }

        private bool _DeleteRequestedOnPlaying = false;

        internal void OnPlayStarted()
        {
            NowPlaying = true;
            OnPlayStarted_Internal();
        }

        internal void OnPlayDone()
        {
            if (NowPlaying)
            {
                NowPlaying = false;

                if (_DeleteRequestedOnPlaying)
                {
                    DeleteCache().ConfigureAwait(false);
                    _DeleteRequestedOnPlaying = false;
                }

                OnPlayDone_Internal();
            }
        }

        protected virtual void OnPlayStarted_Internal() { }
        protected virtual void OnPlayDone_Internal() { }

    }


    public class LowQualityNicoVideo : DividedQualityNicoVideo
	{
		public LowQualityNicoVideo(NicoVideo nicoVideo, VideoCacheManager context) 
			: base(NicoVideoQuality.Smile_Low, nicoVideo, context)
		{
		}




		public override string VideoFileName
		{
			get
			{
				return VideoFileNameBase + ".low.mp4";
			}
		}
      


		public override bool CanDownload
		{
			get
			{
				// インターネット繋がってるか
				if (!Helpers.InternetConnection.IsInternet()) { return false; }

				// キャッシュ済みじゃないか
				if (CacheState == NicoVideoCacheState.Cached) { return false; }

				// オリジナル画質しか存在しない動画
				if (NicoVideo.IsOriginalQualityOnly)
				{
					return false;
				}

				return true;
			}
		}





        public override Task<Uri> GenerateVideoContentUrl()
        {
            return Task.FromResult(NicoVideo.LegacyVideoUrl);
        }







    }

	


    public class OriginalQualityNicoVideo : DividedQualityNicoVideo
    {
        public OriginalQualityNicoVideo(NicoVideo nicoVideo, VideoCacheManager context)
            : base(NicoVideoQuality.Smile_Original, nicoVideo, context)
        {
        }

        public override string VideoFileName
        {
            get
            {
                return VideoFileNameBase + ".mp4";
            }
        }

        public override bool CanDownload
        {
            get
            {
                // インターネット繋がってるか
                if (!Helpers.InternetConnection.IsInternet()) { return false; }

                // キャッシュ済みじゃないか
                if (CacheState == NicoVideoCacheState.Cached) { return false; }

                if (!NicoVideo.IsOriginalQualityOnly)
                {
                    if (NicoVideo.LegacyVideoUrl == null)
                    {
                        return false;
                    }

                    if (NicoVideo.LegacyVideoUrl.OriginalString.EndsWith("low"))
                    {
                        return false;
                    }
                }

                // オリジナル画質DL可能時間帯か
                if (NicoVideo.NowLowQualityOnly)
                {
                    return false;
                }

                return true;
            }
        }

        public override Task<Uri> GenerateVideoContentUrl()
        {
            return Task.FromResult(NicoVideo.LegacyVideoUrl);
        }


    }

    public class DmcQualityNicoVideo : DividedQualityNicoVideo
    {
        private Timer _DmcSessionHeartbeatTimer;

        private static AsyncLock DmcSessionHeartbeatLock = new AsyncLock();

        private int _HeartbeatCount = 0;
        private bool IsFirstHeartbeat => _HeartbeatCount == 0;

        public VideoContent Video
        {
            get
            {
                if (_DmcWatchResponse?.Video.DmcInfo?.Quality.Videos == null)
                {
                    return null;
                }

                var videos = _DmcWatchResponse.Video.DmcInfo.Quality.Videos;

                int qulity_position = 0;
                switch (Quality)
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

                var pos = videos.Count - qulity_position;
                if (videos.Count >= qulity_position)
                {
                    return (videos.ElementAtOrDefault(pos) ?? null);
                }
                else
                {
                    return null;
                }

            }
        }

        DmcWatchData _DmcWatchData;
        static DmcSessionResponse _DmcSessionResponse;
        public DmcWatchData DmcWatchData
        {
            get { return _DmcWatchData; }
            set
            {
                if (_DmcWatchData == null)
                {
                    _DmcWatchData = value;
                    _DmcWatchResponse = _DmcWatchData.DmcWatchResponse;
                    _IsAvailable = Video?.Available ?? false;
                }
            }
        }

        DmcWatchResponse _DmcWatchResponse;
        public DmcWatchResponse DmcWatchResponse => _DmcWatchResponse;
        public DmcWatchEnvironment DmcWatchEnvironment => _DmcWatchData.DmcWatchEnvironment;

        public DmcQualityNicoVideo(NicoVideoQuality quality, NicoVideo nicoVideo, VideoCacheManager context)
            : base(quality, nicoVideo, context)
        {
        }

        public override string VideoFileName
        {
            get
            {
                return VideoFileNameBase + "..mp4";
            }
        }

        private bool _IsAvailable = false;
        public bool IsAvailable
        {
            get
            {
                
                return _IsAvailable;
            }
        }


        public override bool CanDownload
        {
            get
            {
                // インターネット繋がってるか
                if (!Helpers.InternetConnection.IsInternet()) { return false; }

                // キャッシュ済みじゃないか
                if (CacheState == NicoVideoCacheState.Cached) { return false; }

                if (!IsAvailable) { return false; }

                return true;
            }
        }

        public override async Task<Uri> GenerateVideoContentUrl()
        {
            if (DmcWatchResponse == null) { return null; }

            if (DmcWatchResponse.Video.DmcInfo == null) { return null; }

            VideoContent videoQuality = Video;
            if (Video == null)
            {
                return null;
            }
            
            try
            {
                // 直前に同一動画を見ていた場合には、動画ページに再アクセスする
                DmcSessionResponse clearPreviousSession = null;
                if (_DmcSessionResponse != null && DmcWatchEnvironment != null)
                {
                    if (_DmcSessionResponse.Data.Session.RecipeId.EndsWith(RawVideoId))
                    {
                        clearPreviousSession = _DmcSessionResponse;
                        _DmcSessionResponse = null;
                        _DmcWatchResponse = await HohoemaApp.NiconicoContext.Video.GetDmcWatchJsonAsync(RawVideoId, DmcWatchEnvironment.PlaylistToken);
                        videoQuality = Video;
                    }
                }

                _DmcSessionResponse = await HohoemaApp.NiconicoContext.Video.GetDmcSessionResponse(DmcWatchResponse, videoQuality);

                if (_DmcSessionResponse == null) { return null; }

                if (clearPreviousSession != null)
                {
                    await HohoemaApp.NiconicoContext.Video.DmcSessionExitHeartbeatAsync(DmcWatchResponse, clearPreviousSession);
                }

                return new Uri(_DmcSessionResponse.Data.Session.ContentUri);
            }
            catch
            {
                return null;
            }
        }

        protected override void OnPlayStarted_Internal()
        {
            if (DmcWatchResponse != null && _DmcSessionResponse != null)
            {
                Debug.WriteLine($"{DmcWatchResponse.Video.Title} のハートビートを開始しました");
                _DmcSessionHeartbeatTimer = new Timer(async (_) =>
                {
                    using (var releaser = await DmcSessionHeartbeatLock.LockAsync())
                    {
                        Debug.WriteLine($"{DmcWatchResponse.Video.Title} のハートビート {_HeartbeatCount+1}回目");

                        if (IsFirstHeartbeat)
                        {
                            await HohoemaApp.NiconicoContext.Video.DmcSessionFirstHeartbeatAsync(DmcWatchResponse, _DmcSessionResponse);
                            Debug.WriteLine($"{DmcWatchResponse.Video.Title} の初回ハートビート実行");
                            await Task.Delay(2);
                        }

                        await HohoemaApp.NiconicoContext.Video.DmcSessionHeartbeatAsync(DmcWatchResponse, _DmcSessionResponse);
                        Debug.WriteLine($"{DmcWatchResponse.Video.Title} のハートビート実行");

                        _HeartbeatCount++;
                    }
                }
            , null
            , TimeSpan.FromSeconds(5)
            , TimeSpan.FromSeconds(30)
            );
            }
            
        }

        protected override async void OnPlayDone_Internal()
        {
            _DmcSessionHeartbeatTimer.Dispose();
            _DmcSessionHeartbeatTimer = null;
            Debug.WriteLine($"{DmcWatchResponse.Video.Title} のハートビートを終了しました");

            if (_DmcSessionResponse != null)
            {
                await HohoemaApp.NiconicoContext.Video.DmcSessionLeaveAsync(DmcWatchResponse, _DmcSessionResponse);
            }
        }
    }
}