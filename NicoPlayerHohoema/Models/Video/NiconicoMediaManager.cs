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
using Windows.UI.Core;
using Windows.Networking.BackgroundTransfer;
using System.Text.RegularExpressions;

namespace NicoPlayerHohoema.Models
{
	/// <summary>
	/// ニコニコ動画の動画やサムネイル画像、
	/// 動画情報など動画に関わるメディアを管理します
	/// </summary>
	public class NiconicoMediaManager : BindableBase, IDisposable, IBackgroundUpdateable
	{
		const string CACHE_REQUESTED_FILENAME = "cache_requested.json";


        private FileAccessor<IList<NicoVideoCacheRequest>> _CacheRequestedItemsFileAccessor;

        private ObservableCollection<NicoVideoCacheRequest> _CacheRequestedItemsStack;
        public ReadOnlyObservableCollection<NicoVideoCacheRequest> CacheRequestedItemsStack { get; private set; }

        private ObservableCollection<NicoVideoCacheInfo> _CacheCompletedItemsStack;
        public ReadOnlyObservableCollection<NicoVideoCacheInfo> CacheCompletedItemsStack { get; private set; }

        private ObservableCollection<NicoVideoCacheRequest> _CacheDownloadingItemsStack;
        public ReadOnlyObservableCollection<NicoVideoCacheRequest> CacheDownloadingItemsStack { get; private set; }


        private AsyncLock _Lock;
        private AsyncLock _NicoVideoUpdateLock = new AsyncLock();

        public Dictionary<string, NicoVideo> VideoIdToNicoVideo { get; private set; }

        HohoemaApp _HohoemaApp;
        public bool IsInitialized { get; private set; }


        private BackgroundDownloader _BackgroundDownloader;

        private AsyncLock _DownloadOperationsLock = new AsyncLock();
        private List<DownloadOperation> _DownloadOperations = new List<DownloadOperation>();



        // ダウンロードライン数
        // 通常会員は1ライン（再生DL含む）
        // プレミアム会員は2ライン（再生DL含まず）
        public uint CurrentDownloadTaskCount { get; private set; }
        public const uint MaxDownloadLineCount_Ippan = 1;
        public const uint MaxDownloadLineCount_Premium = 2;


        public bool CanAddDownloadLine
        {
            get
            {
                return _HohoemaApp.IsPremiumUser
                    ? CurrentDownloadTaskCount < MaxDownloadLineCount_Premium
                    : CurrentDownloadTaskCount < MaxDownloadLineCount_Ippan;
            }

        }

        static internal async Task<NiconicoMediaManager> Create(HohoemaApp app)
		{
			var man = new NiconicoMediaManager(app);

			// キャッシュリクエストファイルのアクセサーを初期化
			var videoSaveFolder = await app.GetApplicationLocalDataFolder();
			man._CacheRequestedItemsFileAccessor = new FileAccessor<IList<NicoVideoCacheRequest>>(videoSaveFolder, CACHE_REQUESTED_FILENAME);

//            await man.RetrieveCacheCompletedVideos();

            return man;
		}



		private NiconicoMediaManager(HohoemaApp app)
		{
			_HohoemaApp = app;

			VideoIdToNicoVideo = new Dictionary<string, NicoVideo>();

			_Lock = new AsyncLock();

            _CacheCompletedItemsStack = new ObservableCollection<NicoVideoCacheInfo>();
            CacheCompletedItemsStack = new ReadOnlyObservableCollection<NicoVideoCacheInfo>(_CacheCompletedItemsStack);

			_CacheRequestedItemsStack = new ObservableCollection<NicoVideoCacheRequest>();
			CacheRequestedItemsStack = new ReadOnlyObservableCollection<NicoVideoCacheRequest>(_CacheRequestedItemsStack);

            _CacheDownloadingItemsStack = new ObservableCollection<NicoVideoCacheRequest>();
            CacheDownloadingItemsStack = new ReadOnlyObservableCollection<NicoVideoCacheRequest>(_CacheDownloadingItemsStack);

            _HohoemaApp.OnSignin += _HohoemaApp_OnSignin;
            
            _BackgroundDownloader = new BackgroundDownloader();
            

        }

		private void _HohoemaApp_OnSignin()
		{
			// 初期化をバックグラウンドタスクに登録
			//var updater = 
			//updater.Completed += (sender, item) => 
			//{
//				IsInitialized = true;
			//};
		}

