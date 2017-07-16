using Microsoft.Toolkit.Uwp.Notifications;
using NicoPlayerHohoema.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.UI.Notifications;

namespace NicoPlayerHohoema.Models
{
    public delegate void VideoCacheRequestedEventHandler(object sender, NicoVideoCacheRequest request);
    public delegate void VideoCacheRequestCanceledEventHandler(object sender, NicoVideoCacheRequest request);
    public delegate void VideoCacheDownloadStartedEventHandler(object sender, NicoVideoCacheRequest request, DownloadOperation op);
    public delegate void VideoCacheDownloadProgressEventHandler(object sender, NicoVideoCacheRequest request, DownloadOperation op);
    public delegate void VideoCacheCompletedEventHandler(object sender, NicoVideoCacheRequest request, string filePath);
    public delegate void VideoCacheDownloadCanceledventHandler(object sender, NicoVideoCacheRequest request);



    public struct BackgroundTransferCompletionInfo
    {
        public string Id { get; set; }
        public BackgroundTaskRegistration TaskRegistration { get; set; }
        public BackgroundDownloader Downloader { get; set; }
        public BackgroundTransferCompletionGroup TransferCompletionGroup { get; set; }
    }


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
        public event VideoCacheDownloadProgressEventHandler DownloadProgress;
        public event VideoCacheCompletedEventHandler DownloadCompleted;
        public event VideoCacheDownloadCanceledventHandler DownloadCanceled;



        private AsyncLock _CacheDownloadPendingVideosLock = new AsyncLock();
        private ObservableCollection<NicoVideoCacheRequest> _CacheDownloadPendingVideos;
        public ReadOnlyObservableCollection<NicoVideoCacheRequest> CacheDownloadPendingVideos { get; private set; }

//        private BackgroundTransferCompletionGroup _BTCG = new BackgroundTransferCompletionGroup();

//        private BackgroundDownloader _BackgroundDownloader;

        private AsyncLock _DownloadOperationsLock = new AsyncLock();
        private Dictionary<NicoVideoCacheRequest, DownloadOperation> _DownloadOperations = new Dictionary<NicoVideoCacheRequest, DownloadOperation>();

        private AsyncLock _RegistrationBackgroundTaskLock = new AsyncLock();



        private Dictionary<string, BackgroundTransferCompletionInfo> _BackgroundTransferCompletionInfoMap = new Dictionary<string, BackgroundTransferCompletionInfo>();

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


        private static TileContent GetSuccessTileContent(string videoTitle, string videoId)
        {
            var tileTitle = "キャッシュ完了";
            var tileSubject = videoTitle;
            var tileBody = videoId;
            return new TileContent()
            {
                Visual = new TileVisual()
                {
                    TileMedium = new TileBinding()
                    {
                        Content = new TileBindingContentAdaptive()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = tileTitle
                                },

                                new AdaptiveText()
                                {
                                    Text = tileSubject,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                },

                                new AdaptiveText()
                                {
                                    Text = tileBody,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                }
                            }
                        }
                    },

                    TileWide = new TileBinding()
                    {
                        Content = new TileBindingContentAdaptive()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = tileTitle,
                                    HintStyle = AdaptiveTextStyle.Subtitle
                                },

                                new AdaptiveText()
                                {
                                    Text = tileSubject,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                },

