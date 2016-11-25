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

		private CommentResponse _CachedCommentResponse;
		private NicoVideoQuality _VisitedPageType = NicoVideoQuality.Low;

		internal WatchApiResponse CachedWatchApiResponse { get; private set; }

		bool _IsInitialized = false;
		bool _thumbnailInitialized = false;

		public NicoVideo(HohoemaApp app, string rawVideoid, NicoVideoDownloadContext context)
		{
			HohoemaApp = app;
			RawVideoId = rawVideoid;
			_Context = context;

			_InterfaceByQuality = new Dictionary<NicoVideoQuality, DividedQualityNicoVideo>();
		}


		public async Task Initialize()
		{
			if (_IsInitialized) { return; }

			if (HohoemaApp.ServiceStatus >= HohoemaAppServiceLevel.OnlineWithoutLoggedIn)
			{
				Debug.WriteLine("start initialize : " + RawVideoId);

				await UpdateWithThumbnail();
			}
			else
			{
				return;
			}

			if (false == IsDeleted)
			{
				await OriginalQuality.SetupDownloadProgress();
				await LowQuality.SetupDownloadProgress();

				await CheckCacheStatus();
			}

			_IsInitialized = true;

		}





		public async Task CheckCacheStatus()
		{
			if (!_IsInitialized) { return; }

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

		

		private async Task UpdateWithThumbnail()
		{
			if (_thumbnailInitialized) { return; }

			// 動画のサムネイル情報にアクセスさせて、アプリ内部DBを更新
			try
			{
				var res = await HohoemaApp.ContentFinder.GetThumbnailResponse(RawVideoId);

				this.VideoId = res.Id;
				this.VideoLength = res.Length;
				this.SizeLow = (uint)res.SizeLow;
				this.SizeHigh = (uint)res.SizeHigh;
				this.Title = res.Title;
				this.VideoOwnerId = res.UserId;
				this.ContentType = res.MovieType;
				this.PostedAt = res.PostedAt.DateTime;
				this.Tags = res.Tags.Value.ToList();
				this.ViewCount = res.ViewCount;
				this.MylistCount = res.MylistCount;
				this.CommentCount = res.CommentCount;
				this.ThumbnailUrl = res.ThumbnailUrl.AbsoluteUri;

				_thumbnailInitialized = true;
			}
			catch (Exception e) when (e.Message.Contains("delete"))
			{
				this.IsDeleted = true;
				await DeletedTeardown();
			}
			catch (Exception e) when (e.Message.Contains("community"))
			{
				this.IsCommunity = true;
			}

			if (!this.IsDeleted && OriginalQuality.IsCacheRequested || LowQuality.IsCacheRequested)
			{
				// キャッシュ情報を最新の状態に更新
				await OnCacheRequested();
			}
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

			_VisitedPageType = watchApiRes.VideoUrl.AbsoluteUri.EndsWith("low") ? NicoVideoQuality.Low : NicoVideoQuality.Original;
			
			if (watchApiRes != null)
			{
				CachedWatchApiResponse = watchApiRes;

				VideoUrl = watchApiRes.VideoUrl;

				ProtocolType = MediaProtocolTypeHelper.ParseMediaProtocolType(watchApiRes.VideoUrl);

				DescriptionWithHtml = watchApiRes.videoDetail.description;
				ThreadId = watchApiRes.ThreadId.ToString();
				PrivateReasonType = watchApiRes.PrivateReason;

				if (!_thumbnailInitialized)
				{
					RawVideoId = watchApiRes.videoDetail.id;
					await UpdateWithThumbnail();
				}
				IsCommunity = watchApiRes.flashvars.is_community_video == "1";
				

				// TODO: 
//				Tags = watchApiRes.videoDetail.tagList.Select(x => new Tag()
//				{
					
//				}).ToList();

				if (watchApiRes.UploaderInfo != null)
				{
					VideoOwnerId = uint.Parse(watchApiRes.UploaderInfo.id);
				}


				this.IsDeleted = watchApiRes.IsDeleted;
				if (IsDeleted)
				{
					await DeletedTeardown();
				}
			}

			return watchApiRes;
		}




		public bool CanGetVideoStream()
		{
			return ProtocolType == MediaProtocolType.RTSPoverHTTP;
		}

		/// <summary>
		/// 動画ストリームの取得します
		/// 他にダウンロードされているアイテムは強制的に一時停止し、再生終了後に再開されます
		/// </summary>
		public async Task<IRandomAccessStream> GetVideoStream(NicoVideoQuality quality)
		{
			IfVideoDeletedThrowException();

            // キャッシュの状態を確認
            await CheckCacheStatus();

			// キャッシュ済みの場合は
			if (quality == NicoVideoQuality.Original && OriginalQuality.IsCached)
			{
				var file = await OriginalQuality.GetCacheFile();
				NicoVideoCachedStream = await file.OpenReadAsync();
			}
			else if (quality == NicoVideoQuality.Low && LowQuality.IsCached)
			{
				var file = await LowQuality.GetCacheFile();
				NicoVideoCachedStream = await file.OpenReadAsync();
			}
			else if (ProtocolType == MediaProtocolType.RTSPoverHTTP)
			{				
				if (Util.InternetConnection.IsInternet())
				{
					// キャッシュ保存フォルダに書き込み権限でアクセスできれば
					// キャッシュを伴ったダウンロード再生ストリームを作成
					if (await _Context.CanWriteAccessVideoCacheFolder()
						&& (
							ContentType == MovieType.Mp4
							//						|| ContentType == MovieType.Flv
							)
						)
					{
						NicoVideoDownloader = await _Context.GetDownloader(this, quality);

						NicoVideoCachedStream = new NicoVideoCachedStream(NicoVideoDownloader);
					}
					// キャッシュしない（出来ない）場合、キャッシュ無しでストリーミング再生
					else
					{
						var size = (quality == NicoVideoQuality.Original ? SizeHigh : SizeLow);
						NicoVideoCachedStream = await HttpSequencialAccessStream.CreateAsync(
							HohoemaApp.NiconicoContext.HttpClient
							, VideoUrl
							);
					}
				}
			}

			if (NicoVideoCachedStream == null)
			{
				throw new NotSupportedException();
			}
			

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
				await OnCacheRequested();
//				var commentRes = await GetCommentResponse();
//				CommentDb.AddOrUpdate(RawVideoId, commentRes);
			}
		}
		
		public async Task StopPlay()
		{
			if (NicoVideoCachedStream == null) { return; }

			NicoVideoCachedStream?.Dispose();
			NicoVideoCachedStream = null;

			if (NicoVideoDownloader != null)
			{
				if (NicoVideoDownloader.IsCacheRequested)
				{
					await NicoVideoDownloader.DividedQualityNicoVideo.SaveProgress();
				}
				else
				{
					await _Context.StopDownload(NicoVideoDownloader.DividedQualityNicoVideo.RawVideoId, NicoVideoDownloader.DividedQualityNicoVideo.Quality);
				}

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


			if (!OriginalQuality.IsCacheRequested 
				&& !LowQuality.IsCacheRequested)
			{
				var info = await VideoInfoDb.GetEnsureNicoVideoInfoAsync(RawVideoId);
				await VideoInfoDb.RemoveAsync(info);

				CommentDb.Remove(RawVideoId);
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

			var info = await VideoInfoDb.GetEnsureNicoVideoInfoAsync(RawVideoId);

			info.VideoId = this.VideoId;
			info.Length = this.VideoLength;
			info.LowSize = (uint)this.SizeLow;
			info.HighSize = (uint)this.SizeHigh;
			info.Title = this.Title;
			info.UserId = this.VideoOwnerId;
			info.MovieType = this.ContentType;
			info.PostedAt = this.PostedAt;
			info.SetTags(this.Tags);
			info.ViewCount = this.ViewCount;
			info.MylistCount = this.MylistCount;
			info.CommentCount = this.CommentCount;
			info.ThumbnailUrl = this.ThumbnailUrl;

			await VideoInfoDb.UpdateAsync(info);
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
			VideoInfoDb.Deleted(RawVideoId);
		}



		


		


		private void IfVideoDeletedThrowException()
		{
			if (IsDeleted) { throw new Exception("video is deleted"); }
		}


		public NGResult CheckUserNGVideo()
		{
			return HohoemaApp.UserSettings?.NGSettings.IsNgVideo(this);
		}


		// Initializeが呼ばれるまで有効
		public void PreSetTitle(string title)
		{
			if (_IsInitialized) { return; }

			Title = title;
		}

		public void PreSetPostAt(DateTime dateTime)
		{
			if (_IsInitialized) { return; }

			PostedAt = dateTime;
		}

		public void PreSetVideoLength(TimeSpan length)
		{
			if (_IsInitialized) { return; }

			VideoLength = length;
		}

		public void PreSetCommentCount(uint count)
		{
			if (_IsInitialized) { return; }

			CommentCount = count;
		}
		public void PreSetViewCount(uint count)
		{
			if (_IsInitialized) { return; }

			ViewCount = count;
		}
		public void PreSetMylistCount(uint count)
		{
			if (_IsInitialized) { return; }

			MylistCount = count;
		}

		public void PreSetThumbnailUrl(string thumbnailUrl)
		{
			if (_IsInitialized) { return; }

			ThumbnailUrl = thumbnailUrl;
		}


		public string RawVideoId { get; private set; }
		public string VideoId { get; private set; }

		public bool IsDeleted { get; private set; }
		public bool IsCommunity { get; private set; }

		public MovieType ContentType { get; private set; }
		public string Title { get; private set; }
		public TimeSpan VideoLength { get; private set; }
		public DateTime PostedAt { get; private set; }
		public uint VideoOwnerId { get; private set; }
		public bool IsOriginalQualityOnly => SizeLow == 0 || ContentType != MovieType.Mp4;
		public List<Tag> Tags { get; private set; }
		public uint SizeLow { get; private set; }
		public uint SizeHigh { get; private set; }
		public uint ViewCount { get; internal set; }
		public uint CommentCount { get; internal set; }
		public uint MylistCount { get; internal set; }
		public string ThumbnailUrl { get; internal set; }

		public Uri VideoUrl { get; private set; }
		public string ThreadId { get; private set; }
		public string DescriptionWithHtml { get; private set; }
		public bool NowLowQualityOnly { get; private set; }
		public MediaProtocolType ProtocolType { get; private set; }
		public PrivateReasonType PrivateReasonType { get; private set; } 




		public bool IsNeedPayment { get; private set; }


		public bool IsRequireConfirmDelete { get; private set; }

		public HohoemaApp HohoemaApp { get; private set; }
		NicoVideoDownloadContext _Context;

		public bool LastAccessIsLowQuality { get; private set; }


		// 有害動画への対応
		public bool IsBlockedHarmfulVideo { get; private set; }

		public HarmfulContentReactionType HarmfulContentReactionType { get; set; }
		


//		internal NicoVideoInfo Info { get; private set; }

//		internal ThumbnailResponseCache ThumbnailResponseCache { get; private set; }
//		internal WatchApiResponseCache WatchApiResponseCache { get; private set; }
//		internal CommentResponseCache CommentResponseCache { get; private set; }

		public NicoVideoDownloader NicoVideoDownloader { get; private set; }

		public IRandomAccessStream NicoVideoCachedStream { get; private set; }


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