		public void Dispose()
		{
		}


		#region interface IBackgroundUpdateable

		public IAsyncAction BackgroundUpdate(CoreDispatcher uiDispatcher)
		{
			return Initialize()
				.AsAsyncAction();
		}

		#endregion


		private async Task Initialize()
		{
			Debug.Write($"ダウンロードリクエストの復元を開始");

            // ダウンロード中の情報を復元
            await RestoreDownloadTask();

            // キャッシュ完了したアイテムを検索
            await RetrieveCacheCompletedVideos();

            // キャッシュリクエストされたアイテムを復元
            await RestoreCacheRequestedItems();

            // ダウンロード中のアイテムが無い場合にはBGダウンロードを走らせられるようにする
            if (_CacheDownloadingItemsStack.Count == 0)
            {
//                await TryNextCacheRequestedVideoDownload();
            }
        }


		


		


		public async Task<NicoVideo> GetNicoVideoAsync(string rawVideoId, bool withInitialize = true)
		{
			NicoVideo nicoVideo = null;
			bool isFirstGet = false;
			using (var releaser = await _Lock.LockAsync())
			{
				if (false == VideoIdToNicoVideo.ContainsKey(rawVideoId))
				{
					nicoVideo = new NicoVideo(_HohoemaApp, rawVideoId, _HohoemaApp.MediaManager);
					VideoIdToNicoVideo.Add(rawVideoId, nicoVideo);
					isFirstGet = true;
				}
				else
				{
					nicoVideo = VideoIdToNicoVideo[rawVideoId];
				}
			}

			if (isFirstGet && withInitialize)
			{
				using (var releaser = await _Lock.LockAsync())
				{
					await nicoVideo.Initialize();
				}
			}

			return nicoVideo;
		}


		public async Task<List<NicoVideo>> GetNicoVideoItemsAsync(params string[] idList)
		{
			List<NicoVideo> videos = new List<NicoVideo>();

			using (var releaser = await _Lock.LockAsync())
			{
				foreach (var id in idList)
				{
					NicoVideo nicoVideo = null;
					if (false == VideoIdToNicoVideo.ContainsKey(id))
					{
						nicoVideo = new NicoVideo(_HohoemaApp, id, _HohoemaApp.MediaManager);
						VideoIdToNicoVideo.Add(id, nicoVideo);
					}
					else
					{
						nicoVideo = VideoIdToNicoVideo[id];
					}

					videos.Add(nicoVideo);
				}
			}

			foreach (var video in videos.AsParallel())
			{
				await video.Initialize().ConfigureAwait(false);
			}

			return videos;
		}

        
		private async Task RetrieveCacheCompletedVideos()
		{
			var videoFolder = await _HohoemaApp.GetVideoCacheFolder();
			if (videoFolder != null)
			{
				var files = await videoFolder.GetFilesAsync();

                foreach (var file in files)
                {
                    if (file.FileType != ".mp4")
                    {
                        continue;
                    }
                    // ファイル名の最後方にある[]の中身の文字列を取得
                    // (動画タイトルに[]が含まれる可能性に配慮)
                    var regex = new Regex("(?:(?:sm|so|lv)\\d*)");
                    var match = regex.Match(file.Name);
                    var id = match.Value;
                    var quality = NicoVideoQualityFileNameHelper.NicoVideoQualityFromFileNameExtention(file.Name);
                    var info = new Tuple<string, NicoVideoQuality>(id, quality);

					var nicoVideo = await GetNicoVideoAsync(info.Item1);
					_CacheCompletedItemsStack.Add(new NicoVideoCacheInfo()
					{
						RawVideoId = info.Item1,
						Quality = info.Item2,
                        FilePath = file.Path

                    });
					await nicoVideo.CheckCacheStatus();

                    Debug.Write(".");
				}
			}
		}


        private async Task RestoreCacheRequestedItems()
        {
            // ダウンロードリクエストされたアイテムのNicoVideoオブジェクトの作成
            // 及び、リクエストの再構築
            var list = await LoadDownloadRequestItems();
            foreach (var req in list)
            {
                _CacheRequestedItemsStack.Insert(0, req);
                //                var nicoVideo = await GetNicoVideoAsync(req.RawVideoId);
                Debug.Write(".");
            }

            Debug.WriteLine("");
            Debug.WriteLine($"{list.Count} 件のダウンロードリクエストを復元");
        }


