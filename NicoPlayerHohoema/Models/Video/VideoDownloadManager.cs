using NicoPlayerHohoema.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;

namespace NicoPlayerHohoema.Models
{
    public delegate void VideoCacheRequestedEventHandler(object sender, NicoVideoCacheRequest request);
    public delegate void VideoCacheRequestCanceledEventHandler(object sender, NicoVideoCacheRequest request);
    public delegate void VideoCacheDownloadStartedEventHandler(object sender, NicoVideoCacheRequest request, DownloadOperation op);
    public delegate void VideoCacheCompletedEventHandler(object sender, NicoVideoCacheRequest request, string filePath);
    public delegate void VideoCacheDownloadCanceledventHandler(object sender, NicoVideoCacheRequest request);

    public class VideoDownloadManager 
    {
        // Note: PendingVideosとダウンロードタスクの復元について
        // ダウンロードタスクはアプリではなくOS側で管理されるため、
        // アプリ再開時にダウンロードタスクを取得し直して
        // ダウンロード操作を再ハンドルする必要があります

        // しかし、アプリがアップデートされると
        // このダウンロードタスクをロストすることになるため、
        // ダウンロードタスクの復元は常にアプリ側で管理しなければいけません
        // 最悪の場合、ダウンロードリクエストされた記録が無くなる可能性があります

        // PendingVideosにはダウンロードタスクに登録されたアイテムも含んでいます

        // TryNextCacheRequestedVideoDownloadでPendingVideosからダウンロード中のリクエストを除外して
        // 次にダウンロードすべきアイテムを検索するようにしています



        const string CACHE_REQUESTED_FILENAME = "cache_requested.json";


        private FileAccessor<IList<NicoVideoCacheRequest>> _CacheRequestedItemsFileAccessor;


        public HohoemaApp HohoemaApp { get; private set; }
        public NiconicoMediaManager MediaManager { get; private set; }

        public event VideoCacheRequestedEventHandler Requested;
        public event VideoCacheRequestCanceledEventHandler RequestCanceled;
        public event VideoCacheDownloadStartedEventHandler DownloadStarted;
        public event VideoCacheCompletedEventHandler DownloadCompleted;
        public event VideoCacheDownloadCanceledventHandler DownloadCanceled;



        private AsyncLock _CacheDownloadPendingVideosLock = new AsyncLock();
        private ObservableCollection<NicoVideoCacheRequest> _CacheDownloadPendingVideos;
        public ReadOnlyObservableCollection<NicoVideoCacheRequest> CacheDownloadPendingVideos { get; private set; }

        private BackgroundDownloader _BackgroundDownloader;

        private AsyncLock _DownloadOperationsLock = new AsyncLock();
        private Dictionary<NicoVideoCacheRequest, DownloadOperation> _DownloadOperations = new Dictionary<NicoVideoCacheRequest, DownloadOperation>();

        private AsyncLock _RegistrationBackgroundTaskLock = new AsyncLock();

        // ダウンロードライン数
        // 通常会員は1ライン（再生DL含む）
        // プレミアム会員は2ライン（再生DL含まず）
        public uint CurrentDownloadTaskCount { get; private set; }
        public const uint MaxDownloadLineCount_Ippan = 1;
        public const uint MaxDownloadLineCount_Premium = 2;


        private bool IsInitialized = false;

        public bool CanAddDownloadLine
        {
            get
            {
                return HohoemaApp.IsPremiumUser
                    ? CurrentDownloadTaskCount < MaxDownloadLineCount_Premium
                    : CurrentDownloadTaskCount < MaxDownloadLineCount_Ippan;
            }

        }


        public VideoDownloadManager(HohoemaApp hohoemaApp, NiconicoMediaManager mediaManager)
        {
            HohoemaApp = hohoemaApp;
            MediaManager = mediaManager;

            _CacheDownloadPendingVideos = new ObservableCollection<NicoVideoCacheRequest>();
            CacheDownloadPendingVideos = new ReadOnlyObservableCollection<NicoVideoCacheRequest>(_CacheDownloadPendingVideos);

            _BackgroundDownloader = new BackgroundDownloader();

            HohoemaApp.OnSignin += HohoemaApp_OnSignin;
        }

        private async void HohoemaApp_OnSignin()
        {
            await TryNextCacheRequestedVideoDownload();
        }

