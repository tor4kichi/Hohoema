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

        public bool IsNotCacheRequested => CacheState == NicoVideoCacheState.NotCacheRequested;
        public bool NowCachePending => CacheState == NicoVideoCacheState.Pending;
        public bool NowCacheDonwloading => CacheState == NicoVideoCacheState.Downloading;
        public bool IsCached => CacheState == NicoVideoCacheState.Cached;

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


        internal void RestoreCache(string path)
        {
            CacheFilePath = path;
            IsCacheRequested = !string.IsNullOrEmpty(path);
            CacheState = NicoVideoCacheState.Cached;
        }

        /*
		internal async Task CheckCacheStatus()
		{
			if (!IsAvailable)
			{
				CacheState = null;
			}

			IsCacheRequested = NiconicoMediaManager.CheckCacheRequested(this.RawVideoId, Quality);
            var isCacheCompleted = NiconicoMediaManager.CheckVideoCached(this.RawVideoId, Quality);
            if (isCacheCompleted)
            {
                CacheState = NicoVideoCacheState.Cached;
                var videoFile = await NiconicoMediaManager.GetCachedVideo(this.RawVideoId, Quality);
                VideoFileCreatedAt = videoFile.DateCreated.LocalDateTime;

                OnPropertyChanged(nameof(CanRequestCache));
                OnPropertyChanged(nameof(CanPlay));
                OnPropertyChanged(nameof(IsCached));
                OnPropertyChanged(nameof(CanPlay));

            }
            else if (IsCacheRequested)
			{
                var downloadOperation = NiconicoMediaManager.GetCacheProgressVideoStream(this.RawVideoId, Quality);
                if (downloadOperation != null)
                {
                    CacheState = NicoVideoCacheState.Downloading;
                    var videoFile = downloadOperation.ResultFile;
                    VideoFileCreatedAt = videoFile.DateCreated.LocalDateTime;
                }
                else
                {
                    CacheState = NicoVideoCacheState.Pending;
                }

				OnPropertyChanged(nameof(CanRequestCache));
				OnPropertyChanged(nameof(CanPlay));
				OnPropertyChanged(nameof(IsCached));
				OnPropertyChanged(nameof(CanPlay));
			}
			else
			{
				CacheState = null;
				VideoFileCreatedAt = default(DateTime);
			}

		}
        */
        

		

		public async Task DeleteCache()
		{
			Debug.Write($"{NicoVideo.Title}:{Quality.ToString()}のキャッシュを削除開始...");

			await DeleteCacheFile();

			Debug.WriteLine($".完了");
		}

		public async Task RequestCache()
		{
			if (!IsAvailable) { return; }

			if (IsCached || NowCacheDonwloading)
			{
                // update cached time
                await GetCacheFile();
				return;
			}

            await VideoDownloadManager.AddCacheRequest(NicoVideo.RawVideoId, Quality);

			await NicoVideo.OnCacheRequested();
		}

		protected async Task<bool> DeleteCacheFile()
		{
			if (!IsAvailable) { return false; }

            return await VideoDownloadManager.RemoveCacheRequest(NicoVideo.RawVideoId, Quality);
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


        public Task<DownloadOperation> GetDownloadOperation()
        {
            return VideoDownloadManager.GetDownloadingVideoOperation(this.RawVideoId, this.Quality);
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