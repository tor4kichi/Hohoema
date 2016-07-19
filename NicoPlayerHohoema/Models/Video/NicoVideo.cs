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

namespace NicoPlayerHohoema.Models
{
	public class NicoVideo : BindableBase
	{
		internal static async Task<NicoVideo> Create(HohoemaApp app, string rawVideoid, NicoVideoDownloadContext context)
		{
			Debug.WriteLine("start initialize : " + rawVideoid);
			var nicoVideo = new NicoVideo(app, rawVideoid, context);

			await nicoVideo.GetThumbnailInfo();
			await nicoVideo.SetupVideoInfoFromLocal();

			if (!nicoVideo.IsDeleted)
			{
				nicoVideo.VideoId = nicoVideo.CachedThumbnailInfo.Id;
				nicoVideo.Title = nicoVideo.CachedWatchApiResponse?.videoDetail.title ?? nicoVideo.CachedThumbnailInfo.Title;

				await nicoVideo.CheckCacheStatus();
			}
			else
			{
				nicoVideo.VideoId = "";
				nicoVideo.Title = "this video has been deleted.";
				Debug.WriteLine($"{rawVideoid}は削除された動画です");
				//				await nicoVideo.DeleteLowQualityCache();
				//				await nicoVideo.DeleteOriginalQualityCache();
			}



			return nicoVideo;

		}

		private NicoVideo(HohoemaApp app, string rawVideoid, NicoVideoDownloadContext context)
		{
			HohoemaApp = app;
			RawVideoId = rawVideoid;
			_Context = context;

			_ThumbnailInfoFileWriteSemaphore = new SemaphoreSlim(1, 1);
			_VideoInfoFileWriteSemaphore = new SemaphoreSlim(1, 1);
			_CommentFileWriteSemaphore = new SemaphoreSlim(1, 1);

			OriginalQualityCacheState = NicoVideoCacheState.Incomplete;
			LowQualityCacheState = NicoVideoCacheState.Incomplete;

			CacheRequestTime = DateTime.MinValue;
			NowLowQualityOnly = true;

			_WatchApiGettingLock = new SemaphoreSlim(1, 1);
		}



		public async Task CheckCacheStatus()
		{
			// すでにダウンロード済みのキャッシュファイルをチェック
			var saveFolder = _Context.VideoSaveFolder;

			if (NicoVideoCachedStream.ExistOriginalQuorityVideo(Title, VideoId, saveFolder))
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
				OriginalQualityCacheState = NicoVideoCacheState.Incomplete;
			}


			if (NicoVideoCachedStream.ExistLowQuorityVideo(Title, VideoId, saveFolder))
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
				LowQualityCacheState = NicoVideoCacheState.Incomplete;
			}

			OnPropertyChanged(nameof(CanRequestDownloadOriginalQuality));
			OnPropertyChanged(nameof(CanRequestDownloadLowQuality));
			OnPropertyChanged(nameof(CanPlayOriginalQuality));
			OnPropertyChanged(nameof(CanPlayLowQuality));

