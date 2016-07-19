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
	public delegate void CacheStartedHandler(string rawVideoId, NicoVideoQuality quality);
	public delegate void CacheCompleteHandler(string videoId, NicoVideoQuality quality, bool isSuccess);
	public delegate void CacheProgressHandler(string rawVideoId, NicoVideoQuality quality, uint totalSize, uint size);



	public sealed class NicoVideoDownloadContext : BindableBase, IDisposable
	{
		static internal async Task<NicoVideoDownloadContext> Create(HohoemaApp hohoemaApp)
		{
			var context = new NicoVideoDownloadContext(hohoemaApp);

			context.VideoSaveFolder = await hohoemaApp.GetCurrentUserVideoFolder();

			return context;
		}





		private NicoVideoDownloadContext(HohoemaApp hohoemaApp)
		{
			_HohoemaApp = hohoemaApp;
			_CacheRequestStack = new ObservableCollection<NicoVideoCacheRequest>();
			CacheRequestStack = new ReadOnlyObservableCollection<NicoVideoCacheRequest>(_CacheRequestStack);

			CurrentPlayingStream = null;
			CurrentDownloadStream = null;

			_StreamControlLock = new SemaphoreSlim(1, 1);
		}

		public async Task<NicoVideoCachedStream> GetPlayingStream(string rawVideoId, NicoVideoQuality quality)
		{
			await CloseCurrentPlayingStream();


			// 閉じれていない場合はNG
			Debug.Assert(CurrentPlayingStream == null);

			// 再生用のストリームを取得します
			// すでに開始されたダウンロードタスクは一旦中断し、再生用ストリームに回線を明け渡します。
			// 中断されたダウンロードタスクは、ダウンロードスタックに積み、再生用ストリームのダウンロード完了を確認して
			// ダウンロードを再開させます。		

			// 再生用対象をダウンロード中の場合は
			// キャッシュモードを無視してダウンロード中のストリームをそのまま帰す
			if (_CurrentDownloadStream != null &&
				_CurrentDownloadStream.RawVideoId == rawVideoId)
			{
				await AssignPlayingStream(_CurrentDownloadStream);
			}
			else
			{
				// 再生ストリームを作成します
				// 現在のダウンロードタスクは後でダウンロードするようにスタックへ積み直します
				if (_CurrentDownloadStream != null)
				{
					await PushToTopCurrentDownloadRequest();
				}

				var stream = await CreateDownloadStream(rawVideoId, quality, isRequireCache: false);

				await AssignPlayingStream(stream);

				if (!stream.IsCacheComplete)
				{
					await AssignDownloadStream(stream);
				}
				else
				{
					// call NextDownload 
					await TryBeginNextDownloadRequest();
				}
			}

			return CurrentPlayingStream;
		}



		public async Task RequestDownload(string rawVideoId, NicoVideoQuality quality)
		{
			if (CurrentDownloadStream != null)
			{
				if (CurrentDownloadStream.RawVideoId == rawVideoId && CurrentDownloadStream.Quality == quality)
				{
					CurrentDownloadStream.ChangeCacheRequire(true);
					return;
				}
			}


			PushDownloadRequest(rawVideoId, quality);

			await TryBeginNextDownloadRequest();
		}

		public async Task ClosePlayingStream(string rawVideoId)
		{
			if (_CurrentPlayingStream != null &&
				_CurrentPlayingStream.VideoId == rawVideoId)
			{
				await CloseCurrentPlayingStream();
			}
		}

		private async Task CloseCurrentPlayingStream()
		{
			if (_CurrentPlayingStream != null)
			{
				// 再生ストリームが再生終了後に継続ダウンロードの必要がなければ、閉じる
				if (_CurrentPlayingStream == _CurrentDownloadStream)
				{
					if (!_CurrentPlayingStream.IsRequireCache)
					{
						await CloseCurrentDownloadStream();
						await TryBeginNextDownloadRequest();
					}
				}
				else
				{
					_CurrentPlayingStream.Dispose();
				}

				_CurrentPlayingStream = null;
			}
		}


		private async Task CloseCurrentDownloadStream()
		{
			try
			{
				await _StreamControlLock.WaitAsync();

				if (_CurrentDownloadStream != null)
				{
					await _CurrentDownloadStream.StopDownload();

					OnCacheCompleted?.Invoke(_CurrentDownloadStream.RawVideoId, _CurrentDownloadStream.Quality, false);
					_CurrentDownloadStream.OnCacheComplete -= DownloadCompleteAction;
					_CurrentDownloadStream.Dispose();
					_CurrentDownloadStream = null;
				}
			}
			finally
			{
				_StreamControlLock.Release();
			}
		}


		public async Task CacnelDownloadRequest(string rawVideoId, NicoVideoQuality quality)
		{
			if (CheckVideoPlaying(rawVideoId, quality))
			{
				CurrentPlayingStream.ChangeCacheRequire(false);
			}
			else if (CheckVideoDownloading(rawVideoId, quality))
			{
				await this.CloseCurrentDownloadStream();

				await TryBeginNextDownloadRequest();
			}
			else
			{
				var req = _CacheRequestStack.SingleOrDefault(x => x.RawVideoid == rawVideoId && x.Quality == quality);

				if (req != null)
				{
					_CacheRequestStack.Remove(req);
					OnCacheCompleted?.Invoke(req.RawVideoid, req.Quality, false);
				}
			}
		}


		public bool CheckCacheRequested(string rawVideoId, NicoVideoQuality quality)
		{
			return _CacheRequestStack.Any(x => x.RawVideoid == rawVideoId && x.Quality == quality);
		}

		public bool CheckVideoPlaying(string rawVideoId, NicoVideoQuality quality)
		{
			if (CurrentPlayingStream == null) { return false; }

			return CurrentPlayingStream.VideoId == rawVideoId && CurrentPlayingStream.Quality == quality;
		}

		public bool CheckVideoDownloading(string rawVideoId, NicoVideoQuality quality)
		{
			if (CurrentDownloadStream == null) { return false; }

			return CurrentDownloadStream.VideoId == rawVideoId && CurrentDownloadStream.Quality == quality;
		}


		public void StartBackgroundDownload()
		{
			TryBeginNextDownloadRequest().ConfigureAwait(false);
		}

		private async Task<bool> TryBeginNextDownloadRequest()
		{
			if (_HohoemaApp.MediaManager == null)
			{
				return false;
			}

			if (_CurrentDownloadStream != null)
			{
				Debug.WriteLine("ダウンロードがすでに実行中のためリクエスト処理をスキップ");
				return false;
			}

			if (_CacheRequestStack.Count == 0)
			{
				return false;
			}

			while (_CacheRequestStack.Count > 0)
			{
				var req = _CacheRequestStack.Last();
				_CacheRequestStack.Remove(req);

				Debug.WriteLine($"{req.RawVideoid}のダウンロードリクエストを処理しています");

				var stream = await CreateDownloadStream(req.RawVideoid, req.Quality, isRequireCache: true);

				if (!stream.IsCacheComplete)
				{
					Debug.WriteLine($"{req.RawVideoid}のダウンロードを開始");

					await AssignDownloadStream(stream);
					break;
				}
				else
				{
					Debug.WriteLine($"{req.RawVideoid}はダウンロード済みのため処理をスキップ");
					stream.Dispose();
				}
			}

			return true;
		}

		private async void DownloadCompleteAction(string rawVideoid)
		{
			if (_CurrentDownloadStream != null &&
				_CurrentDownloadStream.RawVideoId == rawVideoid)
			{
				if (_CurrentDownloadStream != _CurrentPlayingStream)
				{
					_CurrentDownloadStream.Dispose();
				}

				var quality = _CurrentDownloadStream.Quality;
				_CurrentDownloadStream.OnCacheComplete -= DownloadCompleteAction;
				CurrentDownloadStream = null;

				Debug.WriteLine($"{rawVideoid} のダウンロード完了");
				OnCacheCompleted?.Invoke(rawVideoid, quality, true);

				await SaveDownloadRequestItems().ConfigureAwait(false);

				await TryBeginNextDownloadRequest().ConfigureAwait(false);
			}
			else
			{
				throw new Exception("ダウンロードタスクの解除処理が異常");
			}

		}



		private async Task<NicoVideoCachedStream> CreateDownloadStream(string rawVideoid, WatchApiResponse res, ThumbnailResponse thubmnailRes, NicoVideoQuality quality, bool isRequireCache)
		{
			var saveFolder = await _HohoemaApp.GetCurrentUserVideoFolder();
			return await NicoVideoCachedStream.Create(this._HohoemaApp.NiconicoContext.HttpClient, rawVideoid, res, thubmnailRes, saveFolder, quality, isRequireCache);
		}


		private async Task<NicoVideoCachedStream> CreateDownloadStream(string rawVideoid, NicoVideoQuality quality, bool isRequireCache)
		{
			// TODO: オフラインのときのストリーム作成

			var nicoVideo = await _HohoemaApp.MediaManager.GetNicoVideo(rawVideoid);
			WatchApiResponse res;
			if (quality == NicoVideoQuality.Low)
			{
				res = await nicoVideo.GetVideoInfoFromOnline(true);
			}
			else
			{
				res = await nicoVideo.GetVideoInfoFromOnline();
				if (nicoVideo.NowLowQualityOnly && quality == NicoVideoQuality.Original && nicoVideo.OriginalQualityCacheState != NicoVideoCacheState.Cached)
				{
					// ダウンロード再生ができない
					throw new Exception("can not download video with original quality in current time.");
				}
			}

			var thumbnailRes = await nicoVideo.GetThumbnailInfo();

			return await CreateDownloadStream(rawVideoid, res, thumbnailRes, quality, isRequireCache);
		}

		private async Task AssignPlayingStream(NicoVideoCachedStream stream)
		{
			try
			{
				await _StreamControlLock.WaitAsync();

				CurrentPlayingStream = stream;
			}
			finally
			{
				_StreamControlLock.Release();
			}
		}

		private async Task AssignDownloadStream(NicoVideoCachedStream downloadStream)
		{
			try
			{
				await _StreamControlLock.WaitAsync();

				CurrentDownloadStream = downloadStream;
				if (!_CurrentDownloadStream.IsCacheComplete)
				{
					_CurrentDownloadStream.OnCacheComplete += DownloadCompleteAction;
					_CurrentDownloadStream.OnCacheProgress += _CurrentDownloadStream_OnCacheProgress;

					OnCacheStarted?.Invoke(downloadStream.RawVideoId, downloadStream.Quality);
				}

				await _CurrentDownloadStream.Download();
			}
			finally
			{
				_StreamControlLock.Release();
			}
		}

		private void _CurrentDownloadStream_OnCacheProgress(string rawVideoId, NicoVideoQuality quality, uint totalSize, uint size)
		{
			OnCacheProgress?.Invoke(rawVideoId, quality, totalSize, size);
		}

		private async Task PushToTopCurrentDownloadRequest()
		{
			if (_CurrentDownloadStream != null)
			{
				var isReqireCache = _CurrentDownloadStream.IsRequireCache;
				var videoId = _CurrentDownloadStream.RawVideoId;
				var quality = _CurrentDownloadStream.Quality;

				await CloseCurrentDownloadStream();

				if (isReqireCache)
				{
					_CacheRequestStack.Add(new NicoVideoCacheRequest()
					{
						RawVideoid = videoId,
						Quality = quality,
					});
				}
			}
		}
		private void PushDownloadRequest(string rawVideoid, NicoVideoQuality quality)
		{
			var alreadyRequest = _CacheRequestStack.SingleOrDefault(x => x.RawVideoid == rawVideoid);
			if (alreadyRequest != null)
			{
				_CacheRequestStack.Remove(alreadyRequest);
			}
			_CacheRequestStack.Insert(0, new NicoVideoCacheRequest()
			{
				RawVideoid = rawVideoid,
				Quality = quality,
			});
		}


		public async Task Suspending()
		{
			await PushToTopCurrentDownloadRequest();

			await SaveDownloadRequestItems().ConfigureAwait(false);
		}

		public async Task Resume()
		{
			await TryBeginNextDownloadRequest();
		}

		public void Dispose()
		{
			Suspending().ConfigureAwait(false);
		}

		const string DOWNLOAD_QUEUE_FILENAME = "download_queue.json";

		public async Task SaveDownloadRequestItems()
		{
			var videoFolder = await _HohoemaApp.GetCurrentUserVideoFolder();
			if (_CacheRequestStack.Count > 0)
			{
				var file = await videoFolder.CreateFileAsync(DOWNLOAD_QUEUE_FILENAME, CreationCollisionOption.OpenIfExists);
				var jsonText = Newtonsoft.Json.JsonConvert.SerializeObject(_CacheRequestStack.ToArray());
				await FileIO.WriteTextAsync(file, jsonText);

				Debug.WriteLine("ダウンロード待ち状況を保存しました。");
			}
			else if (File.Exists(Path.Combine(videoFolder.Path, DOWNLOAD_QUEUE_FILENAME)))
			{
				var file = await videoFolder.GetFileAsync(DOWNLOAD_QUEUE_FILENAME);
				await file.DeleteAsync();

				Debug.WriteLine("ダウンロード待ちがないため、状況ファイルを削除しました。");
			}
		}

		public async Task<List<NicoVideoCacheRequest>> LoadDownloadRequestItems()
		{

			try
			{
				var videoFolder = await _HohoemaApp.GetCurrentUserVideoFolder();
				if (!File.Exists(Path.Combine(videoFolder.Path, DOWNLOAD_QUEUE_FILENAME)))
				{
					return new List<NicoVideoCacheRequest>();
				}

				var file = await videoFolder.GetFileAsync(DOWNLOAD_QUEUE_FILENAME);
				var jsonText = await FileIO.ReadTextAsync(file);
				return Newtonsoft.Json.JsonConvert.DeserializeObject<NicoVideoCacheRequest[]>(jsonText).ToList();
			}
			catch (FileNotFoundException)
			{
				return new List<NicoVideoCacheRequest>();
			}
		}


		// Donwload/Playingのストリームのアサイン、破棄のタイミング同期用ロック
		private SemaphoreSlim _StreamControlLock;

		public StorageFolder VideoSaveFolder { get; private set; }
		private ObservableCollection<NicoVideoCacheRequest> _CacheRequestStack;
		public ReadOnlyObservableCollection<NicoVideoCacheRequest> CacheRequestStack { get; private set; }

		private NicoVideoCachedStream _CurrentPlayingStream;
		public NicoVideoCachedStream CurrentPlayingStream
		{
			get
			{
				return _CurrentPlayingStream;
			}
			private set
			{
				SetProperty(ref _CurrentPlayingStream, value);
			}
		}

		private NicoVideoCachedStream _CurrentDownloadStream;
		public NicoVideoCachedStream CurrentDownloadStream
		{
			get
			{
				return _CurrentDownloadStream;
			}
			private set
			{
				SetProperty(ref _CurrentDownloadStream, value);
			}
		}

		// TODO: アプリ終了時にDownloadStreamを中断して、_CacheRequestStackに積む
		// TODO: _CacheRequestStackの内容を終了時に未ダウンロードアイテムとしてvideoフォルダに書き出す

		public event CacheStartedHandler OnCacheStarted;
		public event CacheCompleteHandler OnCacheCompleted;
		public event CacheProgressHandler OnCacheProgress;

		HohoemaApp _HohoemaApp;
	}

}