                                new AdaptiveText()
                                {
                                    Text = tileBody,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                }
                            }
                        }
                    }
                }
            };
        }

        public VideoDownloadManager(HohoemaApp hohoemaApp, NiconicoMediaManager mediaManager)
        {
            HohoemaApp = hohoemaApp;
            MediaManager = mediaManager;

            _CacheDownloadPendingVideos = new ObservableCollection<NicoVideoCacheRequest>();
            CacheDownloadPendingVideos = new ReadOnlyObservableCollection<NicoVideoCacheRequest>(_CacheDownloadPendingVideos);

            HohoemaApp.OnSignin += HohoemaApp_OnSignin;

            // ダウンロード完了をバックグラウンドで処理
            CoreApplication.BackgroundActivated += CoreApplication_BackgroundActivated;

        }

        private void CoreApplication_BackgroundActivated(object sender, BackgroundActivatedEventArgs e)
        {
            var taskInstance = e.TaskInstance;
            var deferral = taskInstance.GetDeferral();

            var details = taskInstance.TriggerDetails as BackgroundTransferCompletionGroupTriggerDetails;

            if (details == null) { return; }

            IReadOnlyList<DownloadOperation> downloads = details.Downloads;



            var notifier = ToastNotificationManager.CreateToastNotifier();

            foreach (var dl in downloads)
            {
                try
                {
                    if (dl.Progress.BytesReceived != dl.Progress.TotalBytesToReceive)
                    {
                        continue;
                    }

                    if (dl.ResultFile == null)
                    {
                        continue;
                    }

                    var file = dl.ResultFile;

                    // ファイル名の最後方にある[]の中身の文字列を取得
                    // (動画タイトルに[]が含まれる可能性に配慮)
                    var regex = new Regex("(?:(?:sm|so|lv)\\d*)");
                    var match = regex.Match(file.Name);
                    var id = match.Value;

                    // キャッシュファイルからタイトルを抜き出します
                    // ファイルタイトルの決定は 
                    // DividedQualityNicoVideo.VideoFileName プロパティの
                    // 実装に依存します
                    // 想定された形式は以下の形です

                    // タイトル - [sm12345667].mp4
                    // タイトル - [sm12345667].low.mp4

                    var index = file.Name.LastIndexOf(" - [");
                    var title = file.Name.Remove(index);

                    // トーストのレイアウトを作成
                    ToastContent content = new ToastContent()
                    {
                        Launch = "niconico://" + id,

                        Visual = new ToastVisual()
                        {
                            BindingGeneric = new ToastBindingGeneric()
                            {
                                Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = title,
                                },

                                new AdaptiveText()
                                {
                                    Text = "キャッシュ完了",
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                },

                                new AdaptiveText()
                                {
                                    Text = "ここをタップして再生を開始",
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                }
                            },
                                /*
                                AppLogoOverride = new ToastGenericAppLogo()
                                {
                                    Source = "oneAlarm.png"
                                }
                                */
                            }
                        },
                        /*
                        Actions = new ToastActionsCustom()
                        {
                            Buttons =
                            {
                                new ToastButton("check", "check")
                                {
                                    ImageUri = "check.png"
                                },

                                new ToastButton("cancel", "cancel")
                                {
                                    ImageUri = "cancel.png"
                                }
                            }
                        },
                        */
                        /*
                        Audio = new ToastAudio()
                        {
                            Src = new Uri("ms-winsoundevent:Notification.Reminder")
                        }
                        */
                    };

                    // トースト表示を実行
                    ToastNotification notification = new ToastNotification(content.GetXml());
                    notifier.Show(notification);

                }
                catch { }



            }

            deferral.Complete();
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

            // ダウンロードリクエストされたものが削除済み動画だった場合に対応
            // 削除されたDLリクエストを反映
            await SaveDownloadRequestItems();

            await TryNextCacheRequestedVideoDownload();
        }

        private async Task<BackgroundDownloader> ResetDownloader()
        {
            await BackgroundExecutionManager.RequestAccessAsync();

            const string BackgroundTransferCompletetionTaskNameBase = "HohoemaBGDLCompletion";
            var _BTCG = new BackgroundTransferCompletionGroup();

            var groupName = BackgroundTransferCompletetionTaskNameBase + Path.GetFileNameWithoutExtension(Path.GetRandomFileName());

            BackgroundTaskBuilder builder = new BackgroundTaskBuilder();

            builder.Name = groupName;
            builder.SetTrigger(_BTCG.Trigger);

            var status = BackgroundExecutionManager.GetAccessStatus();
            BackgroundTaskRegistration downloadProcessingTask = builder.Register();

            var _BackgroundDownloader = new BackgroundDownloader(_BTCG)
            {
                TransferGroup = BackgroundTransferGroup.CreateGroup(groupName)
            };

            _BackgroundTransferCompletionInfoMap.Add(groupName, new BackgroundTransferCompletionInfo()
            {
                Id = groupName,
                Downloader = _BackgroundDownloader,
                TaskRegistration = downloadProcessingTask,
                TransferCompletionGroup = _BTCG
            });

            return _BackgroundDownloader;            
        }

        private async Task RestoreCacheRequestedItems()
        {
            // ダウンロードリクエストされたアイテムのNicoVideoオブジェクトの作成
            // 及び、リクエストの再構築
            var list = await LoadDownloadRequestItems();
            List<NicoVideoCacheRequest> deletedItems = new List<NicoVideoCacheRequest>();
            foreach (var req in list)
            {
                var nicoVideo = await MediaManager.GetNicoVideoAsync(req.RawVideoId, true);
                var div = nicoVideo.GetDividedQualityNicoVideo(req.Quality);
                if (nicoVideo.IsDeleted)
                {
                    deletedItems.Add(req);
                }
                else
                {
                    await div.RestoreRequestCache(req);
                }

                Debug.Write(".");
            }

            
            foreach (var deleted in deletedItems)
            {
                _CacheDownloadPendingVideos.Remove(deleted);
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
                if (_DownloadOperations.ContainsKey(req))
                {
                    return _DownloadOperations[req];
                }
                else
                {
                    return null;
                }                
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

        public async Task<bool> AddCacheRequest(NicoVideoCacheRequest req, bool forceUpdate = false)
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
                    return true;
                }
            }

            var result = await _AddCacheRequest_Internal(req);

            if (result)
            {
                await SaveDownloadRequestItems();

                await TryNextCacheRequestedVideoDownload();
            }

            return result;
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



        internal async Task<bool> _AddCacheRequest_Internal(NicoVideoCacheRequest req)
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
                    return false;
                }
            }

            using (var pendingVideoLockReleaser = await _CacheDownloadPendingVideosLock.LockAsync())
            {
                _CacheDownloadPendingVideos.Add(req);
            }

            Requested?.Invoke(this, req);

            return true;
        }

        public async Task<bool> RemoveCacheRequest(string rawVideoId, NicoVideoQuality quality)
        {
            NicoVideoCacheRequest removeTarget = null;
            using (var pendingVideoLockReleaser = await _CacheDownloadPendingVideosLock.LockAsync())
            {
                removeTarget = _CacheDownloadPendingVideos.SingleOrDefault(x => x.RawVideoId == rawVideoId && x.Quality == quality);
            }

            // ダウンロード中タスクを削除（DLのキャンセル）

            bool removed = false;
            if (removeTarget != null)
            {
                await RemoveDownloadOperation(removeTarget);

                using (var pendingVideoLockReleaser = await _CacheDownloadPendingVideosLock.LockAsync())
                {
                    _CacheDownloadPendingVideos.Remove(removeTarget);
                }

                await SaveDownloadRequestItems();

                RequestCanceled?.Invoke(this, removeTarget);

                removed = true;
            }

            await TryNextCacheRequestedVideoDownload();

            return removed;
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

                Uri uri = null;
                try
                {
                    uri = await nicoVideo.SetupWatchPageVisit(req.Quality);
                    if (uri == null)
                    {
                        throw new Exception($"can't download {req.Quality} quality Video, in {req.RawVideoId}.");
                    }
                }
                catch
                {
                    return null;
                }

                var downloader = await ResetDownloader();

                // 認証情報付きクッキーをダウンローダーのHttpヘッダにコピー
                // 動画ページアクセス後のクッキーが必須になるため、サインイン時ではなく
                // ダウンロード開始直前のこのタイミングでクッキーをコピーしています
                var httpclinet = HohoemaApp.NiconicoContext.HttpClient;
                foreach (var header in httpclinet.DefaultRequestHeaders)
                {
                    downloader.SetRequestHeader(header.Key, header.Value);
                }

                // 保存先ファイルの確保
                var filename = div.VideoFileName;
                var videoFolder = await HohoemaApp.GetVideoCacheFolder();
                var videoFile = await videoFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);

                // ダウンロード操作を作成
                var operation = downloader.CreateDownload(uri, videoFile);

                downloader.CompletionGroup?.Enable();

                await AddDownloadOperation(req, operation);

                Debug.WriteLine($"キャッシュ準備完了: {req.RawVideoId} {req.Quality}");


                // ダウンロードを開始
                var action = operation.StartAsync();
                action.Progress = OnDownloadProgress;
                var task = action.AsTask().ContinueWith(OnDownloadCompleted).ConfigureAwait(false);

                Debug.WriteLine($"キャッシュ開始: {req.RawVideoId} {req.Quality}");

                return operation;
            }
        }

        private void OnDownloadProgress(object sender, DownloadOperation op)
        {
            Debug.WriteLine($"{op.RequestedUri}:{op.Progress.TotalBytesToReceive}");
            var info = NiconicoMediaManager.CacheRequestInfoFromFileName(op.ResultFile);
            DownloadProgress?.Invoke(this, info, op);
        }

        // ダウンロード完了
        private async Task OnDownloadCompleted(Task<DownloadOperation> prevTask)
        {
            if (prevTask.IsFaulted) { return; }

            Debug.WriteLine("キャッシュ完了");

            
            if (prevTask.Result != null)
            {
                var op = prevTask.Result;
                var info = NiconicoMediaManager.CacheRequestInfoFromFileName(op.ResultFile);
                await RemoveDownloadOperation(info);

                if (op.Progress.Status == BackgroundTransferStatus.Completed)
                {
                    if (op.Progress.TotalBytesToReceive == op.Progress.BytesReceived)
                    {
                        Debug.WriteLine("キャッシュ済み: " + op.ResultFile.Name);
                        DownloadCompleted?.Invoke(this, info, op.ResultFile.Path);
                    }
                    else
                    {
                        Debug.WriteLine("キャッシュキャンセル: " + op.ResultFile.Name);
                        DownloadCanceled?.Invoke(this, info);
                    }
                    using (var pendingVideoLockReleaser = await _CacheDownloadPendingVideosLock.LockAsync())
                    {
                        _CacheDownloadPendingVideos.Remove(info);
                    }
                }
                else
                {
                    Debug.WriteLine($"キャッシュ失敗: {op.ResultFile.Name} （再ダウンロードします）");
                    DownloadCanceled?.Invoke(this, info);

                    await AddCacheRequest(info.RawVideoId, info.Quality, forceUpdate: true);
                }

                try
                {
                    if (op.TransferGroup != null 
                        && _BackgroundTransferCompletionInfoMap.ContainsKey(op.TransferGroup.Name)
                        )
                    {
                        var btcInfo = _BackgroundTransferCompletionInfoMap[op.TransferGroup.Name];
                        btcInfo.TaskRegistration.Unregister(cancelTask: false);

                        _BackgroundTransferCompletionInfoMap.Remove(op.TransferGroup.Name);
                    }
                }
                catch
                {
                    Debug.WriteLine("failed unregister background download completion task.");
                }

            }



            await TryNextCacheRequestedVideoDownload();
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

                    await RestoreDonloadOperation(_info, task);


                    

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

        private async Task<DividedQualityNicoVideo> GetNicoVideo(NicoVideoCacheRequest cacheRequest)
        {
            var nicoVideo = await MediaManager.GetNicoVideoAsync(cacheRequest.RawVideoId);
            var div = nicoVideo.GetDividedQualityNicoVideo(cacheRequest.Quality);

            return div;
        }

        private async Task RestoreDonloadOperation(NicoVideoCacheRequest _info, DownloadOperation operation)
        {
            var div = await GetNicoVideo(_info);

            await AddDownloadOperation(_info, operation);

            await div.RestoreDownload(operation);

            var action = operation.AttachAsync();
            action.Progress = OnDownloadProgress;
            var task = action.AsTask()
                .ContinueWith(OnDownloadCompleted)
                .ConfigureAwait(false);
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
                if (_DownloadOperations.ContainsKey(req))
                {
                    op = _DownloadOperations[req];
                    if (op.Progress.BytesReceived != op.Progress.TotalBytesToReceive)
                    {
                        op.AttachAsync().Cancel();
                        await op.ResultFile.DeleteAsync();                        
                    }
                    _DownloadOperations.Remove(req);
                    --CurrentDownloadTaskCount;
                }
                else
                {
                    return;
                }
            }


            
        }
    }
}