		#region Download Queue management


		
		public bool HasDownloadQueue
		{
			get
			{
				return CacheRequestedItemsStack.Count > 0;
			}
		}


		
		/// <summary>
		/// キャッシュリクエストをキューの最後尾に積みます
		/// 通常のダウンロードリクエストではこちらを利用します
		/// </summary>
		/// <param name="req"></param>
		/// <returns></returns>
		internal async Task AddCacheRequest(string rawVideoId, NicoVideoQuality quality, bool forceUpdate = false)
		{
            if (!forceUpdate && CheckVideoCached(rawVideoId, quality))
            {
                return;
            }


            if (CheckCacheRequested(rawVideoId, quality))
            {
                if (forceUpdate)
                {
                    await RemoveCacheRequest(rawVideoId, quality);
                }
                else
                {
                    return;
                }
            }
			

			_CacheRequestedItemsStack.Insert(0, new NicoVideoCacheRequest()
			{
				RawVideoId = rawVideoId,
				Quality = quality,
			});

			await SaveDownloadRequestItems();

            await TryNextCacheRequestedVideoDownload();
        }


		public async Task<bool> RemoveCacheRequest(string rawVideoId, NicoVideoQuality quality)
		{
			var removeTarget = _CacheRequestedItemsStack.SingleOrDefault(x => x.RawVideoId == rawVideoId && x.Quality == quality);
			if (removeTarget != null)
			{
				_CacheRequestedItemsStack.Remove(removeTarget);
				await SaveDownloadRequestItems();

				return true;
			}
			else
			{
				return false;
			}
        }

        public bool CheckCacheRequested(string rawVideoId, NicoVideoQuality quality)
		{
			return _CacheRequestedItemsStack.Any(x => x.RawVideoId == rawVideoId && x.Quality == quality);
		}


        public bool CheckVideoCached(string rawVideoId, NicoVideoQuality quality)
        {
            return _CacheCompletedItemsStack.Any(x => x.RawVideoId == rawVideoId && x.Quality == quality);
        }


