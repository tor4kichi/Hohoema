using Mntone.Nico2.Videos.Comment;
using Mntone.Nico2.Videos.Thumbnail;
using Mntone.Nico2.Videos.WatchAPI;
using NicoPlayerHohoema.Util;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;

namespace NicoPlayerHohoema.Models
{
	/// <summary>
	/// ニコニコ動画の動画やサムネイル画像、
	/// 動画情報など動画に関わるメディアを管理します
	/// </summary>
	public class NiconicoMediaManager : BindableBase, IDisposable
	{
		static internal async Task<NiconicoMediaManager> Create(HohoemaApp app)
		{
			var man = new NiconicoMediaManager(app);
			man.Context = await VideoDownloadContext.Create(app);
			return man;
		}


		private NiconicoMediaManager(HohoemaApp app)
		{
			_HohoemaApp = app;

			VideoIdToThumbnailInfo = new Dictionary<string, ThumbnailResponse>();
		}



		public async Task<ThumbnailResponse> GetThumbnail(string videoId)
		{
			if (VideoIdToThumbnailInfo.ContainsKey(videoId))
			{
				var value = VideoIdToThumbnailInfo[videoId];

				// TODO: サムネイル情報が古い場合は更新する

				return value;
			}
			else
			{
				var thumbnail = await ConnectionRetryUtil.TaskWithRetry(async () =>
					{
						return await _HohoemaApp.NiconicoContext.Video.GetThumbnailAsync(videoId);
					});
				try
				{
					if (!VideoIdToThumbnailInfo.ContainsKey(videoId))
					{
						VideoIdToThumbnailInfo.Add(videoId, thumbnail);
					}
				}
				catch { }

				return thumbnail;
			}
		}


		public static async Task<StorageFolder> GetLocalVideoCacheFolderAsync()
		{
			return await ApplicationData.Current.LocalFolder.CreateFolderAsync("video", CreationCollisionOption.OpenIfExists);
		}

		public async Task DownloadVideoAsync(string videoId)
		{
			var saveFolder = await GetLocalVideoCacheFolderAsync();


			var videoInfo = await _HohoemaApp.NiconicoContext.Video.GetWatchApiAsync(videoId, false);
			var file = await saveFolder.CreateFileAsync(videoInfo.videoDetail.title + ".mp4", CreationCollisionOption.ReplaceExisting);

			System.Diagnostics.Debug.WriteLine($"{videoInfo.videoDetail.title}のダウンロードを開始します。");
			System.Diagnostics.Debug.WriteLine($"{videoInfo.VideoUrl}");



			var downloadTask = DownloadVideo(file, videoInfo.VideoUrl);
			downloadTask.Progress = (progress, current) => 
			{
				var parcent = (float)current.Progress / current.VideoSize * 100.0f;
				System.Diagnostics.Debug.WriteLine($"{current.Progress}/{current.VideoSize}({parcent:0.##})%");

				if (current.VideoSize == current.Progress)
				{
					System.Diagnostics.Debug.WriteLine("done poi");
				}

				_HohoemaApp.NiconicoContext.User.GetInfoAsync();
			};

			downloadTask.Completed = (x, y) => 
			{
				System.Diagnostics.Debug.WriteLine("download done.");

				if (downloadTask.ErrorCode != null)
				{
					System.Diagnostics.Debug.WriteLine(downloadTask.ErrorCode.Message);
				}
			};
		}

		
		public async Task<NicoVideo> CreateNicoVideoAccessor(string videoId)
		{
			return await NicoVideo.Create(_HohoemaApp, videoId, Context);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="file"></param>
		/// <param name="videoId"></param>
		/// <returns>progress max is 1000</returns>
		internal IAsyncActionWithProgress<VideoDownloadProgressEventArgs> DownloadVideo(StorageFile file, Uri videoUri)
		{ 

			return AsyncInfo.Run<VideoDownloadProgressEventArgs>(async (token, task) =>
			{
				var stream = await Util.HttpRandomAccessStream.CreateAsync(_HohoemaApp.NiconicoContext.HttpClient, videoUri);

				var arg = new VideoDownloadProgressEventArgs();


				using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
				{
					var buffer = new byte[262144].AsBuffer();

					arg.VideoSize = (uint)stream.Size;

					
					while (stream.Position < arg.VideoSize)
					{
						var downloadSize = (uint) Math.Min(buffer.Capacity, stream.Size);
						await stream.ReadAsync(buffer, downloadSize, Windows.Storage.Streams.InputStreamOptions.None);

						await fileStream.WriteAsync(buffer);

						arg.Progress = (uint)stream.Position;

						task.Report(arg);

						await Task.Delay(1000);
					}
				}
			});
		}



		public void Dispose()
		{
			
		}

		public Dictionary<string, ThumbnailResponse> VideoIdToThumbnailInfo { get; private set; }

		public VideoDownloadContext Context { get; private set; }
		HohoemaApp _HohoemaApp;
	}


