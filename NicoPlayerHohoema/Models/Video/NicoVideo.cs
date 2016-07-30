using Mntone.Nico2;
using Mntone.Nico2.Videos.Comment;
using Mntone.Nico2.Videos.Thumbnail;
using Mntone.Nico2.Videos.WatchAPI;
using NicoPlayerHohoema.Util;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.Models
{
	public class NicoVideo : BindableBase
	{
		static CoreDispatcher _Dispatcher;

		static NicoVideo()
		{
			_Dispatcher = Window.Current.Dispatcher;
		}

		internal static async Task<NicoVideo> Create(HohoemaApp app, string rawVideoid, NicoVideoDownloadContext context)
		{
			Debug.WriteLine("start initialize : " + rawVideoid);
			var nicoVideo = new NicoVideo(app, rawVideoid, context);

			await nicoVideo.ThumbnailResponseCache.Update();
			await nicoVideo.WatchApiResponseCache.UpdateFromLocal();

			var progressFileName = NicoVideo.GetProgressFileName(rawVideoid);

			nicoVideo._OriginalQualityDownloadProgressFileAccessor = new FileAccessor<VideoDownloadProgress>(context.VideoSaveFolder, progressFileName + ".json");
			nicoVideo._OriginalQualityProgress = await nicoVideo._OriginalQualityDownloadProgressFileAccessor.Load();
			nicoVideo._LowQualityDownloadProgressFileAccessor = new FileAccessor<VideoDownloadProgress>(context.VideoSaveFolder, progressFileName + ".low.json");
			nicoVideo._LowQualityProgress = await nicoVideo._LowQualityDownloadProgressFileAccessor.Load();

			return nicoVideo;

		}

		
		

		private NicoVideo(HohoemaApp app, string rawVideoid, NicoVideoDownloadContext context)
		{
			HohoemaApp = app;
			RawVideoId = rawVideoid;
			_Context = context;

			OriginalQualityCacheState = null;
			LowQualityCacheState = null;

			CacheRequestTime = DateTime.MinValue;

			ThumbnailResponseCache = new ThumbnailResponseCache(RawVideoId, HohoemaApp, context.VideoSaveFolder, $"{RawVideoId}_thumb.json");
			WatchApiResponseCache = new WatchApiResponseCache(RawVideoId, HohoemaApp, context.VideoSaveFolder, $"{RawVideoId}_info.json");
			CommentResponseCache = new CommentResponseCache(WatchApiResponseCache, HohoemaApp, context.VideoSaveFolder, $"{RawVideoId}_comment.json");
		}



		public async Task CheckCacheStatus()
		{
			var saveFolder = _Context.VideoSaveFolder;


			if (_Context.CheckCacheRequested(this.RawVideoId, NicoVideoQuality.Original))
			{
				// すでにダウンロード済みのキャッシュファイルをチェック
				if (ExistOriginalQuorityVideo(Title, VideoId, saveFolder)
					&& (_OriginalQualityProgress == null || _OriginalQualityProgress.CheckComplete()))
				{
					OriginalQualityCacheState = NicoVideoCacheState.Cached;
				}
				else if (_Context.CheckCacheRequested(this.RawVideoId, NicoVideoQuality.Original))
				{
					OriginalQualityCacheState = NicoVideoCacheState.CacheRequested;
				}
				else if (_Context.CheckVideoDownloading(this.RawVideoId, NicoVideoQuality.Original))
				{
					OriginalQualityCacheState = NicoVideoCacheState.NowDownloading;
				}
				else // if (NicoVideoCachedStream.ExistIncompleteOriginalQuorityVideo(CachedWatchApiResponse, saveFolder))
				{
					OriginalQualityCacheState = null;
				}
			}
			else
			{
				OriginalQualityCacheState = null;
			}


			if (_Context.CheckCacheRequested(this.RawVideoId, NicoVideoQuality.Low))
			{
				if (ExistLowQuorityVideo(Title, VideoId, saveFolder)
					&& (_LowQualityProgress == null || _LowQualityProgress.CheckComplete()))
				{
					LowQualityCacheState = NicoVideoCacheState.Cached;
				}
				else if (_Context.CheckCacheRequested(this.RawVideoId, NicoVideoQuality.Low))
				{
					LowQualityCacheState = NicoVideoCacheState.CacheRequested;
				}
				else if (_Context.CheckVideoDownloading(this.RawVideoId, NicoVideoQuality.Low))
				{
					LowQualityCacheState = NicoVideoCacheState.NowDownloading;
				}
				else // if (NicoVideoCachedStream.ExistIncompleteOriginalQuorityVideo(CachedWatchApiResponse, saveFolder))
				{
					LowQualityCacheState = null;
				}
			}
			else
			{
				LowQualityCacheState = null;
			}


			OnPropertyChanged(nameof(CanRequestDownloadOriginalQuality));
			OnPropertyChanged(nameof(CanRequestDownloadLowQuality));
			OnPropertyChanged(nameof(CanPlayOriginalQuality));
			OnPropertyChanged(nameof(CanPlayLowQuality));

			if (WatchApiResponseCache.ExistCachedFile())
			{
				DateTime time = DateTime.MinValue;
				await WatchApiResponseCache.DoCacheFileAction((file) => 
				{
					time = file.DateCreated.DateTime;
				});
				CacheRequestTime = time;
			}
		}

		// コメントのキャッシュまたはオンラインからの取得と更新
		public Task<CommentResponse> GetCommentResponse(bool requierLatest = false)
		{
			return CommentResponseCache.GetItem(requierLatest);
		}

		

		public async Task<ThumbnailResponse> GetThumbnailResponse()
		{
			var res = await ThumbnailResponseCache.GetItem();

			if (ThumbnailResponseCache.IsDeleted)
			{
				await DeletedTeardown();
			}

			return res;
		}

		

		// 動画情報のキャッシュまたはオンラインからの取得と更新

		public async Task<WatchApiResponse> GetWatchApiResponse(bool requireLatest = false)
		{
			return await WatchApiResponseCache.GetItem(requireLatest);
		}


		



		/// <summary>
		/// 動画ストリームの取得します
		/// 他にダウンロードされているアイテムは強制的に一時停止し、再生終了後に再開されます
		/// </summary>
		public async Task<NicoVideoCachedStream> GetVideoStream(NicoVideoQuality quality)
		{
			IfVideoDeletedThrowException();

			NicoVideoDownloader = await _Context.GetPlayingDownloader(this, quality);

			NicoVideoCachedStream = new NicoVideoCachedStream(NicoVideoDownloader);

			_Context.OnCacheStarted += _Context_OnCacheStarted;
			_Context.OnCacheCompleted += _Context_OnCacheCompleted;
			_Context.OnCacheProgress += _Context_OnCacheProgress;

			return NicoVideoCachedStream;
		}


		internal async Task SetupWatchPageVisit(NicoVideoQuality quality)
		{
			WatchApiResponse res;
			if (quality == NicoVideoQuality.Original)
			{
				if (OriginalQualityCacheState == NicoVideoCacheState.Cached)
				{
					res = await WatchApiResponseCache.GetItem();
				}
				// オリジナル画質の視聴ページにアクセスしたWacthApiResponseを取得する
				else if (WatchApiResponseCache.HasCache
					&& WatchApiResponseCache.VisitedPageType == NicoVideoQuality.Original
					)
				{
					// すでに
					res = WatchApiResponseCache.CachedItem;
				}
				else
				{
					res = await WatchApiResponseCache.GetItem(requireLatest: true);
				}

				// ないです				
				if (WatchApiResponseCache.VisitedPageType == NicoVideoQuality.Low)
				{
					throw new Exception("cant download original quality video.");
				}


			}
			else
			{
				// 低画質動画キャッシュ済みの場合
				if (LowQualityCacheState == NicoVideoCacheState.Cached)
				{
					res = await WatchApiResponseCache.GetItem();
				}
				// 低画質の視聴ページへアクセスしたWatchApiResponseを取得する
				else if (WatchApiResponseCache.HasCache
					&& WatchApiResponseCache.VisitedPageType == NicoVideoQuality.Low
					)
				{
					// すでに
					res = WatchApiResponseCache.CachedItem;
				}
				else
				{
					// まだなので、低画質を指定してアクセスする
					WatchApiResponseCache.OnceSetForceLowQualityForcing();
					res = await WatchApiResponseCache.GetItem(requireLatest: true);
				}
			}

			if (res == null)
			{
				throw new Exception("");
			}

		}

		internal async Task<NicoVideoDownloader> CreateDownloader(NicoVideoQuality quality)
		{

			var watchApiRes = WatchApiResponseCache.CachedItem;
			var thumbnailRes = ThumbnailResponseCache.CachedItem;
			var videoSaveFolder = _Context.VideoSaveFolder;

			// fileはincompleteか
			var videoId = watchApiRes.videoDetail.id;
			var videoTitle = watchApiRes.videoDetail.title.ToSafeFilePath();
			var videoFileName = MakeVideoFileName(videoTitle, videoId);

			var fileName = quality == NicoVideoQuality.Original ? $"{videoFileName}.mp4" : $"{videoFileName}.low.mp4";

			var dirInfo = new DirectoryInfo(videoSaveFolder.Path);

			StorageFile videoFile = null;

			if (quality == NicoVideoQuality.Original && WatchApiResponseCache.NowLowQualityOnly)
			{
				throw new Exception("エコノミーモードのためオリジナル画質の動画がダウンロードできません。");
			}

			// プログレスファイルのアクセサ
			var size = quality == NicoVideoQuality.Original ? OriginalQualityVideoSize : LowQualityVideoSize;
			if (videoSaveFolder.ExistFile(fileName))
			{
				videoFile = await videoSaveFolder.GetFileAsync(fileName);
				if (quality == NicoVideoQuality.Original)
				{
					if (_OriginalQualityProgress == null)
					{
						_OriginalQualityProgress = new VideoDownloadProgress(size);
						_OriginalQualityProgress.Update(0, size);
					}
				}
				else
				{
					if (_LowQualityProgress == null)
					{
						_LowQualityProgress = new VideoDownloadProgress(size);
						_LowQualityProgress.Update(0, size);
					}
				}

			}

			if (videoFile == null)
			{
				videoFile = await videoSaveFolder.CreateFileAsync($"{fileName}", CreationCollisionOption.ReplaceExisting);

				if (quality == NicoVideoQuality.Original)
				{
					_OriginalQualityProgress = new VideoDownloadProgress(size);
					await _OriginalQualityDownloadProgressFileAccessor.Save(_OriginalQualityProgress);
				}
				else
				{
					_LowQualityProgress = new VideoDownloadProgress(size);
					await _LowQualityDownloadProgressFileAccessor.Save(_LowQualityProgress);
				}

			}

			var progress = (quality == NicoVideoQuality.Original ? _OriginalQualityProgress : _LowQualityProgress);

			var stream = new NicoVideoDownloader(HohoemaApp.NiconicoContext.HttpClient, RawVideoId, quality, watchApiRes, thumbnailRes, progress, videoFile);

			System.Diagnostics.Debug.WriteLine($"size:{stream.Size}");

			if (quality == NicoVideoQuality.Original)
			{
				OriginalQualityCacheProgressSize = _OriginalQualityProgress.BufferedSize();
			}
			else
			{
				LowQualityCacheProgressSize = _LowQualityProgress.BufferedSize();
			}

			return stream;
		}

		public async Task StopPlay()
		{
			if (NicoVideoCachedStream == null) { return; }

			_Context.OnCacheStarted -= _Context_OnCacheStarted;
			_Context.OnCacheCompleted -= _Context_OnCacheCompleted;
			_Context.OnCacheProgress -= _Context_OnCacheProgress;

			NicoVideoCachedStream?.Dispose();
			NicoVideoCachedStream = null;

			await _Context.ClosePlayingStream(this.RawVideoId);

			if (NicoVideoDownloader.Quality == NicoVideoQuality.Original)
			{
				await _OriginalQualityDownloadProgressFileAccessor.Save(_OriginalQualityProgress);
			}
			else
			{
				await _LowQualityDownloadProgressFileAccessor.Save(_LowQualityProgress);
			}

			NicoVideoDownloader = null;

			await CheckCacheStatus();
		}

		public async Task<PostCommentResponse> SubmitComment(string comment, TimeSpan position, string commands)
		{
			var commentRes = await CommentResponseCache.GetItem();
			var watchApiRes = await WatchApiResponseCache.GetItem();

			try
			{
				return await HohoemaApp.NiconicoContext.Video.PostCommentAsync(watchApiRes, commentRes.Thread, comment, position, commands);
			}
			catch
			{
				// コメントデータを再取得してもう一度？
				return null;
			}
		}


		
		// TODO: 
		// 動画のキャッシュ要求
		public async Task RequestCache(NicoVideoQuality quality)
		{
			IfVideoDeletedThrowException();

			if (_Context.CheckCacheRequested(this.RawVideoId, quality))
			{
				return;
			}

			_Context.OnCacheStarted += _Context_OnCacheStarted;
			_Context.OnCacheCompleted += _Context_OnCacheCompleted;
			_Context.OnCacheProgress += _Context_OnCacheProgress;

			if (false == await _Context.RequestDownload(RawVideoId, quality))
			{
				_Context.OnCacheStarted -= _Context_OnCacheStarted;
				_Context.OnCacheCompleted -= _Context_OnCacheCompleted;
				_Context.OnCacheProgress -= _Context_OnCacheProgress;
			}

			await CheckCacheStatus();

			await CommentResponseCache.Update();
			await CommentResponseCache.Save();
			await WatchApiResponseCache.Save();
			await ThumbnailResponseCache.Save();
		}

		private void _Context_OnCacheStarted(string rawVideoId, NicoVideoQuality quality)
		{
			if (RawVideoId == rawVideoId)
			{
				switch (quality)
				{
					case NicoVideoQuality.Original:
						OriginalQualityCacheState = NicoVideoCacheState.NowDownloading;
						break;
					case NicoVideoQuality.Low:
						LowQualityCacheState = NicoVideoCacheState.NowDownloading;
						break;
					default:
						break;
				}
			}
		}

		private async void _Context_OnCacheProgress(string rawVideoId, NicoVideoQuality quality, uint totalSize, uint size)
		{
			if (rawVideoId == RawVideoId)
			{
				switch (quality)
				{
					case NicoVideoQuality.Original:
						await _Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => 
						{
							OriginalQualityCacheState = NicoVideoCacheState.NowDownloading;
							OriginalQualityCacheProgressSize = size;
						});
						break;
					case NicoVideoQuality.Low:
						await _Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
						{
							LowQualityCacheState = NicoVideoCacheState.NowDownloading;
							LowQualityCacheProgressSize = size;
						});
						break;
					default:
						break;
				}
			}
		}

		private async void _Context_OnCacheCompleted(string videoId, NicoVideoQuality quality, bool isSuccess)
		{
			if (videoId == RawVideoId)
			{
				await _Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
				{
					if (NicoVideoDownloader != null && NicoVideoDownloader.Quality == quality)
					{
						_OriginalQualityDownloadProgressFileAccessor?.Delete();
					}

					CheckCacheStatus().ConfigureAwait(false);
				});
			}
		}

		

		public void CancelCacheRequest()
		{
			CancelCacheRequest(NicoVideoQuality.Original);
			CancelCacheRequest(NicoVideoQuality.Low);
		}

		public Task CancelCacheRequest(NicoVideoQuality quality)
		{
			return _Context.CacnelDownloadRequest(this.RawVideoId, quality);
		}

		public async Task DeleteCache(NicoVideoQuality quality)
		{
			Debug.Write($"{Title}:{quality.ToString()}のキャッシュを削除開始...");

			// キャッシュリクエストのキャンセル
			if (_Context.CheckVideoDownloading(this.RawVideoId, quality))
			{
				await _Context.CacnelDownloadRequest(this.RawVideoId, quality);
			}

			// 動画ファイルの削除
			if (quality == NicoVideoQuality.Original)
			{
				await DeleteOriginalQualityCache();
			}
			else if (quality == NicoVideoQuality.Low)
			{
				await DeleteLowQualityCache();
			}


			// ダウンロードプログレスの削除
			await DeleteDownloadProgress(quality);

			await CheckCacheStatus();

			// 動画キャッシュがすべて削除されたらコメントなどの情報も削除
			if (LowQualityCacheState == null
				&& OriginalQualityCacheState == null)
			{
				// jsonファイルを削除
				await WatchApiResponseCache.Delete();
				await ThumbnailResponseCache.Delete();
				await CommentResponseCache.Delete();
			}

			Debug.WriteLine($".完了");
		}

		

		private async Task DeleteOriginalQualityCache()
		{
			// 動画ファイルの削除
			var videoId = VideoId;
			var videoTitle = Title.ToSafeFilePath();
			var videoFileName = MakeVideoFileName(videoTitle, videoId);
			var fileName = $"{videoFileName}.mp4";

			var saveFolder = _Context.VideoSaveFolder;
			if (saveFolder.ExistFile(fileName))
			{
				var file = await saveFolder.GetFileAsync(fileName);
				await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
			}
		}

		private async Task DeleteLowQualityCache()
		{
			// 動画ファイルの削除
			var videoId = VideoId;
			var videoTitle = Title.ToSafeFilePath();
			var videoFileName = MakeVideoFileName(videoTitle, videoId);
			var fileName = $"{videoFileName}.low.mp4";

			var saveFolder = _Context.VideoSaveFolder;
			if (saveFolder.ExistFile(fileName))
			{
				var file = await saveFolder.GetFileAsync(fileName);
				await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
			}
		}


		private Task DeleteDownloadProgress(NicoVideoQuality quality)
		{
			// プログレスファイルの削除
			var progressFileName = GetProgressFileName(RawVideoId);
			if (quality == NicoVideoQuality.Original)
			{
				progressFileName += ".json";
			}
			else
			{
				progressFileName += ".low.json";
			}

			var progressFileAccessor = new FileAccessor<VideoDownloadProgress>(_Context.VideoSaveFolder, progressFileName);
			return progressFileAccessor.Delete();
		}






		/// <summary>
		/// 動画削除済みの場合の処理
		/// </summary>
		private async Task DeletedTeardown()
		{
			if (!IsDeleted)
			{
				// コンテキスト内から動画のキャッシュリクエストを削除
				// 古いWatchApiResponseの削除
				// IsDeletedを示すWatchApiResponseの取得
				// WatchApiResponseを.deleteをつけて保存

				// コメントの削除
				// ThumbnailInfoの削除
				var cacheRequested = _Context.CheckCacheRequested(RawVideoId, NicoVideoQuality.Original)
					|| _Context.CheckCacheRequested(RawVideoId, NicoVideoQuality.Low);

				await DeleteCache(NicoVideoQuality.Original);
				await DeleteCache(NicoVideoQuality.Low);

				// キャッシュリクエストがされていた場合はユーザーに伝えるために情報を残す
				if (cacheRequested)
				{
					// GetVideoInfo内で削除済みを示すWatchApiResponseを取得しています。
					// オンラインから動画情報を取得
					await WatchApiResponseCache.Update(requireLatest: true);

					var res = await WatchApiResponseCache.GetItem();

					if (!res.IsDeleted)
					{
						throw new Exception("Thumbnail情報では削除済みを示していますが、動画ページ上では削除されていないとなっています");
					}

					await WatchApiResponseCache.Save();
				}

				IsDeleted = true;
			}
		}



		


		


		private void IfVideoDeletedThrowException()
		{
			if (IsDeleted) { throw new Exception("video is deleted"); }
		}


		public async Task<NGResult> CheckUserNGVideo()
		{
			var thumb = await ThumbnailResponseCache.GetItem();
			return HohoemaApp.UserSettings?.NGSettings.IsNgVideo(thumb);
		}


		public string RawVideoId { get; private set; }


		public string VideoId
		{
			get
			{
				if (ThumbnailResponseCache.HasCache)
				{
					return ThumbnailResponseCache.CachedItem.Id;
				}
				if (WatchApiResponseCache.HasCache)
				{
					return WatchApiResponseCache.CachedItem.videoDetail.id;
				}

				return null;
			}
		}

		public bool IsDeleted { get; private set; }

		private NicoVideoCacheState? _OriginalQualityCacheState;
		public NicoVideoCacheState? OriginalQualityCacheState
		{
			get { return _OriginalQualityCacheState; }
			set { SetProperty(ref _OriginalQualityCacheState, value); }
		}

		private NicoVideoCacheState? _LowQualityCacheState;
		public NicoVideoCacheState? LowQualityCacheState
		{
			get { return _LowQualityCacheState; }
			set { SetProperty(ref _LowQualityCacheState, value); }
		}


		public bool CanRequestDownloadOriginalQuality
		{
			get
			{
				// インターネット繋がってるか
				if (!Util.InternetConnection.IsInternet()) { return false; }

				// キャッシュリクエスト済みじゃないか
				if (OriginalQualityCacheState != null) { return false; }

				// 
				if (ThumbnailResponseCache.IsOriginalQualityOnly)
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

		public bool CanRequestDownloadLowQuality
		{
			get
			{
				// インターネット繋がってるか
				if (!Util.InternetConnection.IsInternet()) { return false; }

				// キャッシュリクエスト済みじゃないか
				if (LowQualityCacheState != null) { return false; }


				if (!ThumbnailResponseCache.HasCache)
				{
					throw new Exception();
				}

				// オリジナル画質しか存在しない動画
				if (ThumbnailResponseCache.IsOriginalQualityOnly)
				{
					return false;
				}

				return true;
			}
		}

		public string Title
		{
			get
			{
				if (ThumbnailResponseCache.HasCache)
				{
					return ThumbnailResponseCache.CachedItem.Title;
				}
				if (WatchApiResponseCache.HasCache)
				{
					return WatchApiResponseCache.CachedItem.videoDetail.title;
				}

				return "";
			}
		}

		public TimeSpan VideoLength
		{
			get
			{
				return ThumbnailResponseCache.CachedItem.Length;
			}
		}

		public bool NowLowQualityOnly
		{
			get
			{
				return WatchApiResponseCache.NowLowQualityOnly;
			}
		}

		public bool IsOriginalQualityOnly
		{
			get
			{
				return ThumbnailResponseCache.IsOriginalQualityOnly;
			}
		}


		public bool CanPlayOriginalQuality
		{
			get
			{
				return CanRequestDownloadOriginalQuality
					|| OriginalQualityCacheState == NicoVideoCacheState.Cached;
			}
		}

		public bool CanPlayLowQuality
		{
			get
			{
				return CanRequestDownloadLowQuality
					|| LowQualityCacheState == NicoVideoCacheState.Cached;
			}
		}

		


		public uint OriginalQualityVideoSize
		{
			get
			{
				return ThumbnailResponseCache.OriginalQualityVideoSize;
			}
		}


		public uint LowQualityVideoSize
		{
			get
			{
				return ThumbnailResponseCache.LowQualityVideoSize;
			}
		}

		public uint VideoOwnerId
		{
			get
			{
				return ThumbnailResponseCache.CachedItem.UserId;
			}
		}


		private uint _LowQualityCacheProgressSize;
		public uint LowQualityCacheProgressSize
		{
			get { return _LowQualityCacheProgressSize; }
			set { SetProperty(ref _LowQualityCacheProgressSize, value); }
		}

		

		private uint _OriginalQualityCacheProgressSize;
		public uint OriginalQualityCacheProgressSize
		{
			get { return _OriginalQualityCacheProgressSize; }
			set { SetProperty(ref _OriginalQualityCacheProgressSize, value); }
		}

		public bool IsNeedPayment { get; private set; }

		public bool IsLowQualityCacheRequested
		{
			get { return _Context.CheckCacheRequested(this.RawVideoId, NicoVideoQuality.Low); }
		}

		public bool IsOriginalQualityCacheRequested
		{
			get { return _Context.CheckCacheRequested(this.RawVideoId, NicoVideoQuality.Original); }
		}


		public bool IsRequireConfirmDelete { get; private set; }

		public bool NowOffline { get; private set; }

		public DateTime CacheRequestTime { get; private set; }

		public HohoemaApp HohoemaApp { get; private set; }
		NicoVideoDownloadContext _Context;

		public bool LastAccessIsLowQuality { get; private set; }


		// 有害動画への対応
		public bool IsBlockedHarmfulVideo { get; private set; }
		public HarmfulContentReactionType HarmfulContentReactionType { get; set; }


		public ThumbnailResponseCache ThumbnailResponseCache { get; private set; }
		public WatchApiResponseCache WatchApiResponseCache { get; private set; }
		public CommentResponseCache CommentResponseCache { get; private set; }

		public NicoVideoDownloader NicoVideoDownloader { get; private set; }

		public NicoVideoCachedStream NicoVideoCachedStream { get; private set; }



		private VideoDownloadProgress _OriginalQualityProgress;
		private FileAccessor<VideoDownloadProgress> _OriginalQualityDownloadProgressFileAccessor;


		private VideoDownloadProgress _LowQualityProgress;
		private FileAccessor<VideoDownloadProgress> _LowQualityDownloadProgressFileAccessor;


		public static bool ExistOriginalQuorityVideo(string title, string videoId, StorageFolder folder)
		{
			return File.Exists(Path.Combine(folder.Path, $"{MakeVideoFileName(title, videoId)}.mp4".ToSafeFilePath()));
		}

		public static bool ExistLowQuorityVideo(string title, string videoId, StorageFolder folder)
		{
			return File.Exists(Path.Combine(folder.Path, $"{MakeVideoFileName(title, videoId)}.low.mp4".ToSafeFilePath()));
		}


		public static string MakeVideoFileName(string title, string videoid)
		{
			return $"{title} - [{videoid}]";
		}



		private static string GetProgressFileName(string rawVideoId)
		{
			return $"{rawVideoId}_progress";
		}


	}
}