        internal async Task Initialize()
        {
            IsInitialized = false;

            Debug.Write($"ダウンロードリクエストの復元を開始");
            // キャッシュリクエストファイルのアクセサーを初期化
            var videoSaveFolder = await HohoemaApp.GetApplicationLocalDataFolder();
            _CacheRequestedItemsFileAccessor = new FileAccessor<IList<NicoVideoCacheRequest>>(videoSaveFolder, CACHE_REQUESTED_FILENAME);

            // ダウンロード待機中のアイテムを復元
            await RestoreCacheRequestedItems();

            // ダウンロードバックグラウンドタスクの情報を復元
            await RestoreBackgroundDownloadTask();

            IsInitialized = true;
        }



        private async Task RestoreCacheRequestedItems()
        {
            // ダウンロードリクエストされたアイテムのNicoVideoオブジェクトの作成
            // 及び、リクエストの再構築
            var list = await LoadDownloadRequestItems();
            foreach (var req in list)
            {
                var nicoVideo = await MediaManager.GetNicoVideoAsync(req.RawVideoId);
                var div = nicoVideo.GetDividedQualityNicoVideo(req.Quality);
                await div.RequestCache();

                Debug.Write(".");
            }

            Debug.WriteLine("");
            Debug.WriteLine($"{list.Count} 件のダウンロードリクエストを復元");
        }



        #region Save & Load download request 

