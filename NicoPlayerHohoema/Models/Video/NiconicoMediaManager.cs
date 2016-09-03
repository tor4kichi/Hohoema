using Mntone.Nico2.Videos.Thumbnail;
using Mntone.Nico2.Videos.WatchAPI;
using NicoPlayerHohoema.Util;
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
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.Models
{
	/// <summary>
	/// ニコニコ動画の動画やサムネイル画像、
	/// 動画情報など動画に関わるメディアを管理します
	/// </summary>
	public class NiconicoMediaManager : BindableBase, IDisposable
	{
		const string CACHE_REQUESTED_FILENAME = "cache_requested.json";

		static internal async Task<NiconicoMediaManager> Create(HohoemaApp app)
		{
			var man = new NiconicoMediaManager(app);


			// キャッシュリクエストファイルのアクセサーを初期化
			var videoSaveFolder = await app.GetCurrentUserVideoDataFolder();
			man._CacheRequestedItemsFileAccessor = new FileAccessor<IList<NicoVideoCacheRequest>>(videoSaveFolder, CACHE_REQUESTED_FILENAME);

			// ダウンロードコンテキストを作成
			man.Context = await NicoVideoDownloadContext.Create(app, man);

			// 初期化をバックグラウンドタスクに登録
			var updater = new SimpleBackgroundUpdate("NicoMediaManager", () => man.Initialize());
			await app.BackgroundUpdater.Schedule(updater);

			return man;
		}



		private NiconicoMediaManager(HohoemaApp app)
		{
			_HohoemaApp = app;

			VideoIdToNicoVideo = new Dictionary<string, NicoVideo>();

			_NicoVideoSemaphore = new SemaphoreSlim(1, 1);
			_CacheRequestedItemsStack = new ObservableCollection<NicoVideoCacheRequest>();
			CacheRequestedItemsStack = new ReadOnlyObservableCollection<NicoVideoCacheRequest>(_CacheRequestedItemsStack);

		}


		private async Task Initialize()
		{

			Debug.Write($"ダウンロードリクエストの復元を開始");


			// ダウンロードリクエストされたアイテムのNicoVideoオブジェクトの作成
			// 及び、リクエストの再構築
			var list = await LoadDownloadRequestItems();
			foreach (var req in list)
			{
				var nicoVideo = await GetNicoVideo(req.RawVideoid);
				_CacheRequestedItemsStack.Add(req);
				await nicoVideo.CheckCacheStatus();
				Debug.Write(".");
			}

			Debug.WriteLine("");
			Debug.WriteLine($"{list.Count} 件のダウンロードリクエストを復元");

			// キャッシュ済み動画情報のNicoVideoオブジェクトの作成
			// キャッシュリクエスト対象でなかった場合でも異常動作で終了していた場合に対応するため
			var saveFolder = Context.VideoSaveFolder;
			var files = await saveFolder.GetFilesAsync();

			var cachedFiles = files
				.Where(x => x.Name.EndsWith("_info.json"))
				.Select(x => new String(x.Name.TakeWhile(y => y != '_').ToArray()));

			foreach (var cachedFile in cachedFiles)
			{
				await GetNicoVideo(cachedFile);

				await Task.Delay(50);
			}


		}

		public async Task<NicoVideo> GetNicoVideo(string rawVideoId)
		{
			try
			{
				await _NicoVideoSemaphore.WaitAsync();

				if (!VideoIdToNicoVideo.ContainsKey(rawVideoId))
				{
					var nicoVideo = await NicoVideo.Create(_HohoemaApp, rawVideoId, Context);
					VideoIdToNicoVideo.Add(rawVideoId, nicoVideo);
				}

				return VideoIdToNicoVideo[rawVideoId];
			}
			finally
			{
				_NicoVideoSemaphore.Release();
			}
		}



		public void Dispose()
		{
			Context.Dispose();
		}



		#region Download Queue management


		// TODO: キャッシュ対象の検索が低速にならないように対策

		public bool HasDownloadQueue
		{
			get
			{
				return CacheRequestedItemsStack.Count > 0;
			}
		}


		/// <summary>
		/// 次のキャッシュリクエストを取得します
		/// </summary>
		/// <returns></returns>
		internal async Task<NicoVideoCacheRequest> GetNextCacheRequest()
		{
			foreach (var req in _CacheRequestedItemsStack)
			{
				var nicoVideo = await GetNicoVideo(req.RawVideoid);

				await nicoVideo.CheckCacheStatus();

				if (req.Quality == NicoVideoQuality.Original)
				{
					if (nicoVideo.OriginalQuality.IsCacheRequested
						&& nicoVideo.OriginalQuality.CanRequestDownload
						)
					{
						return req;
					}
				}
				else
				{
					if (nicoVideo.LowQuality.IsCacheRequested
						&& nicoVideo.LowQuality.CanRequestDownload
						)
					{
						return req;
					}
				}
			}

			return null;
		}


		/// <summary>
		/// キャッシュリクエストをキューの最後尾に積みます
		/// 通常のダウンロードリクエストではこちらを利用します
		/// </summary>
		/// <param name="req"></param>
		/// <returns></returns>
		internal async Task AddCacheRequest(string rawVideoId, NicoVideoQuality quality)
		{
			await RemoveCacheRequest(rawVideoId, quality);

			_CacheRequestedItemsStack.Add(new NicoVideoCacheRequest()
			{
				RawVideoid = rawVideoId,
				Quality = quality,
			});

			await SaveDownloadRequestItems();
		}


		public async Task<bool> RemoveCacheRequest(string rawVideoId, NicoVideoQuality quality)
		{
			var removeTarget = _CacheRequestedItemsStack.SingleOrDefault(x => x.RawVideoid == rawVideoId && x.Quality == quality);
			if (removeTarget != null)
			{
				_CacheRequestedItemsStack.Remove(removeTarget);
				await SaveDownloadRequestItems();

				return true;
			}
			else
			{
				return true;
			}
		}

		public bool CheckHasCacheRequest(string rawVideoId, NicoVideoQuality quality)
		{
			return _CacheRequestedItemsStack.Any(x => x.RawVideoid == rawVideoId && x.Quality == quality);
		}


		public async Task DeleteUnrequestedVideos()
		{
			var removeTargets = new List<string>();
			foreach (var item in VideoIdToNicoVideo.Values.ToArray())
			{
				if (PreventDeleteOnPlayingVideoId != null && item.RawVideoId == PreventDeleteOnPlayingVideoId)
				{
					Debug.WriteLine("再生中だった " + item.Title + " の動画キャッシュ削除を抑制");
					continue;
				}

				if (!item.OriginalQuality.IsCacheRequested && item.OriginalQuality.HasCache)
				{
					await item.OriginalQuality.DeleteCache();
				}

				if (!item.LowQuality.IsCacheRequested && item.LowQuality.HasCache)
				{
					await item.LowQuality.DeleteCache();
				}

				if (!item.OriginalQuality.IsCached
					&& !item.LowQuality.IsCached)
				{
					removeTargets.Add(item.RawVideoId);
				}
			}

			// 不要になったNicoVideoオブジェクトを破棄
			Debug.WriteLine("不要なNiciVideoオブジェクト破棄");
			foreach (var id in removeTargets)
			{
				VideoIdToNicoVideo.Remove(id);

				Debug.Write($"[{id}]");
			}


			PreventDeleteOnPlayingVideoId = null;

			Debug.WriteLine("done");
		}

		public async Task SaveDownloadRequestItems()
		{
			if (HasDownloadQueue)
			{
				await _CacheRequestedItemsFileAccessor.Save(_CacheRequestedItemsStack);

				Debug.WriteLine("ダウンロード待ち状況を保存しました。");
			}
			else
			{
				if (await _CacheRequestedItemsFileAccessor.Delete())
				{
					Debug.WriteLine("ダウンロード待ちがないため、状況ファイルを削除しました。");
				}
			}
		}

		public async Task<IList<NicoVideoCacheRequest>> LoadDownloadRequestItems()
		{
			if (await _CacheRequestedItemsFileAccessor.ExistFile())
			{
				return await _CacheRequestedItemsFileAccessor.Load();
			}
			else
			{
				return new List<NicoVideoCacheRequest>();
			}

		}

		#endregion


		public void OncePrevnetDeleteCacheOnPlayingVideo(string rawVideoId)
		{
			PreventDeleteOnPlayingVideoId = rawVideoId;
		}



		private FileAccessor<IList<NicoVideoCacheRequest>> _CacheRequestedItemsFileAccessor;
		private ObservableCollection<NicoVideoCacheRequest> _CacheRequestedItemsStack;
		public ReadOnlyObservableCollection<NicoVideoCacheRequest> CacheRequestedItemsStack { get; private set; }



		private SemaphoreSlim _NicoVideoSemaphore;

		public Dictionary<string, NicoVideo> VideoIdToNicoVideo { get; private set; }

		public NicoVideoDownloadContext Context { get; private set; }
		HohoemaApp _HohoemaApp;


		public string PreventDeleteOnPlayingVideoId { get; private set; }
	}








}
