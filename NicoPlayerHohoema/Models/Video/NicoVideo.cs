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

		internal static async Task<NicoVideo> Create(HohoemaApp app, string rawVideoid, NicoVideoDownloadContext context)
		{
			Debug.WriteLine("start initialize : " + rawVideoid);
			var nicoVideo = new NicoVideo(app, rawVideoid, context);

			await nicoVideo.ThumbnailResponseCache.Update();
			await nicoVideo.WatchApiResponseCache.UpdateFromLocal();

			await nicoVideo.OriginalQuality.SetupDownloadProgress();
			await nicoVideo.LowQuality.SetupDownloadProgress();

			await nicoVideo.CheckCacheStatus();

			return nicoVideo;
		}

		
		

		private NicoVideo(HohoemaApp app, string rawVideoid, NicoVideoDownloadContext context)
		{
			HohoemaApp = app;
			RawVideoId = rawVideoid;
			_Context = context;

			CacheRequestTime = DateTime.MinValue;
			_InterfaceByQuality = new Dictionary<NicoVideoQuality, DividedQualityNicoVideo>();

			ThumbnailResponseCache = new ThumbnailResponseCache(RawVideoId, HohoemaApp, context.VideoSaveFolder, $"{RawVideoId}_thumb.json");
			WatchApiResponseCache = new WatchApiResponseCache(RawVideoId, HohoemaApp, context.VideoSaveFolder, $"{RawVideoId}_info.json");
			CommentResponseCache = new CommentResponseCache(WatchApiResponseCache, HohoemaApp, context.VideoSaveFolder, $"{RawVideoId}_comment.json");
		}



		public async Task CheckCacheStatus()
		{
			var saveFolder = _Context.VideoSaveFolder;

			await OriginalQuality.CheckCacheStatus();
			await LowQuality.CheckCacheStatus();

			
			if (await WatchApiResponseCache.ExistCachedFile())
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

			return NicoVideoCachedStream;
		}


		internal async Task SetupWatchPageVisit(NicoVideoQuality quality)
		{
			WatchApiResponse res;
			if (quality == NicoVideoQuality.Original)
			{
				if (OriginalQuality.IsCached)
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
				if (LowQuality.IsCached)
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
		
		public async Task StopPlay()
		{
			if (NicoVideoCachedStream == null) { return; }

			NicoVideoCachedStream?.Dispose();
			NicoVideoCachedStream = null;

			await _Context.ClosePlayingStream(this.RawVideoId);

			if (NicoVideoDownloader != null)
			{
				await NicoVideoDownloader.DividedQualityNicoVideo.SaveProgress();
				NicoVideoDownloader = null;
			}

			await CheckCacheStatus();
		}

		public Task RequestCache(NicoVideoQuality quality)
		{
			switch (quality)
			{
				case NicoVideoQuality.Original:
					return OriginalQuality.RequestCache();
				case NicoVideoQuality.Low:
					return LowQuality.RequestCache();
				default:
					throw new NotSupportedException(quality.ToString());
			}
		}


		public async Task CancelCacheRequest(NicoVideoQuality? quality = null)
		{
			if (quality == NicoVideoQuality.Original)
			{
				await OriginalQuality.CancelCacheRequest();
			}
			else if (quality == NicoVideoQuality.Low)
			{
				await LowQuality.CancelCacheRequest();
			}
			else
			{
				await CancelCacheRequest(NicoVideoQuality.Low);
				await CancelCacheRequest(NicoVideoQuality.Original);
			}
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


		/// <summary>
		/// キャッシュ要求の基本処理を実行します
		/// DividedQualityNicoVideoから呼び出されます
		/// </summary>
		/// <returns></returns>
		internal async Task OnCacheRequested()
		{
			IfVideoDeletedThrowException();

			await CommentResponseCache.Update();
			await CommentResponseCache.Save();
			await WatchApiResponseCache.Save();
			await ThumbnailResponseCache.Save();
		}



		/// <summary>
		/// キャッシュ削除の後処理を実行します
		/// DividedQualityNicoVideoから呼び出されます
		/// </summary>
		/// <returns></returns>
		internal async Task OnDeleteCache()
		{
			// 動画キャッシュがすべて削除されたらコメントなどの情報も削除
			if (!OriginalQuality.IsCached
				&& !LowQuality.IsCached)
			{
				// jsonファイルを削除
				await WatchApiResponseCache.Delete();
				await ThumbnailResponseCache.Delete();
				await CommentResponseCache.Delete();
			}

			Debug.WriteLine($".完了");
		}



		/// <summary>
		/// 動画削除済みの場合の処理
		/// </summary>
		private async Task DeletedTeardown()
		{
			// コンテキスト内から動画のキャッシュリクエストを削除
			// 古いWatchApiResponseの削除
			// IsDeletedを示すWatchApiResponseの取得
			// WatchApiResponseを.deleteをつけて保存

			// コメントの削除
			// ThumbnailInfoの削除
			var cacheRequested = _Context.CheckCacheRequested(RawVideoId, NicoVideoQuality.Original)
				|| _Context.CheckCacheRequested(RawVideoId, NicoVideoQuality.Low);

			OriginalQuality.DeletedTeardown();
			LowQuality.DeletedTeardown();

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
		}



		


		


		private void IfVideoDeletedThrowException()
		{
			if (IsDeleted) { throw new Exception("video is deleted"); }
		}


		public NGResult CheckUserNGVideo(ThumbnailResponse thumb)
		{
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

		public bool IsDeleted
		{
			get
			{
				return ThumbnailResponseCache.IsDeleted 
					|| (WatchApiResponseCache.CachedItem?.IsDeleted ?? false);
			}
		}


		public MediaProtocolType ProtocolType
		{
			get
			{
				return WatchApiResponseCache.MediaProtocolType;
			}
		}

		public MovieType ContentType
		{
			get
			{
				return ThumbnailResponseCache.MovieType;
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


		public uint VideoOwnerId
		{
			get
			{
				return ThumbnailResponseCache.CachedItem.UserId;
			}
		}


		public bool IsNeedPayment { get; private set; }


		public bool IsRequireConfirmDelete { get; private set; }

		public DateTime CacheRequestTime { get; private set; }

		public HohoemaApp HohoemaApp { get; private set; }
		NicoVideoDownloadContext _Context;

		public bool LastAccessIsLowQuality { get; private set; }


		// 有害動画への対応
		public bool IsBlockedHarmfulVideo
		{
			get
			{
				return WatchApiResponseCache.HasCache ? WatchApiResponseCache.IsBlockedHarmfulVideo : false;
			}
		}

		public HarmfulContentReactionType HarmfulContentReactionType
		{
			get
			{
				return WatchApiResponseCache.HarmfulContentReactionType;
			}
			set
			{
				WatchApiResponseCache.HarmfulContentReactionType = value;
			}
		}


		public ThumbnailResponseCache ThumbnailResponseCache { get; private set; }
		public WatchApiResponseCache WatchApiResponseCache { get; private set; }
		public CommentResponseCache CommentResponseCache { get; private set; }

		public NicoVideoDownloader NicoVideoDownloader { get; private set; }

		public NicoVideoCachedStream NicoVideoCachedStream { get; private set; }


		private Dictionary<NicoVideoQuality, DividedQualityNicoVideo> _InterfaceByQuality;

		private DividedQualityNicoVideo _OriginalQuality;
		public DividedQualityNicoVideo OriginalQuality
		{
			get
			{
				return _OriginalQuality ??
					(_OriginalQuality = GetDividedQualityNicoVideo(NicoVideoQuality.Original));
			}
		}


		private DividedQualityNicoVideo _LowQuality;
		public DividedQualityNicoVideo LowQuality
		{
			get
			{
				return _LowQuality ??
					(_LowQuality = GetDividedQualityNicoVideo(NicoVideoQuality.Low));
			}
		}


		private DividedQualityNicoVideo GetDividedQualityNicoVideo(NicoVideoQuality quality)
		{
			if(!_InterfaceByQuality.ContainsKey(quality))
			{
				switch (quality)
				{
					case NicoVideoQuality.Original:
						_InterfaceByQuality.Add(quality, new OriginalQualityNicoVideo(this, _Context));
						break;
					case NicoVideoQuality.Low:
						_InterfaceByQuality.Add(quality, new LowQualityNicoVideo(this, _Context));
						break;
					default:
						break;
				}
			}

			return _InterfaceByQuality[quality];
		}

	

		public static string MakeVideoFileName(string title, string videoid)
		{
			return $"{title.ToSafeFilePath()} - [{videoid}]";
		}



		public static string GetProgressFileName(string rawVideoId)
		{
			return $"{rawVideoId}_progress";
		}


	}
}
