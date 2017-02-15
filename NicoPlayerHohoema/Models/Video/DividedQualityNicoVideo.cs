using NicoPlayerHohoema.Util;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;

namespace NicoPlayerHohoema.Models
{
    public abstract class DividedQualityNicoVideo : BindableBase
    {
        // Note: ThumbnailResponseが初期化されていないと利用できない

        public NicoVideoQuality Quality { get; private set; }
        public NicoVideo NicoVideo { get; private set; }
        public NiconicoMediaManager NiconicoMediaManager { get; private set; }
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


        private uint _CacheProgressSize;
        public uint CacheProgressSize
        {
            get { return _CacheProgressSize; }
            internal set { SetProperty(ref _CacheProgressSize, value); }
        }


        public bool IsReadyOfflinePlay { get; private set; }


		
		/// <summary>
		/// このクオリティは利用可能か
		/// (オリジナル画質しか存在しない動画の場合、true)
		/// </summary>
		public abstract bool IsAvailable { get; }

		public DividedQualityNicoVideo(NicoVideoQuality quality, NicoVideo nicoVideo, NiconicoMediaManager mediaManager)
		{
			Quality = quality;
			NicoVideo = nicoVideo;
            HohoemaApp = nicoVideo.HohoemaApp;
            NiconicoMediaManager = mediaManager;
            VideoDownloadManager = mediaManager.VideoDownloadManager;
		}

		


		public abstract string VideoFileName { get; }

		public abstract string ProgressFileName { get; }


		public abstract bool CanDownload { get; }

		public abstract uint VideoSize { get; }
		
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

            if (VideoSize != 0)
            {
                var file = await GetCacheFile();
                var prop = await file.GetBasicPropertiesAsync();
                if ((uint)prop.Size != VideoSize)
                {
                    await RequestCache(forceUpdate: true);
                    Debug.WriteLine($"{RawVideoId}<{Quality}> のキャッシュがキャッシュサイズと異なっているため、キャッシュを削除して再ダウンロード");
                }
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
			if (!IsAvailable) { return; }

            
			if (!forceUpdate &&(IsCached || NowCacheDonwloading))
			{
                // update cached time
                await GetCacheFile();
				return;
			}

            VideoDownloadManager.DownloadStarted += VideoDownloadManager_DownloadStarted;
            IsCacheRequested = true;
            CacheState = NicoVideoCacheState.Pending;

            VideoFileCreatedAt = DateTime.Now;

            await VideoDownloadManager.AddCacheRequest(NicoVideo.RawVideoId, Quality, forceUpdate);

			await NicoVideo.OnCacheRequested();
		}

        private void VideoDownloadManager_DownloadStarted(object sender, NicoVideoCacheRequest request, DownloadOperation op)
        {
            VideoDownloadManager.DownloadStarted -= VideoDownloadManager_DownloadStarted;

            if (request.RawVideoId == this.RawVideoId && request.Quality == this.Quality)
            {
                CacheState = NicoVideoCacheState.Downloading;

                VideoDownloadManager.DownloadProgress += VideoDownloadManager_DownloadProgress;
                VideoDownloadManager.DownloadCanceled += VideoDownloadManager_DownloadCanceled;
                VideoDownloadManager.DownloadCompleted += VideoDownloadManager_DownloadCompleted;
            }
        }

        private void VideoDownloadManager_DownloadCompleted(object sender, NicoVideoCacheRequest request, string filePath)
        {
            if (request.RawVideoId == this.RawVideoId && request.Quality == this.Quality)
            {
                RestoreCache(filePath);

                VideoDownloadManager.DownloadProgress -= VideoDownloadManager_DownloadProgress;
                VideoDownloadManager.DownloadCanceled -= VideoDownloadManager_DownloadCanceled;
                VideoDownloadManager.DownloadCompleted -= VideoDownloadManager_DownloadCompleted;
            }
        }