	public delegate void VideoDownloadProgressEvent(VideoDownloadProgressEventArgs args);

	public class VideoDownloadProgressEventArgs
	{
		public uint VideoSize { get; set; }
		public uint Progress { get; set; }
	}

	public sealed class VideoDownloadContext : BindableBase, IDisposable
	{
		static internal async Task<VideoDownloadContext> Create(HohoemaApp hohoemaApp)
		{
			var context = new VideoDownloadContext(hohoemaApp);

			var list = await VideoDownloadContext.LoadDownloadRequestItems().ConfigureAwait(false);
			foreach (var req in list)
			{
				context._CacheRequestStack.Add(req);
			}

			await context.TryBeginNextDownloadRequest().ConfigureAwait(false);

			return context;
		}


		

		private VideoDownloadContext(HohoemaApp hohoemaApp)
		{
			_HohoemaApp = hohoemaApp;
			_CacheRequestStack = new ObservableCollection<NicoVideoCacheRequest>();

			_CurrentPlayingStream = null;
			_CurrentDownloadStream = null;
		}

		public async Task<NicoVideoCachedStream> GetPlayingStream(WatchApiResponse res, NicoVideoCacheMode cacheMode = NicoVideoCacheMode.Auto)
		{
			CloseCurrentPlayingStream();

			// 再生用のストリームを取得します
			// すでに開始されたダウンロードタスクは一旦中断し、再生用ストリームに回線を明け渡します。
			// 中断されたダウンロードタスクは、ダウンロードスタックに積み、再生用ストリームのダウンロード完了を確認して
			// ダウンロードを再開させます。		

			// 再生用対象をダウンロード中の場合は
			// キャッシュモードを無視してダウンロード中のストリームをそのまま帰す
			if (_CurrentDownloadStream != null && 
				_CurrentDownloadStream.VideoId == res.videoDetail.id)
			{
				if (cacheMode == NicoVideoCacheMode.Low &&
					_CurrentDownloadStream.CacheMode != NicoVideoCacheMode.Low)
				{

				}
				

				_CurrentPlayingStream = _CurrentDownloadStream;
			}
			else
			{
				var stream = await CreatePlayingStream(res, cacheMode);

				_CurrentPlayingStream = stream;
			}

			return _CurrentPlayingStream;
		}

		

		public async Task RequestDownload(WatchApiResponse res, NicoVideoQuority quority)
		{
			PushDownloadRequest(res.videoDetail.id, quority == NicoVideoQuority.Low ? NicoVideoCacheMode.Low : NicoVideoCacheMode.Original);

			await TryBeginNextDownloadRequest();
		}

		public void ClosePlayingStream(string videoId)
		{
			if (_CurrentPlayingStream != null &&
				_CurrentPlayingStream.VideoId == videoId)
			{
				CloseCurrentPlayingStream();
			}
		}

		private void CloseCurrentPlayingStream()
		{
			if (_CurrentPlayingStream != _CurrentDownloadStream)
			{
				_CurrentPlayingStream?.Dispose();
			}

			_CurrentPlayingStream = null;
		}