        public async Task SaveDownloadRequestItems()
        {
            if (!IsInitialized) { return; }

            using (var pendingVideoLockReleaser = await _CacheDownloadPendingVideosLock.LockAsync())
            {
                if (CacheDownloadPendingVideos.Count > 0)
                {
                    await _CacheRequestedItemsFileAccessor.Save(CacheDownloadPendingVideos);

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


        public async Task<DownloadOperation> GetDownloadingVideoOperation(string rawVideoId, NicoVideoQuality quality)
        {
            NicoVideoCacheRequest req = new NicoVideoCacheRequest()
            {
                RawVideoId = rawVideoId,
                Quality = quality
            };

            using (var releaser = await _DownloadOperationsLock.LockAsync())
            {
                return _DownloadOperations[req];
            }
        }



        // 次のキャッシュダウンロードを試行
        // ダウンロード用の本数
        public async Task TryNextCacheRequestedVideoDownload()
        {
            if (!IsInitialized) { return; }

            if (!HohoemaApp.IsLoggedIn) { return; }


            NicoVideoCacheRequest nextDownloadItem = null;
            using (var releaser = await _DownloadOperationsLock.LockAsync())
            using (var pendingVideoLockReleaser = await _CacheDownloadPendingVideosLock.LockAsync())
            {
                if (CanAddDownloadLine)
                {
                    nextDownloadItem = _CacheDownloadPendingVideos
                        .SkipWhile(x => _DownloadOperations.ContainsKey(x))
                        .FirstOrDefault();
                }
            }

            if (nextDownloadItem != null)
            {
                var op = await DonwloadVideoInBackgroundTask(nextDownloadItem);
            }
        }

        public async Task AddCacheRequest(NicoVideoCacheRequest req, bool forceUpdate = false)
        {
            NicoVideoCacheRequest already = null;
            using (var pendingVideoLockReleaser = await _CacheDownloadPendingVideosLock.LockAsync())
            {
                already = _CacheDownloadPendingVideos.FirstOrDefault(x => x.RawVideoId == req.RawVideoId && x.Quality == req.Quality);
            }
            if (already != null)
            {
                if (forceUpdate)
                {
                    using (var pendingVideoLockReleaser = await _CacheDownloadPendingVideosLock.LockAsync())
                    {
                        req.RequestAt = already.RequestAt;
                        _CacheDownloadPendingVideos.Remove(already);
                    }
                }
                else
                {
                    await TryNextCacheRequestedVideoDownload();
                    return;
                }
            }

            await _AddCacheRequest_Internal(req);

            await SaveDownloadRequestItems();

            await TryNextCacheRequestedVideoDownload();
        }

        /// <summary>
		/// キャッシュリクエストをキューの最後尾に積みます
		/// 通常のダウンロードリクエストではこちらを利用します
		/// </summary>
		/// <param name="req"></param>
		/// <returns></returns>
		public Task AddCacheRequest(string rawVideoId, NicoVideoQuality quality, bool forceUpdate = false)
        {
            var req = new NicoVideoCacheRequest()
            {
                RawVideoId = rawVideoId,
                Quality = quality,
                RequestAt = DateTime.Now
            };

            return AddCacheRequest(req, forceUpdate);
        }



        internal async Task _AddCacheRequest_Internal(NicoVideoCacheRequest req)
        {
            var nicoVideo = await MediaManager.GetNicoVideoAsync(req.RawVideoId);
            var div = nicoVideo.GetDividedQualityNicoVideo(req.Quality);

            if (div.IsCached)
            {
                if (req.IsRequireForceUpdate)
                {
                    Debug.WriteLine($"{req.RawVideoId}<{req.Quality}> is already cached, but enable force update .(re-download)");
                    await RemoveCacheRequest(req.RawVideoId, req.Quality);
                }
                else
                {
                    Debug.WriteLine($"{req.RawVideoId}<{req.Quality}> is already cached. (skip download)");
                    Requested?.Invoke(this, req);
                    DownloadCompleted?.Invoke(this, req, div.CacheFilePath);
                    return;
                }
            }

            using (var pendingVideoLockReleaser = await _CacheDownloadPendingVideosLock.LockAsync())
            {
                _CacheDownloadPendingVideos.Add(req);
            }

            Requested?.Invoke(this, req);
        }

        public async Task<bool> RemoveCacheRequest(string rawVideoId, NicoVideoQuality quality)
        {
            NicoVideoCacheRequest removeTarget = null;
            using (var pendingVideoLockReleaser = await _CacheDownloadPendingVideosLock.LockAsync())
            {
                removeTarget = _CacheDownloadPendingVideos.SingleOrDefault(x => x.RawVideoId == rawVideoId && x.Quality == quality);
            }

            if (removeTarget != null)
            {
                using (var pendingVideoLockReleaser = await _CacheDownloadPendingVideosLock.LockAsync())
                {
                    _CacheDownloadPendingVideos.Remove(removeTarget);
                }

                await SaveDownloadRequestItems();

                RequestCanceled?.Invoke(this, removeTarget);

                return true;
            }
            else
            {
                return false;
            }
        }





        // バックグラウンドで動画キャッシュのダウンロードを行うタスクを作成
        private async Task<DownloadOperation> DonwloadVideoInBackgroundTask(NicoVideoCacheRequest req)
        {
            using (var bgTaskLock = await _RegistrationBackgroundTaskLock.LockAsync())
            {
                using (var releaser = await _DownloadOperationsLock.LockAsync())
                {
                    if (_DownloadOperations.Keys.Any(x => x.RawVideoId == req.RawVideoId && x.Quality == req.Quality))
                    {
                        return null;
                    }
                }

                Debug.WriteLine($"キャッシュ準備を開始: {req.RawVideoId} {req.Quality}");

                // 動画ダウンロードURLを取得
                var nicoVideo = await MediaManager.GetNicoVideoAsync(req.RawVideoId);

                var div = nicoVideo.GetDividedQualityNicoVideo(req.Quality);
                if (div.IsCached)
                {
                    if (!req.IsRequireForceUpdate)
                    {
                        using (var pendingVideoLockReleaser = await _CacheDownloadPendingVideosLock.LockAsync())
                        {
                            _CacheDownloadPendingVideos.Remove(req);
                        }
                    }
                    else
                    {
                        // キャッシュ済みのためダウンロードを行わない
                        // TODO: Completedイベントを発行
                        return null;
                    }
                }

                var uri = await nicoVideo.GetVideoUrl(req.Quality);
                if (uri == null)
                {
                    throw new Exception($"can't download {req.Quality} quality Video, in {req.RawVideoId}.");
                }

                // 認証情報付きクッキーをダウンローダーのHttpヘッダにコピー
                // 動画ページアクセス後のクッキーが必須になるため、サインイン時ではなく
                // ダウンロード開始直前のこのタイミングでクッキーをコピーしています
                var httpclinet = HohoemaApp.NiconicoContext.HttpClient;
                foreach (var header in httpclinet.DefaultRequestHeaders)
                {
                    _BackgroundDownloader.SetRequestHeader(header.Key, header.Value);
                }

                // 保存先ファイルの確保
                var filename = req.Quality.FileNameWithQualityNameExtention(req.RawVideoId);
                var videoFolder = await HohoemaApp.GetVideoCacheFolder();
                var videoFile = await videoFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);

                // ダウンロード操作を作成
                var operation = _BackgroundDownloader.CreateDownload(uri, videoFile);

                await AddDownloadOperation(req, operation);

                Debug.WriteLine($"キャッシュ準備完了: {req.RawVideoId} {req.Quality}");


                // ダウンロードを開始
                var action = operation.StartAsync();
                action.Progress = DownloadProgress;
                var task = action.AsTask().ContinueWith(OnDownloadCompleted).ConfigureAwait(false);

                Debug.WriteLine($"キャッシュ開始: {req.RawVideoId} {req.Quality}");

                return operation;
            }
        }

        private void DownloadProgress(object sender, DownloadOperation op)
        {
            Debug.WriteLine($"{op.RequestedUri}:{op.Progress.TotalBytesToReceive}");
        }

        // ダウンロード完了
        private async Task OnDownloadCompleted(Task<DownloadOperation> prevTask)
        {
            Debug.WriteLine("キャッシュ完了");

            if (prevTask.Result != null)
            {
                var op = prevTask.Result;

                var info = NiconicoMediaManager.CacheRequestInfoFromFileName(op.ResultFile);

                await RemoveDownloadOperation(info);

                if (op.Progress.Status == BackgroundTransferStatus.Completed)
                {
                    Debug.WriteLine("キャッシュ済み: " + op.ResultFile.Name);
                }
                else
                {
                    Debug.WriteLine($"キャッシュ失敗: {op.ResultFile.Name} （再ダウンロードします）");
                }
            }
        }

        /// <summary>
        /// キャッシュダウンロードのタスクから
        /// ダウンロードの情報を復元します
        /// </summary>
        /// <returns></returns>
        private async Task RestoreBackgroundDownloadTask()
        {
            // TODO: ユーザーのログイン情報を更新してダウンロードを再開する必要がある？
            // ユーザー情報の有効期限が切れていた場合には最初からダウンロードし直す必要があるかもしれません

            var tasks = await BackgroundDownloader.GetCurrentDownloadsAsync();
            foreach (var task in tasks)
            {
                NicoVideoCacheProgress info = null;
                try
                {
                    var _info = NiconicoMediaManager.CacheRequestInfoFromFileName(task.ResultFile);
                    info = new NicoVideoCacheProgress()
                    {
                        RawVideoId = _info.RawVideoId,
                        Quality = _info.Quality,
                        DownloadOperation = task
                    };

                    await AddDownloadOperation(_info, task);

                    Debug.WriteLine($"実行中のキャッシュBGDLを補足: {info.RawVideoId} {info.Quality}");
                }
                catch
                {
                    Debug.WriteLine(task.ResultFile + "のキャッシュダウンロード操作を復元に失敗しました");
                    continue;
                }

                
                try
                {
                    task.Resume();
                }
                catch
                {
                    if (task.Progress.Status != BackgroundTransferStatus.Running)
                    {
                        await RemoveDownloadOperation(info);

                        // ダウンロード再開に失敗したらキャッシュリクエストに積み直します
                        // 失敗の通知はここではなくバックグラウンドタスクの 後処理 として渡されるかもしれません
                        await AddCacheRequest(info.RawVideoId, info.Quality, forceUpdate: true);
                    }
                }
            }
        }





        private async Task AddDownloadOperation(NicoVideoCacheRequest req, DownloadOperation op)
        {
            using (var releaser = await _DownloadOperationsLock.LockAsync())
            {
                _DownloadOperations.Add(req, op);
                ++CurrentDownloadTaskCount;
            }

            // ダウンロード開始イベントをトリガー
            DownloadStarted?.Invoke(this, req, op);
        }

        private async Task RemoveDownloadOperation(NicoVideoCacheRequest req)
        {
            DownloadOperation op = null;
            using (var releaser = await _DownloadOperationsLock.LockAsync())
            {
                op = _DownloadOperations[req];

                if (op != null)
                {
                    _DownloadOperations.Remove(req);
                    ++CurrentDownloadTaskCount;
                }
            }


            if (op == null)
            {
                return;
            }
            
            if (op.Progress.Status == BackgroundTransferStatus.Completed)
            {
                DownloadCompleted?.Invoke(this, req, op.ResultFile.Path);

                using (var pendingVideoLockReleaser = await _CacheDownloadPendingVideosLock.LockAsync())
                {
                    _CacheDownloadPendingVideos.Remove(req);
                }
            }
            else
            {
                DownloadCanceled?.Invoke(this, req);

                await AddCacheRequest(req.RawVideoId, req.Quality, forceUpdate: true);
            }
        }
    }
}