        public async Task SaveDownloadRequestItems()
		{
			if (HasDownloadQueue)
			{
				await _CacheRequestedItemsFileAccessor.Save(_CacheRequestedItemsStack);

				Debug.WriteLine("ダウンロードリクエストを保存しました。");
			}
			else
			{
				if (await _CacheRequestedItemsFileAccessor.Delete())
				{
					Debug.WriteLine("ダウンロードリクエストがないため、ファイルを削除しました。");
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


        // 次のキャッシュダウンロードを試行
        // ダウンロード用の本数
        public async Task TryNextCacheRequestedVideoDownload()
        {
            if (CanAddDownloadLine)
            {
                var item = _CacheRequestedItemsStack.LastOrDefault();
                if (item != null)
                {
                    var op = await DonwloadVideoInBackgroundTask(item);
                }
            }
        }

        


        // バックグラウンドで動画キャッシュのダウンロードを行うタスクを作成
        private async Task<DownloadOperation> DonwloadVideoInBackgroundTask(NicoVideoCacheRequest req)
        {
            Debug.WriteLine($"キャッシュ準備を開始: {req.RawVideoId} {req.Quality}");

            // 動画ダウンロードURLを取得
            var nicoVideo = await GetNicoVideoAsync(req.RawVideoId);
            var uri = await nicoVideo.GetVideoUrl(req.Quality);
            if (uri == null)
            {
                throw new Exception($"can't download {req.Quality} quality Video, in {req.RawVideoId}.");
            }

            // 認証情報付きクッキーをダウンローダーのHttpヘッダにコピー
            // 動画ページアクセス後のクッキーが必須になるため、サインイン時ではなく
            // ダウンロード開始直前のこのタイミングでクッキーをコピーしています
            var httpclinet = _HohoemaApp.NiconicoContext.HttpClient;
            foreach (var header in httpclinet.DefaultRequestHeaders)
            {
                _BackgroundDownloader.SetRequestHeader(header.Key, header.Value);
            }

            // 保存先ファイルの確保
            var filename = req.Quality.FileNameWithQualityNameExtention(req.RawVideoId);
            var videoFolder = await _HohoemaApp.GetVideoCacheFolder();
            var videoFile = await videoFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);

            // ダウンロード操作を作成
            var operation = _BackgroundDownloader.CreateDownload(uri, videoFile);
            using (var releaser = await _DownloadOperationsLock.LockAsync())
            {
                _DownloadOperations.Add(operation);
                ++CurrentDownloadTaskCount;
            }

            Debug.WriteLine($"キャッシュ準備完了: {req.RawVideoId} {req.Quality}");


            // ダウンロードを開始
            var action = operation.StartAsync();
            action.Progress = DownloadProgress;

            Debug.WriteLine($"キャッシュ開始: {req.RawVideoId} {req.Quality}");

            return operation;
        }

        private void DownloadProgress(object sender, DownloadOperation op)
        {
            Debug.WriteLine(op.Progress.ToString());
        }

        // ダウンロード完了
        private async Task DownloadCompleted(Task<DownloadOperation> prevTask)
        {
            Debug.WriteLine("キャッシュ完了");

            if (prevTask.Result != null)
            {
                var op = prevTask.Result;

                using (var releaser = await _DownloadOperationsLock.LockAsync())
                {
                    _DownloadOperations.Remove(op);
                    --CurrentDownloadTaskCount;
                }

                var info = CacheRequestInfoFromFileName(op.ResultFile);

                if (op.Progress.Status == BackgroundTransferStatus.Completed)
                {
                    Debug.WriteLine("キャッシュ済み: " + op.ResultFile.Name);                    
                    _CacheCompletedItemsStack.Add(new NicoVideoCacheInfo()
                    {
                        RawVideoId = info.RawVideoId,
                        Quality = info.Quality,
                        FilePath = op.ResultFile.Path
                    });
                }
                else
                {
                    Debug.WriteLine($"キャッシュ失敗: {op.ResultFile.Name} （再ダウンロードします）" );
                    await AddCacheRequest(info.RawVideoId, info.Quality, forceUpdate:true);
                }

                
            }
            
        }



        private static NicoVideoCacheRequest CacheRequestInfoFromFileName(IStorageFile file)
        {
            // キャッシュリクエストを削除
            var id = Path.GetFileNameWithoutExtension(file.Name);
            var quality = NicoVideoQualityFileNameHelper.NicoVideoQualityFromFileNameExtention(file.Name);

            return new NicoVideoCacheRequest() { RawVideoId = id, Quality = quality };
        }

        /// <summary>
        /// キャッシュダウンロードのタスクから
        /// ダウンロードの情報を復元します
        /// </summary>
        /// <returns></returns>
        private async Task RestoreDownloadTask()
        {
            // TODO: _CacheDownloadingItemsStackの操作にLock必要

            // TODO: ユーザーのログイン情報を更新してダウンロードを再開する必要がある？
            // ユーザー情報の有効期限が切れていた場合には最初からダウンロードし直す必要があるかもしれません

            _CacheDownloadingItemsStack.Clear();
            var tasks = await BackgroundDownloader.GetCurrentDownloadsAsync();
            foreach (var task in tasks)
            {
                NicoVideoCacheRequest info = null;
                try
                {
                    info = CacheRequestInfoFromFileName(task.ResultFile);
                    _CacheDownloadingItemsStack.Add(info);
                }
                catch
                {
                    Debug.WriteLine(task.ResultFile + "のキャッシュダウンロード操作を復元に失敗しました" );
                    continue;
                }

                try
                {
                    task.Resume();
                }
                catch
                {
                    // ダウンロード再開に失敗したらキャッシュリクエストに積み直します
                    // 失敗の通知はここではなくバックグラウンドタスクの 後処理 として渡されるかもしれません
                    _CacheDownloadingItemsStack.Remove(info);
                    await AddCacheRequest(info.RawVideoId, info.Quality, forceUpdate:true);
                }
            }
        }



        public IAsyncOperation<StorageFile> GetCachedVideo(string rawVideoId, NicoVideoQuality quality)
        {
            var cacheInfo = CacheCompletedItemsStack.FirstOrDefault(x => x.RawVideoId == rawVideoId && x.Quality == quality);

            return StorageFile.GetFileFromPathAsync(cacheInfo.FilePath);
        }




        public async Task OnCacheFolderChanged()
		{
			foreach (var nicoVideo in VideoIdToNicoVideo.Values)
			{
				await nicoVideo.CheckCacheStatus();
			}
		}

	

        internal void StopCacheDownload()
        {
            
        }
    }








}