		private async Task<bool> TryBeginNextDownloadRequest()
		{
			if (_CurrentDownloadStream != null)
			{
				return false;
			}

			if (_CacheRequestStack.Count == 0)
			{
				return false;
			}

			while(_CacheRequestStack.Count > 0)
			{
				var req = _CacheRequestStack.Last();
				_CacheRequestStack.Remove(req);

				var stream = await CreateDownloadStream(req.VideoId, req.CacheMode);

				if (!stream.IsCacheComplete)
				{
					_CurrentDownloadStream = stream;
					_CurrentDownloadStream.OnCacheComplete += DownloadCompleteAction;
					_CurrentDownloadStream.Download();
					break;
				}
				else
				{
					stream.Dispose();
				}
			}

			return true;
		}

		private void DownloadCompleteAction(string videoId)
		{
			if (_CurrentDownloadStream != null &&
				_CurrentDownloadStream.VideoId == videoId)
			{
				_CurrentDownloadStream.OnCacheComplete -= DownloadCompleteAction;
				_CurrentDownloadStream = null;
			}
		}

		private async Task <NicoVideoCachedStream> CreatePlayingStream(WatchApiResponse res, NicoVideoCacheMode cacheMode)
		{
			if (_CurrentDownloadStream != null)
			{
				PushToTopCurrentDownloadRequest();
			}

			var stream = await CreateDownloadStream(res, cacheMode);

			if (!stream.IsCacheComplete)
			{
				_CurrentDownloadStream = stream;
			}
			else
			{
				// call NextDownload 
				await TryBeginNextDownloadRequest();
			}

			return stream;
		}


		private async Task<NicoVideoCachedStream> CreateDownloadStream(WatchApiResponse res, NicoVideoCacheMode cacheMode)
		{
			var saveFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("video", CreationCollisionOption.OpenIfExists);
			return  await NicoVideoCachedStream.Create(this._HohoemaApp.NiconicoContext.HttpClient, res, saveFolder, cacheMode);
		}


		private async Task<NicoVideoCachedStream> CreateDownloadStream(string videoId, NicoVideoCacheMode cacheMode)
		{
			var res = await _HohoemaApp.NiconicoContext.Video.GetWatchApiAsync(videoId, cacheMode == NicoVideoCacheMode.Low);
			return await CreateDownloadStream(res, cacheMode);
		}


		private void PushToTopCurrentDownloadRequest()
		{
			if (_CurrentDownloadStream != null)
			{
				_CacheRequestStack.Add(new NicoVideoCacheRequest()
				{
					VideoId = _CurrentDownloadStream.VideoId,
					CacheMode = _CurrentDownloadStream.CacheMode,
				});

				_CurrentDownloadStream.Dispose();
				_CurrentDownloadStream = null;
			}
		}
		private void PushDownloadRequest(string videoId, NicoVideoCacheMode mode)
		{
			var alreadyRequest = _CacheRequestStack.SingleOrDefault(x => x.VideoId == videoId);
			if (alreadyRequest != null)
			{
				_CacheRequestStack.Remove(alreadyRequest);
			}

			_CacheRequestStack.Insert(0, new NicoVideoCacheRequest()
			{
				VideoId = videoId,
				CacheMode = mode,
			});
		}

		public void Dispose()
		{
			PushToTopCurrentDownloadRequest();

			SaveDownloadRequestItems().ConfigureAwait(false);
		}

		const string DOWNLOAD_QUEUE_FILENAME = "download_queue.json";

		public async Task SaveDownloadRequestItems()
		{
			var videoFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("video");
			var file = await videoFolder.CreateFileAsync(DOWNLOAD_QUEUE_FILENAME);
			var jsonText = Newtonsoft.Json.JsonConvert.SerializeObject(_CacheRequestStack.ToArray());
			await FileIO.WriteTextAsync(file, jsonText);
		}