        private void VideoDownloadManager_DownloadCanceled(object sender, NicoVideoCacheRequest request)
        {
            if (request.RawVideoId == this.RawVideoId && request.Quality == this.Quality)
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

        private void VideoDownloadManager_DownloadProgress(object sender, NicoVideoCacheRequest request, DownloadOperation op)
        {
            if (request.RawVideoId == this.RawVideoId && request.Quality == this.Quality)
            {
                CacheState = NicoVideoCacheState.Downloading;
                CacheProgressSize = (uint)op.Progress.BytesReceived;
            }
        }

        protected async Task<bool> DeleteCacheFile()
		{
			if (!IsAvailable) { return false; }

            var result = await VideoDownloadManager.RemoveCacheRequest(NicoVideo.RawVideoId, Quality);

            var file = await GetCacheFile();
            await file.DeleteAsync();

            CacheState = NicoVideoCacheState.NotCacheRequested;
            CacheFilePath = null;
            VideoFileCreatedAt = default(DateTime);
            IsCacheRequested = false;

            return result;
        }

		protected string VideoFileNameBase
		{
			get
			{
				return $"{NicoVideo.MakeVideoFileName(NicoVideo.Title, NicoVideo.RawVideoId)}";
			}
		}

		public async Task<StorageFile> GetCacheFile()
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
                if (downloadOp  != null)
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


        public float GetDonwloadProgressParcentage()
        {
            var currentBytes = this.CacheProgressSize;
            var total = VideoSize;
            var parcent = (float)Math.Round(((double)currentBytes / total) * 100.0, 1);

            return parcent;
        }

    }


	public class LowQualityNicoVideo : DividedQualityNicoVideo
	{
		public LowQualityNicoVideo(NicoVideo nicoVideo, NiconicoMediaManager context) 
			: base(NicoVideoQuality.Low, nicoVideo, context)
		{
		}




		public override string VideoFileName
		{
			get
			{
				return VideoFileNameBase + ".low.mp4";
			}
		}

		public override string ProgressFileName
		{
			get
			{
				return NicoVideo.GetProgressFileName(RawVideoId) + ".low.json";
			}
		}


		public override uint VideoSize
		{
			get
			{
				return NicoVideo.SizeLow;
			}
		}

		public override bool IsAvailable
		{
			get
			{
				return !NicoVideo.IsOriginalQualityOnly;
			}
		}


		public override bool CanDownload
		{
			get
			{
				// インターネット繋がってるか
				if (!Util.InternetConnection.IsInternet()) { return false; }

				// キャッシュ済みじゃないか
				if (CacheState == NicoVideoCacheState.Cached) { return false; }

				// オリジナル画質しか存在しない動画
				if (!IsAvailable)
				{
					return false;
				}

				return true;
			}
		}

		


		


		





	}

	public class OriginalQualityNicoVideo : DividedQualityNicoVideo
	{
		public OriginalQualityNicoVideo(NicoVideo nicoVideo, NiconicoMediaManager context)
			: base(NicoVideoQuality.Original, nicoVideo, context)
		{
		}

		public override string VideoFileName
		{
			get
			{
				return VideoFileNameBase + ".mp4";
			}
		}

		public override string ProgressFileName
		{
			get
			{
				return NicoVideo.GetProgressFileName(RawVideoId) + ".json";
			}
		}


		public override uint VideoSize
		{
			get
			{
				return NicoVideo.SizeHigh;
			}
		}

		public override bool IsAvailable
		{
			get
			{
				return true;
			}
		}


		public override bool CanDownload
		{
			get
			{
				// インターネット繋がってるか
				if (!Util.InternetConnection.IsInternet()) { return false; }

				// キャッシュ済みじゃないか
				if (CacheState == NicoVideoCacheState.Cached) { return false; }

				// 
				if (NicoVideo.IsOriginalQualityOnly)
				{
					return true;
				}

				// オリジナル画質DL可能時間帯か
				if (NicoVideo.NowLowQualityOnly)
				{
					return false;
				}

				return true;
			}
		}
	}
}