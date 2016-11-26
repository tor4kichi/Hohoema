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

	
	public class NiconicoDownloadEventArgs
	{
		public string RawVideoId { get; set; }
		public NicoVideoQuality Quality { get; set; }
	}

	public delegate void StartDownloadEventHandler(NicoVideoDownloadContext sender, NiconicoDownloadEventArgs args);
	public delegate void DoneDownloadEventHandler(NicoVideoDownloadContext sender, NiconicoDownloadEventArgs args);
	public delegate void CancelDownloadEventHandler(NicoVideoDownloadContext sender, NiconicoDownloadEventArgs args);

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

			CurrentDownloader = null;

			_StreamControlLock = new SemaphoreSlim(1, 1);
			_ExternalAccessControlLock = new SemaphoreSlim(1, 1);

			_DurtyCachedNicoVideo = new List<DividedQualityNicoVideo>();
		}


		public Task<bool> CanReadAccessVideoCacheFolder()
		{
			return _HohoemaApp.CanReadAccessVideoCacheFolder();
		}

		public Task<bool> CanWriteAccessVideoCacheFolder()
		{
			return _HohoemaApp.CanWriteAccessVideoCacheFolder();
		}

		public Task<StorageFolder> GetVideoCacheFolder()
		{
			return _HohoemaApp.GetVideoCacheFolder();
		}


		#region Application Lifecycle 

		public async Task Suspending()
		{
			await CloseCurrentDownloadStream();

			await ClearDurtyCachedNicoVideo();
		}

		public void Dispose()
		{
			var task = Suspending();
			task.Wait();
		}

		#endregion


		public async Task StartBackgroundDownload()
		{
			try
			{
				_ExternalAccessControlLock.Wait();

				await TryBeginNextDownloadRequest().ConfigureAwait(false);
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
			foreach (var nicoVideo in _DurtyCachedNicoVideo.ToArray())
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

		
		public async Task<NicoVideoDownloader> GetDownloader(NicoVideo nicoVideo, NicoVideoQuality quality)
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
				}
				else
				{
					await CloseCurrentDownloadStream();

					downloader = await CreateDownloader(nicoVideo, quality);

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

		#region Video Downloading management

		public bool CheckCacheRequested(string rawVideoId, NicoVideoQuality quality)
		{
			return _MediaManager.CheckHasCacheRequest(rawVideoId, quality);
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

		public async Task<bool> StopDownload(string rawVideoId, NicoVideoQuality quality)
		{
			try
			{
				await _ExternalAccessControlLock.WaitAsync();

				if (CurrentDownloader != null)
				{
					if (CurrentDownloader.RawVideoId == rawVideoId && CurrentDownloader.Quality == quality)
					{
						await CloseCurrentDownloadStream();
					}

					return true;
				}
				else
				{
					return false;
				}

			}
			finally
			{
				_ExternalAccessControlLock.Release();
			}
		}

		//
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

				if (CheckVideoDownloading(rawVideoId, quality))
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
            if (!_HohoemaApp.IsLoggedIn)
            {
                return false;
            }

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

			if (false == await _HohoemaApp.CanReadAccessVideoCacheFolder())
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
						var stream = await CreateDownloader(req.RawVideoid, req.Quality);
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

				// ダウンロード開始イベントをトリガー
				StartDownload?.Invoke(this, new NiconicoDownloadEventArgs()
				{
					RawVideoId = downloadStream.RawVideoId,
					Quality = downloadStream.Quality
				});
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
					await _CurrentDownloader.StopDownload().ConfigureAwait(false);

					_CurrentDownloader.OnCacheComplete -= DownloadCompleteAction;
					_CurrentDownloader.OnCacheCanceled -= _CurrentDownloader_OnCacheCanceled;
					_CurrentDownloader.OnCacheProgress -= _CurrentDownloadStream_OnCacheProgress;

					try
					{
                        // キャッシュリクエスト済みの場合はDL進捗を保存
                        if (CurrentDownloader.IsCacheRequested)
                        {
                            await CurrentDownloader.DividedQualityNicoVideo.SaveProgress();
                        }

                        if (!CurrentDownloader.IsCacheComplete)
						{
							// ダウンロードキャンセルイベントをトリガー
							CancelDownload?.Invoke(this, new NiconicoDownloadEventArgs()
							{
								RawVideoId = _CurrentDownloader.RawVideoId,
								Quality = _CurrentDownloader.Quality
							});
						}
						else
						{
							// ダウンロード完了イベントをトリガー
							DoneDownload?.Invoke(this, new NiconicoDownloadEventArgs()
							{
								RawVideoId = _CurrentDownloader.RawVideoId,
								Quality = _CurrentDownloader.Quality
							});
						}
					}
					catch (Exception ex)
					{
						Debug.WriteLine(ex.ToString());
					}
					
					_CurrentDownloader.Dispose();
					CurrentDownloader = null;
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
			// オリジナル画質のダウンロードができなくて
			// オリジナル画質がキャッシュされていない場合
			// 例外を投げる
			if (quality == NicoVideoQuality.Original
				&& !nicoVideo.OriginalQuality.CanDownload
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
					Debug.WriteLine($"{rawVideoid}:{_CurrentDownloader.Quality.ToString()} のダウンロード完了");

					await CloseCurrentDownloadStream();
					
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
					Debug.WriteLine($"{rawVideoId}:{_CurrentDownloader.Quality.ToString()} のダウンロードをキャンセル");

					_CurrentDownloader.OnCacheComplete -= DownloadCompleteAction;
					_CurrentDownloader.OnCacheCanceled -= _CurrentDownloader_OnCacheCanceled;
					_CurrentDownloader.OnCacheProgress -= _CurrentDownloadStream_OnCacheProgress;
					
					_CurrentDownloader.Dispose();
					CurrentDownloader = null;

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
			PreventDeleteOnPlayingVideoId = null;
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

		public event StartDownloadEventHandler StartDownload;
		public event CancelDownloadEventHandler CancelDownload;
		public event DoneDownloadEventHandler DoneDownload;


		// Donwload/Playingのストリームのアサイン、破棄のタイミング同期用ロック
		private SemaphoreSlim _StreamControlLock;

		// 外部からの呼び出しによる非同期アクセスからデータを保護するロック
		private SemaphoreSlim _ExternalAccessControlLock;

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