		public static async Task<List<NicoVideoCacheRequest>> LoadDownloadRequestItems()
		{

			try
			{
				var videoFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("video");
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

		private ObservableCollection<NicoVideoCacheRequest> _CacheRequestStack;
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

		public event CacheCompleteHandler OnCacheCompleted;


		HohoemaApp _HohoemaApp;
	}

	public delegate void CacheCompleteHandler(string videoId, bool isSuccess);


	public class NicoVideoCacheRequest
	{
		public string VideoId { get; set; }
		public NicoVideoCacheMode CacheMode { get; set; }
	}

	public class NicoVideo : BindableBase
	{
		internal static async Task<NicoVideo> Create(HohoemaApp app, string videoId, VideoDownloadContext context)
		{
			var nicoVideo = new NicoVideo(app, videoId, context);

			await nicoVideo.CheckCache();

			return nicoVideo;

		}

		private NicoVideo(HohoemaApp app, string videoId, VideoDownloadContext context)
		{
			HohoemaApp = app;
			VideoId = videoId;
			_Context = context;

			_VideoInfoFileWriteSemaphore = new SemaphoreSlim(1, 1);
			_CommentFileWriteSemaphore = new SemaphoreSlim(1, 1);

			OriginalQualityCacheState = NicoVideoCacheState.CanDownload;
			LowQualityCacheState = NicoVideoCacheState.CanDownload;
		}


		public async Task CheckCache()
		{
			// すでにダウンロード済みのキャッシュファイルをチェック
			var saveFolder = await NiconicoMediaManager.GetLocalVideoCacheFolderAsync();

			var watchApiRes = await GetVideoInfo();

			if (NicoVideoCachedStream.ExistOriginalQuorityVideo(watchApiRes, saveFolder))
			{
				OriginalQualityCacheState = NicoVideoCacheState.Cached;
			}

			if (NicoVideoCachedStream.ExistLowQuorityVideo(watchApiRes, saveFolder))
			{
				LowQualityCacheState = NicoVideoCacheState.Cached;
			}

			// 未ダウンロードのオリジナル・エコノミーがダウンロード可能か
			// WatchApiResponseのURLから判別する？
			// lowを削ればダウンロード自体はできる？

		}

		// コメントのキャッシュまたはオンラインからの取得と更新
		public async Task<CommentResponse> GetComment()
		{
			var response = await GetVideoInfo();
			var fileName = $"{VideoId}_comment.json";

			CommentResponse comment = null;
			try
			{
				comment = await this.HohoemaApp.NiconicoContext.Video
					.GetCommentAsync(response);
			}
			catch { }

			var saveFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("video", CreationCollisionOption.OpenIfExists);
			if (comment != null)
			{
				var jsonText = Newtonsoft.Json.JsonConvert.SerializeObject(comment);
				var commentFile = await saveFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
				// コメントデータをファイルに保存
				try
				{
					await _CommentFileWriteSemaphore.WaitAsync();
					await FileIO.WriteTextAsync(commentFile, jsonText);
				}
				finally
				{
					_CommentFileWriteSemaphore.Release();
				}
			}
			else
			{ 
				// オンラインからコメントの取得に失敗
				// ファイルから取得を試みます

				// ファイルに保存されたデータからコメントを再現

				// ファイルが存在するか
				if (!System.IO.File.Exists(Path.Combine(saveFolder.Path, fileName)))
				{
					return null;
				}

				StorageFile commentFile;
				commentFile = await saveFolder.GetFileAsync(fileName);
				string jsonText;
				try
				{
					await _CommentFileWriteSemaphore.WaitAsync();
					jsonText = await FileIO.ReadTextAsync(commentFile);
				}
				finally
				{
					_CommentFileWriteSemaphore.Release();
				}

				comment = Newtonsoft.Json.JsonConvert.DeserializeObject<CommentResponse>(jsonText);
			}

			return comment;
		}

		// 動画情報のキャッシュまたはオンラインからの取得と更新
		public async Task<WatchApiResponse> GetVideoInfo(bool forceLowQuality = false)
		{
			if (_CachedWatchApiResponse != null)
			{
				return _CachedWatchApiResponse;
			}

			// ファイルに保存されたデータから動画情報を再現
			var saveFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("video", CreationCollisionOption.OpenIfExists);

			WatchApiResponse watchApiRes = null;

			var fileName = $"{VideoId}_info.json";

			try
			{
				watchApiRes = await HohoemaApp.NiconicoContext.Video.GetWatchApiAsync(VideoId, forceLowQuality);
			}
			catch { }

			if (watchApiRes != null)
			{
				var jsonText = Newtonsoft.Json.JsonConvert.SerializeObject(watchApiRes);
				var videoInfoFile = await saveFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
				try
				{
					await _VideoInfoFileWriteSemaphore.WaitAsync();

					// 動画情報データをファイルに保存
					await FileIO.WriteTextAsync(videoInfoFile, jsonText);
				}
				finally
				{
					_VideoInfoFileWriteSemaphore.Release();
				}
			}
			else 
			{
				// ファイルが存在するか
				if (!System.IO.File.Exists(Path.Combine(saveFolder.Path, fileName)))
				{
					return null;
				}

				StorageFile videoInfoFile;
				videoInfoFile = await saveFolder.GetFileAsync(fileName);
				string jsonText;
				try
				{
					await _VideoInfoFileWriteSemaphore.WaitAsync();
					jsonText = await FileIO.ReadTextAsync(videoInfoFile);
				}
				finally
				{
					_VideoInfoFileWriteSemaphore.Release();
				}
				watchApiRes = Newtonsoft.Json.JsonConvert.DeserializeObject<WatchApiResponse>(jsonText);
			}

			_CachedWatchApiResponse = watchApiRes;

			return watchApiRes;
		}


		/// <summary>
		/// 動画ストリームの取得します
		/// </summary>
		/// <param name="cacheMode"></param>
		/// <returns></returns>
		/// <remarks>既にキャッシュ対象に指定されている場合、cacheModel.NoCacheは無視されます。</remarks>
		public async Task<IRandomAccessStream> GetVideoStream(NicoVideoCacheMode cacheMode = NicoVideoCacheMode.NoCache)
		{
			var response = await GetVideoInfo();
			return await _Context.GetPlayingStream(response, cacheMode);
		}


		// 動画のキャッシュ要求
		public async Task RequestCache(NicoVideoQuority quority)
		{
			var response = await GetVideoInfo(quority == NicoVideoQuority.Low);
			await _Context.RequestDownload(response, quority);
			_Context.OnCacheCompleted += _Context_OnCacheCompleted;
			
		}

		private void _Context_OnCacheCompleted(string videoId, bool isSuccess)
		{
			if (videoId == VideoId)
			{
				LowQualityCacheState = NicoVideoCacheState.CanDownload;
				OriginalQualityCacheState = NicoVideoCacheState.CanDownload;
				CheckCache();
			}
		}

		public void StopPlay()
		{
			_Context.ClosePlayingStream(VideoId);
		}


		private bool _IsCacheRequested;
		public bool IsCacheRequested
		{
			get
			{
				return _IsCacheRequested;
			}
			set
			{
				SetProperty(ref _IsCacheRequested, value);
			}
		}

		private WatchApiResponse _CachedWatchApiResponse;

		private SemaphoreSlim _VideoInfoFileWriteSemaphore;
		private SemaphoreSlim _CommentFileWriteSemaphore;

		public string VideoId { get; private set; }

		public NicoVideoCacheState OriginalQualityCacheState;
		public NicoVideoCacheState LowQualityCacheState;

		public HohoemaApp HohoemaApp { get; private set; }
		VideoDownloadContext _Context;
	}

	public enum NicoVideoCacheState
	{
		CanDownload,
		NowDownloading,
		Cached,
	}

	public enum NicoVideoQuority
	{
		Original,
		Low,
	}
}
