using Mntone.Nico2;
using Mntone.Nico2.Videos.Comment;
using Mntone.Nico2.Videos.Thumbnail;
using Mntone.Nico2.Videos.WatchAPI;
using NicoPlayerHohoema.Models.Db;
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

			await nicoVideo.Initialize();

			return nicoVideo;
		}


		private CommentResponse _CachedCommentResponse;
		private NicoVideoQuality _VisitedPageType;

		internal WatchApiResponse CachedWatchApiResponse { get; private set; }


		private NicoVideo(HohoemaApp app, string rawVideoid, NicoVideoDownloadContext context)
		{
			HohoemaApp = app;
			RawVideoId = rawVideoid;
			_Context = context;

			CacheRequestTime = DateTime.MinValue;
			_InterfaceByQuality = new Dictionary<NicoVideoQuality, DividedQualityNicoVideo>();
		}


		private async Task Initialize()
		{
			Info = await VideoInfoDb.GetEnsureNicoVideoInfoAsync(RawVideoId);

			if (Info.IsDeleted) { return; }

			if (Util.InternetConnection.IsInternet())
			{
				await UpdateWithThumbnail();

				Info = await VideoInfoDb.GetEnsureNicoVideoInfoAsync(RawVideoId);
			}

			if (false == Info.IsDeleted)
			{
				await OriginalQuality.SetupDownloadProgress();
				await LowQuality.SetupDownloadProgress();

				await CheckCacheStatus();
			}
		}


		public async Task CheckCacheStatus()
		{
			var saveFolder = _Context.VideoSaveFolder;

			await OriginalQuality.CheckCacheStatus();
			await LowQuality.CheckCacheStatus();
		}

		// コメントのキャッシュまたはオンラインからの取得と更新
		public async Task<CommentResponse> GetCommentResponse(bool requierLatest = false)
		{
			if (CachedWatchApiResponse == null)
			{
				throw new Exception("コメントを取得するには先にWatchPageへのアクセスが必要です");
			}

			CommentResponse commentRes = null;
			try
			{
				commentRes = await ConnectionRetryUtil.TaskWithRetry(async () =>
				{
					return await this.HohoemaApp.NiconicoContext.Video
						.GetCommentAsync(CachedWatchApiResponse);
				});

				if (commentRes != null)
				{
					_CachedCommentResponse = commentRes;
				}

				return _CachedCommentResponse;
			}
			catch
			{
				return _CachedCommentResponse;
			}
		}

		

		private Task UpdateWithThumbnail()
		{
			// 動画のサムネイル情報にアクセスさせて、アプリ内部DBを更新
			return HohoemaApp.ContentFinder.GetThumbnailResponse(RawVideoId);
		}

		

		// 動画情報のキャッシュまたはオンラインからの取得と更新

		public Task VisitWatchPage(bool forceLowQuality = false)
		{
			return GetWatchApiResponse(forceLowQuality);
		}

		private async Task<WatchApiResponse> GetWatchApiResponse(bool forceLoqQuality = false)
		{
			WatchApiResponse watchApiRes = null;


			try
			{
				watchApiRes = await HohoemaApp.ContentFinder.GetWatchApiResponse(RawVideoId, forceLoqQuality, HarmfulContentReactionType);
			}
			catch (AggregateException ea) when (ea.Flatten().InnerExceptions.Any(e => e is ContentZoningException))
			{
				IsBlockedHarmfulVideo = true;
			}
			catch (ContentZoningException)
			{
				IsBlockedHarmfulVideo = true;
			}
			catch { }

			if (!forceLoqQuality && watchApiRes != null)
			{
				NowLowQualityOnly = watchApiRes.VideoUrl.AbsoluteUri.EndsWith("low");
			}

			if (watchApiRes != null)
			{
				CachedWatchApiResponse = watchApiRes;

				ProtocolType = MediaProtocolTypeHelper.ParseMediaProtocolType(watchApiRes.VideoUrl);
			}

			Info = await VideoInfoDb.GetAsync(RawVideoId);

			return watchApiRes;
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
					res = await GetWatchApiResponse();
				}
				// オリジナル画質の視聴ページにアクセスしたWacthApiResponseを取得する
				else if (CachedWatchApiResponse != null
					&& _VisitedPageType == NicoVideoQuality.Original
					)
				{
					// すでに
					res = CachedWatchApiResponse;
				}
				else
				{
					res = await GetWatchApiResponse();
				}

				// ないです				
				if (_VisitedPageType == NicoVideoQuality.Low)
				{
					throw new Exception("cant download original quality video.");
				}
			}
			else
			{
				// 低画質動画キャッシュ済みの場合
				if (LowQuality.IsCached)
				{
					res = await GetWatchApiResponse();
				}
				// 低画質の視聴ページへアクセスしたWatchApiResponseを取得する
				else if (CachedWatchApiResponse != null
					&& _VisitedPageType == NicoVideoQuality.Low
					)
				{
					// すでに
					res = CachedWatchApiResponse;
				}
				else
				{
					// まだなので、低画質を指定してアクセスする
					res = await GetWatchApiResponse( forceLoqQuality:true );
				}
			}

			if (res == null)
			{
				throw new Exception("");
			}


			// キャッシュリクエストされている場合このタイミングでコメントを取得
			if (_Context.CheckCacheRequested(RawVideoId, quality))
			{
				var commentRes = await GetCommentResponse();
				CommentDb.AddOrUpdate(RawVideoId, commentRes);
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
			var commentRes = _CachedCommentResponse;
			var watchApiRes = CachedWatchApiResponse;

			if (commentRes == null && watchApiRes == null)
			{
				throw new Exception("コメント投稿には事前に動画ページへのアクセスとコメント情報取得が必要です");
			}

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

			if (CachedWatchApiResponse != null)
			{
				var commentRes = await GetCommentResponse();
				CommentDb.AddOrUpdate(RawVideoId, commentRes);
			}
		}



		/// <summary>
		/// 動画削除済みの場合の処理
		/// </summary>
		private async Task DeletedTeardown()
		{
			// キャッシュした動画データと動画コメントの削除

			await OriginalQuality.DeletedTeardown();
			await LowQuality.DeletedTeardown();

			CommentDb.Remove(RawVideoId);
		}



		


		


		private void IfVideoDeletedThrowException()
		{
			if (IsDeleted) { throw new Exception("video is deleted"); }
		}


		public NGResult CheckUserNGVideo(NicoVideoInfo info)
		{
			return HohoemaApp.UserSettings?.NGSettings.IsNgVideo(info);
		}


		public string RawVideoId { get; private set; }

		public string ThreadId { get; private set; }

		public string VideoId => Info.VideoId;

		public bool IsDeleted => Info.IsDeleted;


		public MediaProtocolType ProtocolType { get; private set; }

		public MovieType ContentType => Info.MovieType;



		public string Title => Info.Title;

		public TimeSpan VideoLength => Info.Length;

		public bool NowLowQualityOnly { get; private set; }

		public bool IsOriginalQualityOnly => Info.LowSize == 0;


		public uint VideoOwnerId => Info.UserId;


		public bool IsNeedPayment { get; private set; }


		public bool IsRequireConfirmDelete { get; private set; }

		public DateTime CacheRequestTime { get; private set; }

		public HohoemaApp HohoemaApp { get; private set; }
		NicoVideoDownloadContext _Context;

		public bool LastAccessIsLowQuality { get; private set; }


		// 有害動画への対応
		public bool IsBlockedHarmfulVideo { get; private set; }

		public HarmfulContentReactionType HarmfulContentReactionType { get; set; }
		


		internal NicoVideoInfo Info { get; private set; }

//		internal ThumbnailResponseCache ThumbnailResponseCache { get; private set; }
//		internal WatchApiResponseCache WatchApiResponseCache { get; private set; }
//		internal CommentResponseCache CommentResponseCache { get; private set; }

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
