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
	abstract public class DividedQualityNicoVideo : BindableBase
	{
		// Note: ThumbnailResponseが初期化されていないと利用できない

		public NicoVideoQuality Quality { get; private set; }
		public NicoVideo NicoVideo { get; private set; }
		protected NicoVideoDownloadContext _Context;


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


		
		/// <summary>
		/// このクオリティは利用可能か
		/// (オリジナル画質しか存在しない動画の場合、true)
		/// </summary>
		abstract public bool IsAvailable { get; }

		public uint CacheProgressSize { get; private set; }

		public VideoDownloadProgress Progress { get; private set; }

		protected FileAccessor<VideoDownloadProgress> _DownloadProgressFileAccessor;


		public DividedQualityNicoVideo(NicoVideoQuality quality, NicoVideo nicoVideo, NicoVideoDownloadContext context)
		{
			Quality = quality;
			NicoVideo = nicoVideo;
			_Context = context;

			_DownloadProgressFileAccessor = new FileAccessor<VideoDownloadProgress>(_Context.VideoSaveFolder, ProgressFileName);
		}

		public async Task SetupDownloadProgress()
		{
			// DLが途中の場合はこのロードが成功しProgressが埋まる
			Progress = await _DownloadProgressFileAccessor.Load();

			if (Progress == null)
			{
				Progress = new VideoDownloadProgress(VideoSize);

				// 既に完了している場合
				if (ExistVideo)
				{
					Progress.Update(0, VideoSize);
				}
			}

			await CheckCacheStatus();
		}



		abstract public string VideoFileName { get; }

		abstract public string ProgressFileName { get; }



		abstract public bool CanRequestDownload { get; }

		public abstract uint VideoSize { get; }


		public bool CanPlay
		{
			get
			{
				return CanRequestDownload
					|| CacheState == NicoVideoCacheState.Cached;
			}
		}


		public bool IsCached
		{
			get
			{
				return CacheState == NicoVideoCacheState.Cached;
			}
		}


		public bool IsCacheRequested
		{
			get
			{
				return CacheState.HasValue;
			}
		}


		public bool ExistVideo
		{
			get
			{
				return _Context.VideoSaveFolder.ExistFile(VideoFileName);
			}
		}


		internal Task SaveProgress()
		{
			return _DownloadProgressFileAccessor.Save(Progress);
		}


		internal Task CheckCacheStatus()
		{
			if (!IsAvailable)
			{
				CacheState = null;
				return Task.CompletedTask;
			}


			if (_Context.CheckCacheRequested(this.RawVideoId, Quality))
			{
				// すでにダウンロード済みのキャッシュファイルをチェック
				if (ExistVideo
					&& (Progress.CheckComplete()))
				{
					CacheState = NicoVideoCacheState.Cached;
				}
				else if (_Context.CheckCacheRequested(this.RawVideoId, Quality))
				{
					CacheState = NicoVideoCacheState.CacheRequested;
				}
				else if (_Context.CheckVideoDownloading(this.RawVideoId, Quality))
				{
					CacheState = NicoVideoCacheState.NowDownloading;
				}
				else // if (NicoVideoCachedStream.ExistIncompleteOriginalQuorityVideo(CachedWatchApiResponse, saveFolder))
				{
					CacheState = null;
				}
			}
			else
			{
				CacheState = null;
			}


			OnPropertyChanged(nameof(CanRequestDownload));
			OnPropertyChanged(nameof(CanPlay));
			OnPropertyChanged(nameof(IsCached));
			
			return Task.CompletedTask;
		}


		internal async Task<NicoVideoDownloader> CreateDownloader()
		{
			if (!IsAvailable)
			{
				throw new Exception("");
			}

			if (!CanPlay && !CanRequestDownload)
			{
				throw new Exception("");
			}


			var file = await GetCacheFile();
			var stream = new NicoVideoDownloader(
				this
				, NicoVideo.HohoemaApp.NiconicoContext.HttpClient
				, NicoVideo.WatchApiResponseCache.CachedItem
				, file
				);

			System.Diagnostics.Debug.WriteLine($"size:{stream.Size}");

			return stream;
		}


		public async Task DeleteCache()
		{
			Debug.Write($"{NicoVideo.Title}:{Quality.ToString()}のキャッシュを削除開始...");

			await CancelCacheRequest();

			await DeleteCacheFile();

			await DeleteDownloadProgress();

			await CheckCacheStatus();


			await NicoVideo.OnDeleteCache();

			Debug.WriteLine($".完了");
		}

		public async Task RequestCache()
		{
			if (!IsAvailable) { return; }

			if (_Context.CheckCacheRequested(NicoVideo.RawVideoId, Quality))
			{
				return;
			}

			await _Context.RequestDownload(NicoVideo.RawVideoId, Quality);

			_Context.OnCacheStarted += _Context_OnCacheStarted;
			_Context.OnCacheCompleted += _Context_OnCacheCompleted;
			_Context.OnCacheProgress += _Context_OnCacheProgress;

			await CheckCacheStatus();


			await NicoVideo.OnCacheRequested();
		}

		protected async Task DeleteCacheFile()
		{
			if (!IsAvailable) { return; }

			var saveFolder = _Context.VideoSaveFolder;
			var fileName = VideoFileName;
			if (saveFolder.ExistFile(fileName))
			{
				var file = await saveFolder.GetFileAsync(fileName);
				await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
			}
		}

		protected Task DeleteDownloadProgress()
		{
			if (!IsAvailable) { return Task.CompletedTask; }

			// プログレスファイルの削除
			var progressFileName = ProgressFileName;

			progressFileName += ".low.json";

			var progressFileAccessor = new FileAccessor<VideoDownloadProgress>(_Context.VideoSaveFolder, progressFileName);
			return progressFileAccessor.Delete();
		}





		protected string VideoFileNameBase
		{
			get
			{
				return $"{NicoVideo.MakeVideoFileName(NicoVideo.Title, NicoVideo.RawVideoId)}";
			}
		}

		public async Task CancelCacheRequest()
		{
			if (await _Context.CacnelDownloadRequest(this.RawVideoId, Quality))
			{
				await this.CheckCacheStatus();
			}
		}

		public async Task<StorageFile> GetCacheFile()
		{
			return await _Context.VideoSaveFolder.CreateFileAsync(VideoFileName, CreationCollisionOption.OpenIfExists);
		}

		public void DeletedTeardown()
		{

		}



		#region Event Handler


		private async void _Context_OnCacheStarted(string rawVideoId, NicoVideoQuality quality)
		{
			if (RawVideoId == rawVideoId && quality == Quality)
			{
				await NicoVideo._Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
				{
					CacheState = NicoVideoCacheState.NowDownloading;
					await SaveProgress();
				});
			}
		}

		private async void _Context_OnCacheProgress(string rawVideoId, NicoVideoQuality quality, uint totalSize, uint size)
		{
			if (rawVideoId == RawVideoId && quality == Quality)
			{
				await NicoVideo._Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
				{
					CacheState = NicoVideoCacheState.NowDownloading;
					CacheProgressSize = size;
					OnPropertyChanged(nameof(CacheProgressSize));
				});
			}
		}

		private async void _Context_OnCacheCompleted(string videoId, NicoVideoQuality quality, bool isSuccess)
		{
			if (videoId == RawVideoId)
			{
				await NicoVideo._Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
				{
					await _DownloadProgressFileAccessor?.Delete();

					await CheckCacheStatus();
				});
			}
		}


		#endregion
	}


	public class LowQualityNicoVideo : DividedQualityNicoVideo
	{
		public LowQualityNicoVideo(NicoVideo nicoVideo, NicoVideoDownloadContext context) 
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
				return NicoVideo.ThumbnailResponseCache.LowQualityVideoSize;
			}
		}

		public override bool IsAvailable
		{
			get
			{
				return !NicoVideo.ThumbnailResponseCache.IsOriginalQualityOnly;
			}
		}



		
		public override bool CanRequestDownload
		{
			get
			{
				// インターネット繋がってるか
				if (!Util.InternetConnection.IsInternet()) { return false; }

				// キャッシュリクエスト済みじゃないか
				if (CacheState.HasValue) { return false; }


				if (!NicoVideo.ThumbnailResponseCache.HasCache)
				{
					throw new Exception();
				}

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
		public OriginalQualityNicoVideo(NicoVideo nicoVideo, NicoVideoDownloadContext context)
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
				return NicoVideo.ThumbnailResponseCache.OriginalQualityVideoSize;
			}
		}

		public override bool IsAvailable
		{
			get
			{
				return true;
			}
		}


		public override bool CanRequestDownload
		{
			get
			{
				// インターネット繋がってるか
				if (!Util.InternetConnection.IsInternet()) { return false; }

				// キャッシュリクエスト済みじゃないか
				if (CacheState.HasValue) { return false; }

				// 
				if (NicoVideo.ThumbnailResponseCache.IsOriginalQualityOnly)
				{
					return true;
				}

				// オリジナル画質DL可能時間帯か
				if (WatchApiResponseCache.NowLowQualityOnly)
				{
					return false;
				}

				return true;
			}
		}
	}
}