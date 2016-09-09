using Mntone.Nico2.Videos.Thumbnail;
using Mntone.Nico2.Videos.WatchAPI;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace NicoPlayerHohoema.Models
{


	public sealed class NicoVideoDownloadContext : BindableBase, IDisposable
	{
		static internal Task<NicoVideoDownloadContext> Create(HohoemaApp hohoemaApp, NiconicoMediaManager mediaMan)
		{
			var context = new NicoVideoDownloadContext(hohoemaApp);
			context._MediaManager = mediaMan;

			return Task.FromResult(context);
		}





		private NicoVideoDownloadContext(HohoemaApp hohoemaApp)
		{
			_HohoemaApp = hohoemaApp;

			CurrentPlayingDownloader = null;
			CurrentDownloader = null;

			_StreamControlLock = new SemaphoreSlim(1, 1);
			_ExternalAccessControlLock = new SemaphoreSlim(1, 1);

			_DurtyCachedNicoVideo = new List<DividedQualityNicoVideo>();
		}


		public Task<bool> CanAccessVideoCacheFolder()
		{
			return _HohoemaApp.CanAccessVideoCacheFolder();
		}

		public Task<StorageFolder> GetVideoCacheFolder()
		{
			return _HohoemaApp.GetVideoCacheFolder();
		}


		#region Application Lifecycle 

		public async Task Suspending()
		{
			await CloseCurrentPlayingStream();
			await CloseCurrentDownloadStream();

			await ClearDurtyCachedNicoVideo();
		}

		public async Task Resume()
		{
			await TryBeginNextDownloadRequest();
		}

		public void Dispose()
		{
			var task = Suspending();
			task.Wait();
		}

		#endregion


		public void StartBackgroundDownload()
		{
			try
			{
				_ExternalAccessControlLock.Wait();

				TryBeginNextDownloadRequest().ConfigureAwait(false);
			}
			finally
			{
				_ExternalAccessControlLock.Release();
			}
		}


		private void AddDurtyCachedNicoVideo(DividedQualityNicoVideo nicoVideo)
		{
			if (_DurtyCachedNicoVideo.Any(x => x == nicoVideo))
			{
				return;
			}

			if (nicoVideo.IsCacheRequested)
			{
				return;
			}

			_DurtyCachedNicoVideo.Add(nicoVideo);
		}

		public async Task ClearDurtyCachedNicoVideo()
		{
			var preventDeleteVideoId = PreventDeleteOnPlayingVideoId;

			// すでにキャッシュリクエストされたNicoVideoのキャッシュを消さないように注意する
			foreach (var nicoVideo in _DurtyCachedNicoVideo)
			{
				if (preventDeleteVideoId != null && nicoVideo.RawVideoId == preventDeleteVideoId)
				{
					continue;
				}

				if (false == nicoVideo.IsCacheRequested)
				{
					await nicoVideo.DeleteCache();
				}
			}

			WriteOncePreventDeleteVideoId(preventDeleteVideoId);

			_DurtyCachedNicoVideo.Clear();
		}


		#region Video Playback management

		public async Task<NicoVideoDownloader> GetPlayingDownloader(NicoVideo nicoVideo, NicoVideoQuality quality)
		{
			try
			{
				await _ExternalAccessControlLock.WaitAsync();

				NicoVideoDownloader downloader;
				if (CurrentDownloader?.RawVideoId == nicoVideo.RawVideoId
					&& CurrentDownloader?.Quality == quality
					)
				{
					downloader = CurrentDownloader;

					// 再生ストリームを作成します
					await AssignPlayingStream(downloader);
				}
				else
				{
					await CloseCurrentDownloadStream();

					downloader = await CreateDownloader(nicoVideo, quality);

					// 再生ストリームを作成します
					await AssignPlayingStream(downloader);

					if (!downloader.IsCacheComplete)
					{
						await AssignDownloadStream(downloader);
					}
					else
					{
						// call NextDownload 
						await TryBeginNextDownloadRequest();
					}
				}

				return downloader;
			}
			finally
			{
				_ExternalAccessControlLock.Release();
			}
		}

		private async Task AssignPlayingStream(NicoVideoDownloader stream)
		{
			try
			{
				await _StreamControlLock.WaitAsync();

				CurrentPlayingDownloader = stream;

				if (stream != null)
				{
					AddDurtyCachedNicoVideo(stream.DividedQualityNicoVideo);
				}
			}
			finally
			{
				_StreamControlLock.Release();
			}
		}

		
		internal async Task ClosePlayingStream(string rawVideoId)
		{
			try
			{
				await _ExternalAccessControlLock.WaitAsync();

				if (CurrentPlayingDownloader != null &&
					CurrentPlayingDownloader.RawVideoId == rawVideoId)
				{
					await CloseCurrentPlayingStream().ConfigureAwait(false);
				}
			}
			finally
			{
				_ExternalAccessControlLock.Release();
			}
		}

		private async Task CloseCurrentPlayingStream()
		{
			if (CurrentPlayingDownloader != null)
			{
				// 再生ストリームが再生終了後に継続ダウンロードの必要がなければ、閉じる
				if (CurrentPlayingDownloader == _CurrentDownloader)
				{
					if (!CheckCacheRequested(_CurrentDownloader.RawVideoId, _CurrentDownloader.Quality))
					{
						await CloseCurrentDownloadStream().ConfigureAwait(false);
						await TryBeginNextDownloadRequest().ConfigureAwait(false);
					}
				}
				else
				{
					await CurrentPlayingDownloader.StopDownload().ConfigureAwait(false);

					CurrentPlayingDownloader.Dispose();
				}


				await AssignPlayingStream(null);
			}
		}


		#endregion


		#region Video Downloading management

		public bool CheckCacheRequested(string rawVideoId, NicoVideoQuality quality)
		{
			return _MediaManager.CheckHasCacheRequest(rawVideoId, quality);
		}



		public bool CheckVideoPlaying(string rawVideoId, NicoVideoQuality quality)
		{
			if (CurrentPlayingDownloader == null) { return false; }

			return CurrentPlayingDownloader.RawVideoId == rawVideoId && CurrentPlayingDownloader.Quality == quality;
		}

		public bool CheckVideoDownloading(string rawVideoId, NicoVideoQuality quality)
		{
			if (CurrentDownloader == null) { return false; }

			return CurrentDownloader.RawVideoId == rawVideoId && CurrentDownloader.Quality == quality;
		}




		public async Task<bool> RequestDownload(string rawVideoId, NicoVideoQuality quality)
		{
			try
			{
				await _ExternalAccessControlLock.WaitAsync();
				
				if (CurrentDownloader != null)
				{
					if (CurrentDownloader.RawVideoId == rawVideoId && CurrentDownloader.Quality == quality)
					{
						CurrentDownloader.IsCacheRequested = true;
					}
				}

				// 一度ダウンロードキューに積んで
				await AddCacheRequest(rawVideoId, quality);

				// ダウンロード処理を試行する
				await TryBeginNextDownloadRequest();

				return true;
			}
			finally
			{
				_ExternalAccessControlLock.Release();
			}
		}

		internal async Task<bool> CacnelDownloadRequest(string rawVideoId, NicoVideoQuality quality)
		{
			try
			{
				await _ExternalAccessControlLock.WaitAsync();

				var successRemove = false;
				// リクエストキューに登録されていればキャンセルする
				if (_MediaManager.CheckHasCacheRequest(rawVideoId, quality))
				{
					successRemove = await _MediaManager.RemoveCacheRequest(rawVideoId, quality);
				}

				if (CheckVideoPlaying(rawVideoId, quality))
				{
					// 再生中のアイテムをキャンセルする
					CurrentPlayingDownloader.IsCacheRequested = false;
				}
				else if (CheckVideoDownloading(rawVideoId, quality))
				{
					// ダウンロード中のアイテムをキャンセルする
					CurrentDownloader.IsCacheRequested = false;

					await this.CloseCurrentDownloadStream();

					await TryBeginNextDownloadRequest();
				}

				return successRemove;
			}
			finally
			{
				_ExternalAccessControlLock.Release();
			}
		}



		

		private Task AddCacheRequest(string rawVideoid, NicoVideoQuality quality)
		{
			return _MediaManager.AddCacheRequest(rawVideoid, quality);
		}

		private async Task<bool> TryBeginNextDownloadRequest()
		{
			if (_HohoemaApp.MediaManager == null)
			{
				return false;
			}

			_MediaManager = _HohoemaApp.MediaManager;

			if (_CurrentDownloader != null)
			{
				Debug.WriteLine("ダウンロードがすでに実行中のためリクエスト処理をスキップ");
				return false;
			}

			if (false == await _HohoemaApp.CanAccessVideoCacheFolder())
			{
				Debug.WriteLine("ダウンロードキャッシュフォルダにアクセスできないのでDL処理をスキップ");
				return false;
			}

			if (!_MediaManager.HasDownloadQueue)
			{
				return false;
			}

			foreach (var req in _MediaManager.CacheRequestedItemsStack)
			{
				var nicoVideo = await _MediaManager.GetNicoVideoAsync(req.RawVideoid);

				bool isCached = false;
				switch (req.Quality)
				{
					case NicoVideoQuality.Original:
						isCached = nicoVideo.OriginalQuality.IsCached;
						break;
					case NicoVideoQuality.Low:
						isCached = nicoVideo.LowQuality.IsCached;
						break;
					default:
						break;
				}

				if (isCached)
				{
					Debug.WriteLine($"{req.RawVideoid}:{req.Quality}はダウンロード済みのため処理をスキップ");
				}
				else
				{

					try
					{
						var stream = await CreateDownloader(req.RawVideoid, req.Quality).ConfigureAwait(false);
						stream.IsCacheRequested = true;

						Debug.WriteLine($"{req.RawVideoid}:{req.Quality}のダウンロードを開始");
						await AssignDownloadStream(stream).ConfigureAwait(false);
					}
					catch (Exception ex)
					{
						Debug.WriteLine($"{req.RawVideoid}:{req.Quality}のダウンロード開始に失敗しました。");
						Debug.WriteLine(ex.ToString());
						if (req.Quality == NicoVideoQuality.Original)
						{
							Debug.WriteLine("オリジナル画質がダウンロードできない時間帯の可能性があります。");
						}
					}
					break;
				}
			}

			return true;
		}

		private async Task AssignDownloadStream(NicoVideoDownloader downloadStream)
		{
			try
			{
				await _StreamControlLock.WaitAsync();

				CurrentDownloader = downloadStream;
				if (!_CurrentDownloader.IsCacheComplete)
				{
					_CurrentDownloader.OnCacheComplete += DownloadCompleteAction;
					_CurrentDownloader.OnCacheCanceled += _CurrentDownloader_OnCacheCanceled;
					_CurrentDownloader.OnCacheProgress += _CurrentDownloadStream_OnCacheProgress;
				}

				await _CurrentDownloader.Download();
			}
			finally
			{
				_StreamControlLock.Release();
			}
		}

		
		private async Task CloseCurrentDownloadStream()
		{
			try
			{
				await _StreamControlLock.WaitAsync();

				if (_CurrentDownloader != null)
				{
					await _CurrentDownloader.StopDownload();
					_CurrentDownloader.Dispose();

					_CurrentDownloader.OnCacheComplete -= DownloadCompleteAction;
					_CurrentDownloader.OnCacheCanceled -= _CurrentDownloader_OnCacheCanceled;
					_CurrentDownloader.OnCacheProgress -= _CurrentDownloadStream_OnCacheProgress;
					_CurrentDownloader = null;
				}
			}
			finally
			{
				_StreamControlLock.Release();
			}
		}

		private async Task<NicoVideoDownloader> CreateDownloader(NicoVideo nicoVideo, NicoVideoQuality quality)
		{
			
			await nicoVideo.SetupWatchPageVisit(quality);


			// オリジナル画質が必要で
			// オリジナル画質のダウンロードリクエストができなくて
			// オリジナル画質がキャッシュされていない場合
			// 例外を投げる
			if (quality == NicoVideoQuality.Original
				&& !nicoVideo.OriginalQuality.CanRequestDownload
				&& !nicoVideo.OriginalQuality.IsCached
				)
			{
				// ダウンロード再生ができない
				throw new Exception("can not download video with original quality in current time.");
			}

			switch (quality)
			{
				case NicoVideoQuality.Original:
					return await nicoVideo.OriginalQuality.CreateDownloader();
				case NicoVideoQuality.Low:
					return await nicoVideo.LowQuality.CreateDownloader();
				default:
					throw new NotSupportedException($"not support NicoVideoQuality, {quality}");
			}
			
		}

		private async Task<NicoVideoDownloader> CreateDownloader(string rawVideoid, NicoVideoQuality quality)
		{
			var nicoVideo = await _HohoemaApp.MediaManager.GetNicoVideoAsync(rawVideoid).ConfigureAwait(false);

			return await CreateDownloader(nicoVideo, quality);
		}

		

		

		#endregion


		#region Cache Progress Event Handler

		private async void _CurrentDownloadStream_OnCacheProgress(string rawVideoId, NicoVideoQuality quality, uint totalSize, uint size)
		{
			try
			{
				await _ExternalAccessControlLock.WaitAsync();
			}
			finally
			{
				_ExternalAccessControlLock.Release();
			}
		}

		private async void DownloadCompleteAction(string rawVideoid)
		{
			try
			{
				await _ExternalAccessControlLock.WaitAsync();


				if (_CurrentDownloader != null &&
				_CurrentDownloader.RawVideoId == rawVideoid)
				{
					_CurrentDownloader.OnCacheComplete -= DownloadCompleteAction;
					_CurrentDownloader.OnCacheCanceled -= _CurrentDownloader_OnCacheCanceled;
					_CurrentDownloader.OnCacheProgress -= _CurrentDownloadStream_OnCacheProgress;

					if (_CurrentDownloader != _CurrentPlayingDownloader)
					{
						_CurrentDownloader.Dispose();
					}

					var quality = _CurrentDownloader.Quality;
					CurrentDownloader = null;

					Debug.WriteLine($"{rawVideoid}:{quality.ToString()} のダウンロード完了");
					
					await TryBeginNextDownloadRequest().ConfigureAwait(false);
				}
				else
				{
					throw new Exception("ダウンロードタスクの解除処理が異常");
				}
			}
			finally
			{
				_ExternalAccessControlLock.Release();
			}
			
		}

		private async void _CurrentDownloader_OnCacheCanceled(string rawVideoId)
		{
			try
			{
				await _ExternalAccessControlLock.WaitAsync();


				if (_CurrentDownloader != null &&
					_CurrentDownloader.RawVideoId == rawVideoId)
				{
					_CurrentDownloader.OnCacheComplete -= DownloadCompleteAction;
					_CurrentDownloader.OnCacheCanceled -= _CurrentDownloader_OnCacheCanceled;
					_CurrentDownloader.OnCacheProgress -= _CurrentDownloadStream_OnCacheProgress;

					if (_CurrentDownloader != _CurrentPlayingDownloader)
					{
						_CurrentDownloader.Dispose();
					}

					var quality = _CurrentDownloader.Quality;
					CurrentDownloader = null;

					Debug.WriteLine($"{rawVideoId}:{quality.ToString()} のダウンロードをキャンセル");

					await TryBeginNextDownloadRequest().ConfigureAwait(false);
				}
			}
			finally
			{
				_ExternalAccessControlLock.Release();
			}
		}



		#endregion


		#region Once Prevent Delete Video

		const string ONCE_PREVCENT_VIDEO_ID_SETTING_KEY = "prevent_delete_video_id";


		public void ClearPreventDeleteCacheOnPlayingVideo()
		{
			PreventDeleteOnPlayingVideoId = "";
		}

		public void OncePreventDeleteCacheOnPlayingVideo(string rawVideoId)
		{
			PreventDeleteOnPlayingVideoId = rawVideoId;
		}

		internal static string ReadAndClearOncePreventDeleteVideoId()
		{
			if (ApplicationData.Current.LocalSettings.Values.ContainsKey(ONCE_PREVCENT_VIDEO_ID_SETTING_KEY))
			{
				var videoId = (string)ApplicationData.Current.LocalSettings.Values[ONCE_PREVCENT_VIDEO_ID_SETTING_KEY];
				ApplicationData.Current.LocalSettings.Values.Remove(ONCE_PREVCENT_VIDEO_ID_SETTING_KEY);

				return videoId;
			}
			else
			{
				return null;
			}
		}

		internal static void WriteOncePreventDeleteVideoId(string videoId)
		{
			if (ApplicationData.Current.LocalSettings.Values.ContainsKey(ONCE_PREVCENT_VIDEO_ID_SETTING_KEY))
			{
				ApplicationData.Current.LocalSettings.Values.Remove(ONCE_PREVCENT_VIDEO_ID_SETTING_KEY);
			}

			ApplicationData.Current.LocalSettings.Values.Add(ONCE_PREVCENT_VIDEO_ID_SETTING_KEY, videoId);
		}


		#endregion


		// Donwload/Playingのストリームのアサイン、破棄のタイミング同期用ロック
		private SemaphoreSlim _StreamControlLock;

		// 外部からの呼び出しによる非同期アクセスからデータを保護するロック
		private SemaphoreSlim _ExternalAccessControlLock;


		private NicoVideoDownloader _CurrentPlayingDownloader;
		public NicoVideoDownloader CurrentPlayingDownloader
		{
			get
			{
				return _CurrentPlayingDownloader;
			}
			private set
			{
				SetProperty(ref _CurrentPlayingDownloader, value);
			}
		}

		private NicoVideoDownloader _CurrentDownloader;
		public NicoVideoDownloader CurrentDownloader
		{
			get
			{
				return _CurrentDownloader;
			}
			private set
			{
				SetProperty(ref _CurrentDownloader, value);
			}
		}

		// キャッシュリクエスト外の中途半端にキャッシュされた
		// NicoVideoオブジェクト
		// キャッシュがOffの状況では常に0になるようにする
		private List<DividedQualityNicoVideo> _DurtyCachedNicoVideo;
		public string PreventDeleteOnPlayingVideoId { get; private set; }

		NiconicoMediaManager _MediaManager;
		HohoemaApp _HohoemaApp;
	}

}
