using NicoPlayerHohoema.Util;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace NicoPlayerHohoema.Models
{
    public abstract class DividedQualityNicoVideo : BindableBase
    {
        // Note: ThumbnailResponseが初期化されていないと利用できない

        public NicoVideoQuality Quality { get; private set; }
        public NicoVideo NicoVideo { get; private set; }
        public NiconicoMediaManager NiconicoMediaManager { get; private set; }
        public HohoemaApp HohoemaApp { get; private set; }
                

        public string RawVideoId
		{
			get
			{
				return NicoVideo.RawVideoId;
			}
		}


		private NicoVideoCacheState? _CacheState;
		public NicoVideoCacheState? CacheState
		{
			get { return _CacheState; }
			protected set { SetProperty(ref _CacheState, value); }
		}

		public bool IsReadyOfflinePlay { get; private set; }


		
		/// <summary>
		/// このクオリティは利用可能か
		/// (オリジナル画質しか存在しない動画の場合、true)
		/// </summary>
		public abstract bool IsAvailable { get; }

		public DividedQualityNicoVideo(NicoVideoQuality quality, NicoVideo nicoVideo, NiconicoMediaManager context)
		{
			Quality = quality;
			NicoVideo = nicoVideo;
            HohoemaApp = nicoVideo.HohoemaApp;
            NiconicoMediaManager = context;
		}

		


		public abstract string VideoFileName { get; }

		public abstract string ProgressFileName { get; }


		public abstract bool CanDownload { get; }

		public abstract uint VideoSize { get; }
		
		public DateTime VideoFileCreatedAt { get; private set; }


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

		public bool IsCached
		{
			get
			{
				return CacheState == NicoVideoCacheState.Cached;
			}
		}

		public bool HasCache
		{
			get
			{
				return CacheState.HasValue;
			}
		}


		private bool _IsCacheRequested;
		public bool IsCacheRequested
		{
			get { return _IsCacheRequested; }
			protected set { SetProperty(ref _IsCacheRequested, value); }
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
            }
			else if (IsCacheRequested)
			{
				var videoCacheFolder = await HohoemaApp.GetVideoCacheFolder();
				if (videoCacheFolder == null)
				{
					return;
				}

				var videoFile = await videoCacheFolder.TryGetItemAsync(VideoFileName) as StorageFile;
				var existVideo = videoFile != null;

				if (existVideo)
				{
					CacheState = NicoVideoCacheState.CacheProgress;
				}
				else // if (NicoVideoCachedStream.ExistIncompleteOriginalQuorityVideo(CachedWatchApiResponse, saveFolder))
				{
					CacheState = null;
				}

				OnPropertyChanged(nameof(CanRequestCache));
				OnPropertyChanged(nameof(CanPlay));
				OnPropertyChanged(nameof(IsCached));
				OnPropertyChanged(nameof(CanPlay));

				// キャッシュの日付を取得
				if (existVideo)
				{
					VideoFileCreatedAt = videoFile.DateCreated.LocalDateTime;
				}
			}
			else
			{
				CacheState = null;
				VideoFileCreatedAt = default(DateTime);
			}

		}

        

		

		public async Task DeleteCache()
		{
			Debug.Write($"{NicoVideo.Title}:{Quality.ToString()}のキャッシュを削除開始...");

			await DeleteCacheFile();

			await CheckCacheStatus();

			Debug.WriteLine($".完了");
		}

		public async Task RequestCache()
		{
			if (!IsAvailable) { return; }

			if (NiconicoMediaManager.CheckCacheRequested(NicoVideo.RawVideoId, Quality))
			{
				return;
			}

			await NiconicoMediaManager.AddCacheRequest(NicoVideo.RawVideoId, Quality);

			await CheckCacheStatus();

			await NicoVideo.OnCacheRequested();
		}

		protected async Task<bool> DeleteCacheFile()
		{
			if (!IsAvailable) { return false; }

            return await NiconicoMediaManager.RemoveCacheRequest(NicoVideo.RawVideoId, Quality);
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
            return await NiconicoMediaManager.GetCachedVideo(this.RawVideoId, Quality);
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