			if (File.Exists(Path.Combine(saveFolder.Path, $"{RawVideoId}_info.json")))
			{
				var cacheFile = await saveFolder.GetFileAsync($"{RawVideoId}_info.json");
				CacheRequestTime = cacheFile.DateCreated.DateTime;
			}
		}

		// コメントのキャッシュまたはオンラインからの取得と更新
		public async Task<CommentResponse> GetComment(bool requierLatest = false)
		{
			if (_CachedCommentResponse == null || requierLatest)
			{
				var comment = await GetCommentFromOnline();

				if (comment != null)
				{
					_CachedCommentResponse = comment;
				}
			}

			if (_CachedCommentResponse == null)
			{
				_CachedCommentResponse = await GetCommentFromLocal();
			}

			return _CachedCommentResponse;
		}

		public async Task<CommentResponse> GetCommentFromOnline()
		{
			var watchApiResponse = await GetVideoInfo();

			CommentResponse comment = null;
			try
			{
				comment = await ConnectionRetryUtil.TaskWithRetry(async () =>
				{
					return await this.HohoemaApp.NiconicoContext.Video
						.GetCommentAsync(watchApiResponse);
				});
			}
			catch { }

			return comment;
		}

		public async Task<CommentResponse> GetCommentFromLocal()
		{
			var fileName = $"{RawVideoId}_comment.json";
			var saveFolder = _Context.VideoSaveFolder;

			if (!System.IO.File.Exists(Path.Combine(saveFolder.Path, fileName)))
			{
				return null;
			}

			CommentResponse comment = null;
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

			return comment;
		}

		public async Task SaveComment(CommentResponse comment)
		{
			var fileName = $"{RawVideoId}_comment.json";
			var saveFolder = _Context.VideoSaveFolder;

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



		public async Task<ThumbnailResponse> GetThumbnailInfo()
		{
			if (_CachedThumbnailInfo == null || !_IsLatestThumbnailResponse)
			{
				var thumb = await GetThumbnailInfoFromOnline();

				if (thumb != null)
				{
					_CachedThumbnailInfo = thumb;
					_IsLatestThumbnailResponse = true;
				}
			}

			if (_CachedThumbnailInfo == null)
			{
				_CachedThumbnailInfo = await GetThumbnailInfoFromLocal();
				_IsLatestThumbnailResponse = false;
			}

			if (_CachedThumbnailInfo != null)
			{
				LowQualityVideoSize = (uint)_CachedThumbnailInfo.SizeLow;
				OriginalQualityVideoSize = (uint)_CachedThumbnailInfo.SizeHigh;
			}

			return _CachedThumbnailInfo;
		}

		internal async Task<ThumbnailResponse> GetThumbnailInfoFromOnline()
		{
			ThumbnailResponse res = null;

			try
			{
				res = await Util.ConnectionRetryUtil.TaskWithRetry(async () =>
				{
					return await HohoemaApp.NiconicoContext.Video.GetThumbnailAsync(RawVideoId);
				});
			}
			catch (Exception e) when (e.Message.Contains("delete"))
			{
				IsDeleted = true;
			}


			return res;
		}

		public async Task<ThumbnailResponse> GetThumbnailInfoFromLocal()
		{
			// ファイルに保存されたデータから動画情報を再現
			var saveFolder = _Context.VideoSaveFolder;

			ThumbnailResponse res = null;

			var fileName = $"{RawVideoId}_thumb.json";

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
				await _ThumbnailInfoFileWriteSemaphore.WaitAsync();
				jsonText = await FileIO.ReadTextAsync(videoInfoFile);
			}
			finally
			{
				_ThumbnailInfoFileWriteSemaphore.Release();
			}
			res = Newtonsoft.Json.JsonConvert.DeserializeObject<ThumbnailResponse>(jsonText);


			return res;
		}

		public async Task SaveLatestThumbnailInfo()
		{
			var thumb = await GetThumbnailInfoFromOnline();
			if (thumb != null)
			{
				await SaveThumbnailInfo(thumb);
			}
		}

		private async Task SaveThumbnailInfo(ThumbnailResponse res)
		{
			// ファイルに保存されたデータから動画情報を再現
			var saveFolder = _Context.VideoSaveFolder;

			var fileName = $"{RawVideoId}_thumb.json";

			var jsonText = Newtonsoft.Json.JsonConvert.SerializeObject(res);
			var videoInfoFile = await saveFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
			try
			{
				await _ThumbnailInfoFileWriteSemaphore.WaitAsync();

				// 動画情報データをファイルに保存
				await FileIO.WriteTextAsync(videoInfoFile, jsonText);
			}
			finally
			{
				_ThumbnailInfoFileWriteSemaphore.Release();
			}
		}

		// 動画情報のキャッシュまたはオンラインからの取得と更新

		public async Task<WatchApiResponse> GetVideoInfo()
		{
			try
			{
				await _WatchApiGettingLock.WaitAsync();

				if (_CachedWatchApiResponse == null || !_IsLatestWatchApiResponse)
				{
					// オンラインから動画情報を取得
					var res = await GetVideoInfoFromOnline();

					if (res != null)
					{
						_CachedWatchApiResponse = res;
						_IsLatestWatchApiResponse = true;
						NowLowQualityOnly = _CachedWatchApiResponse.VideoUrl.AbsoluteUri.EndsWith("low");

						OnPropertyChanged(nameof(CanRequestDownloadOriginalQuality));
						OnPropertyChanged(nameof(CanPlayOriginalQuality));
					}
				}

				if (_CachedWatchApiResponse == null)
				{
					_CachedWatchApiResponse = await GetVideoInfoFromLocal();
					_IsLatestWatchApiResponse = false;
				}
			}
			finally
			{
				_WatchApiGettingLock.Release();
			}

			return _CachedWatchApiResponse;
		}

		internal async Task<WatchApiResponse> GetVideoInfoFromOnline(bool forceLowQuality = false)
		{
			WatchApiResponse watchApiRes = null;

			Debug.WriteLine($"{RawVideoId}の動画情報を取得 : {DateTime.Now}");

			try
			{
				watchApiRes = await Util.ConnectionRetryUtil.TaskWithRetry(async () =>
				{
					return await HohoemaApp.NiconicoContext.Video.GetWatchApiAsync(RawVideoId, forceLowQuality: forceLowQuality);
				});
			}
			catch { }

			return watchApiRes;
		}

		public async Task SetupVideoInfoFromLocal()
		{
			_CachedWatchApiResponse = await GetVideoInfoFromLocal();
			_IsLatestWatchApiResponse = false;
		}

		public async Task<WatchApiResponse> GetVideoInfoFromLocal()
		{
			var fileName = $"{RawVideoId}_info.json";

			var saveFolder = _Context.VideoSaveFolder;
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

			return Newtonsoft.Json.JsonConvert.DeserializeObject<WatchApiResponse>(jsonText);
		}

		public async Task SaveLatestVideoInfo()
		{
			var watchApiRes = await GetVideoInfoFromOnline();
			await SaveVideoInfo(watchApiRes);
		}

		private async Task SaveVideoInfo(WatchApiResponse watchApiRes)
		{
			var fileName = $"{RawVideoId}_info.json";

			var saveFolder = _Context.VideoSaveFolder;

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


			if (RawVideoId != VideoId)
			{
				var aliasDescriptionFilename = $"{RawVideoId}_real_id_is_[{VideoId}]";
				if (!File.Exists(Path.Combine(saveFolder.Path, aliasDescriptionFilename)))
				{
					try
					{
						await saveFolder.CreateFileAsync(aliasDescriptionFilename, CreationCollisionOption.FailIfExists);
					}
					catch { }
				}
			}
		}








		/// <summary>
		/// 動画ストリームの取得します
		/// </summary>
		/// <param name="cacheMode"></param>
		/// <returns></returns>
		/// <remarks>既にキャッシュ対象に指定されている場合、cacheModel.NoCacheは無視されます。</remarks>
		public async Task<IRandomAccessStream> GetVideoStream(NicoVideoQuality quality)
		{
			return await _Context.GetPlayingStream(RawVideoId, quality);
		}

		// TODO: 
		// 動画のキャッシュ要求
		public async Task RequestCache(NicoVideoQuality quality)
		{
			if (_Context.CheckVideoPlaying(this.RawVideoId, quality))
			{
				_Context.CurrentPlayingStream.ChangeCacheRequire(true);
				return;
			}
			else if (_Context.CheckVideoDownloading(this.RawVideoId, quality))
			{
				return;
			}
			else if (_Context.CheckCacheRequested(this.RawVideoId, quality))
			{
				return;
			}

			if (quality == NicoVideoQuality.Original)
			{
				if (OriginalQualityCacheState == NicoVideoCacheState.Cached) { return; }
			}
			else
			{
				if (LowQualityCacheState == NicoVideoCacheState.Cached) { return; }
			}

			if (CachedWatchApiResponse == null)
			{
				await GetVideoInfo();
			}

			await _Context.RequestDownload(RawVideoId, quality);
			_Context.OnCacheStarted += _Context_OnCacheStarted;
			_Context.OnCacheCompleted += _Context_OnCacheCompleted;
			_Context.OnCacheProgress += _Context_OnCacheProgress;
			await CheckCacheStatus();

			await GetComment(true);
			await SaveComment(CachedCommentResponse);
			await SaveVideoInfo(CachedWatchApiResponse);
			await SaveThumbnailInfo(CachedThumbnailInfo);
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

		private void _Context_OnCacheProgress(string rawVideoId, NicoVideoQuality quality, uint totalSize, uint size)
		{
			if (rawVideoId == RawVideoId)
			{
				switch (quality)
				{
					case NicoVideoQuality.Original:
						OriginalQualityCacheState = NicoVideoCacheState.NowDownloading;
						OriginalQualityCacheProgressSize = size;
						break;
					case NicoVideoQuality.Low:
						LowQualityCacheState = NicoVideoCacheState.NowDownloading;
						LowQualityCacheProgressSize = size;
						break;
					default:
						break;
				}
			}
		}

		private void _Context_OnCacheCompleted(string videoId, NicoVideoQuality quality, bool isSuccess)
		{
			if (videoId == RawVideoId)
			{
				CheckCacheStatus().ConfigureAwait(false);
			}
		}

		public Task StopPlay()
		{
			return _Context.ClosePlayingStream(VideoId);
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
			if (_Context.CheckVideoDownloading(this.RawVideoId, quality))
			{
				await _Context.CacnelDownloadRequest(this.RawVideoId, quality);
			}

			if (quality == NicoVideoQuality.Original)
			{
				await DeleteOriginalQualityCache().ConfigureAwait(false);
			}

			if (quality == NicoVideoQuality.Low)
			{
				await DeleteLowQualityCache().ConfigureAwait(false);
			}

			await CheckCacheStatus();

			if (LowQualityCacheState == NicoVideoCacheState.Incomplete
				&& OriginalQualityCacheState == NicoVideoCacheState.Incomplete)
			{
				// jsonファイルを削除
				var saveFolder = _Context.VideoSaveFolder;
				var files = await saveFolder.GetFilesAsync();
				var deleteTargets = files.Where(x => x.Name.Contains(VideoId))
					.Where(x => x.Name.EndsWith(".json"));

				foreach (var file in deleteTargets)
				{
					await file.DeleteAsync();
				}
			}
		}

		private async Task DeleteOriginalQualityCache()
		{
			var saveFolder = _Context.VideoSaveFolder;
			var files = await saveFolder.GetFilesAsync();
			var deleteTargets = files.Where(x => x.Name.Contains(VideoId))
				.Where(x => x.Name.EndsWith(".mp4") || x.Name.EndsWith(".mp4" + NicoVideoCachedStream.IncompleteExt));
			foreach (var file in deleteTargets)
			{
				await file.DeleteAsync();
			}

		}

		private async Task DeleteLowQualityCache()
		{
			var saveFolder = _Context.VideoSaveFolder;
			var files = await saveFolder.GetFilesAsync();
			var deleteTargets = files.Where(x => x.Name.Contains(VideoId))
				.Where(x => x.Name.Contains(".low.mp4"));
			foreach (var file in deleteTargets)
			{
				await file.DeleteAsync();
			}
		}

		public bool HasOriginalQualityIncompleteVideoFile()
		{
			var saveFolder = _Context.VideoSaveFolder;
			return NicoVideoCachedStream.ExistIncompleteOriginalQuorityVideo(Title, VideoId, saveFolder);
		}

		public bool HasLowQualityIncompleteVideoFile()
		{
			var saveFolder = _Context.VideoSaveFolder;
			return NicoVideoCachedStream.ExistIncompleteLowQuorityVideo(Title, VideoId, saveFolder);
		}


		public IList<TagList> GetTags()
		{
			return CachedWatchApiResponse.videoDetail.tagList;
		}



		public Task<PostCommentResponse> SubmitComment(string comment, TimeSpan position, string commands)
		{
			try
			{
				return HohoemaApp.NiconicoContext.Video.PostCommentAsync(CachedWatchApiResponse, CachedCommentResponse.Thread, comment, position, commands);
			}
			catch
			{
				// コメントデータを再取得してもう一度？
				return Task.FromResult<PostCommentResponse>(null);
			}
		}


		private bool _IsLatestWatchApiResponse;

		private bool _IsLatestThumbnailResponse;

		private CommentResponse _CachedCommentResponse;
		public CommentResponse CachedCommentResponse
		{
			get
			{
				return _CachedCommentResponse;
			}
		}

		private WatchApiResponse _CachedWatchApiResponse;
		public WatchApiResponse CachedWatchApiResponse
		{
			get
			{
				return _CachedWatchApiResponse;
			}
		}

		private ThumbnailResponse _CachedThumbnailInfo;
		public ThumbnailResponse CachedThumbnailInfo
		{
			get
			{
				return _CachedThumbnailInfo;
			}
		}

		private SemaphoreSlim _ThumbnailInfoFileWriteSemaphore;

		private SemaphoreSlim _VideoInfoFileWriteSemaphore;
		private SemaphoreSlim _CommentFileWriteSemaphore;

		public string VideoId { get; private set; }
		public string RawVideoId { get; private set; }

		public string Title { get; private set; }

		public bool IsDeleted { get; private set; }


		private NicoVideoCacheState _OriginalQualityCacheState;
		public NicoVideoCacheState OriginalQualityCacheState
		{
			get { return _OriginalQualityCacheState; }
			set { SetProperty(ref _OriginalQualityCacheState, value); }
		}

		private NicoVideoCacheState _LowQualityCacheState;
		public NicoVideoCacheState LowQualityCacheState
		{
			get { return _LowQualityCacheState; }
			set { SetProperty(ref _LowQualityCacheState, value); }
		}


		private bool _NowLowQualityOnly;
		public bool NowLowQualityOnly
		{
			get { return _NowLowQualityOnly; }
			set { SetProperty(ref _NowLowQualityOnly, value); }
		}


		public bool CanRequestDownloadOriginalQuality
		{
			get { return OriginalQualityCacheState == NicoVideoCacheState.Incomplete && !NowLowQualityOnly && !NowOffline; }
		}

		public bool CanRequestDownloadLowQuality
		{
			get { return LowQualityCacheState == NicoVideoCacheState.Incomplete && !NowOffline && LowQualityVideoSize != 0; }
		}



		public bool CanPlayOriginalQuality
		{
			get { return CanRequestDownloadOriginalQuality || OriginalQualityCacheState == NicoVideoCacheState.Cached; }
		}

		public bool CanPlayLowQuality
		{
			get { return CanRequestDownloadLowQuality || LowQualityCacheState == NicoVideoCacheState.Cached; }
		}

		private uint _LowQualityCacheProgressSize;
		public uint LowQualityCacheProgressSize
		{
			get { return _LowQualityCacheProgressSize; }
			set { SetProperty(ref _LowQualityCacheProgressSize, value); }
		}

		private uint _LowQualityVideoSize;
		public uint LowQualityVideoSize
		{
			get { return _LowQualityVideoSize; }
			set { SetProperty(ref _LowQualityVideoSize, value); }
		}

		private uint _OriginalQualityCacheProgressSize;
		public uint OriginalQualityCacheProgressSize
		{
			get { return _OriginalQualityCacheProgressSize; }
			set { SetProperty(ref _OriginalQualityCacheProgressSize, value); }
		}

		private uint _OriginalQualityVideoSize;
		public uint OriginalQualityVideoSize
		{
			get { return _OriginalQualityVideoSize; }
			set { SetProperty(ref _OriginalQualityVideoSize, value); }
		}

		public bool IsNeedPayment { get; private set; }


		public bool NowOffline { get; private set; }

		public DateTime CacheRequestTime { get; private set; }

		public HohoemaApp HohoemaApp { get; private set; }
		NicoVideoDownloadContext _Context;

		public bool LastAccessIsLowQuality { get; private set; }

		private SemaphoreSlim _WatchApiGettingLock;
	}
}
