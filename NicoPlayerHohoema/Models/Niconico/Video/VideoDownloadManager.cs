using Microsoft.Toolkit.Uwp.Notifications;
using NicoPlayerHohoema.Helpers;
using System;
using System.Collections.Concurrent;
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
using Windows.Foundation.Metadata;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.UI.Notifications;
using Windows.UI.ViewManagement;

namespace NicoPlayerHohoema.Models
{
    public delegate void VideoCacheRequestedEventHandler(object sender, NicoVideoCacheRequest request);
    public delegate void VideoCacheRequestCanceledEventHandler(object sender, NicoVideoCacheRequest request);
    public delegate void VideoCacheDownloadStartedEventHandler(object sender, NicoVideoCacheProgress progress);
    public delegate void VideoCacheDownloadProgressEventHandler(object sender, NicoVideoCacheProgress progress);
    public delegate void VideoCacheCompletedEventHandler(object sender, NicoVideoCacheInfo cacheInfo);
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


        private FolderBasedFileAccessor<IList<NicoVideoCacheRequest>> _CacheRequestedItemsFileAccessor;


        public HohoemaApp HohoemaApp { get; private set; }
        public VideoCacheManager MediaManager { get; private set; }

        public event VideoCacheRequestedEventHandler Requested;
        public event VideoCacheRequestCanceledEventHandler RequestCanceled;
        public event VideoCacheDownloadStartedEventHandler DownloadStarted;
        public event VideoCacheDownloadProgressEventHandler DownloadProgress;
        public event VideoCacheCompletedEventHandler DownloadCompleted;
        public event VideoCacheDownloadCanceledventHandler DownloadCanceled;


        AsyncLock _CacheDownloadPendingVideosLock = new AsyncLock();
        List<NicoVideoCacheRequest> _CacheDownloadPendingVideos;

        //        private BackgroundTransferCompletionGroup _BTCG = new BackgroundTransferCompletionGroup();

        //        private BackgroundDownloader _BackgroundDownloader;

        AsyncLock _DownloadOperationsLock = new AsyncLock();
        private List<NicoVideoCacheProgress> _DownloadOperations = new List<NicoVideoCacheProgress>();

        private AsyncLock _RegistrationBackgroundTaskLock = new AsyncLock();



        private Dictionary<string, BackgroundTransferCompletionInfo> _BackgroundTransferCompletionInfoMap = new Dictionary<string, BackgroundTransferCompletionInfo>();

        // ダウンロードライン数
        // 通常会員は1ライン（再生DL含む）
        // プレミアム会員は2ライン（再生DL含まず）
        public uint CurrentDownloadTaskCount { get; private set; }
        public const uint MaxDownloadLineCount_Ippan = 1;
        public const uint MaxDownloadLineCount_Premium = 1;


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

        public VideoDownloadManager(HohoemaApp hohoemaApp, VideoCacheManager mediaManager)
        {
            HohoemaApp = hohoemaApp;
            MediaManager = mediaManager;

            _CacheDownloadPendingVideos = new List<NicoVideoCacheRequest>();

            HohoemaApp.OnSignin += HohoemaApp_OnSignin;

            // ダウンロード完了をバックグラウンドで処理
            CoreApplication.BackgroundActivated += CoreApplication_BackgroundActivated;
            CoreApplication.Suspending += CoreApplication_Suspending;
        }

        private void CoreApplication_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            RemoveProgressToast();
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
            _CacheRequestedItemsFileAccessor = new FolderBasedFileAccessor<IList<NicoVideoCacheRequest>>(videoSaveFolder, CACHE_REQUESTED_FILENAME);

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

            using (var releaser = await _CacheDownloadPendingVideosLock.LockAsync())
            {
                foreach (var req in list)
                {
                    _CacheDownloadPendingVideos.Add(req);
                }
            }

            Debug.WriteLine("");
            Debug.WriteLine($"{list.Count} 件のダウンロードリクエストを復元");
        }

        
        public async Task<IEnumerable<NicoVideoCacheRequest>> GetCacheRequestedVideosAsync()
        {
            using (var releaser = await _CacheDownloadPendingVideosLock.LockAsync())
            using (var releaser2 = await _DownloadOperationsLock.LockAsync())
            {
                return _DownloadOperations.Concat(_CacheDownloadPendingVideos).ToList();
            }
        }
        

        #region Save & Load download request 

        private async Task SaveDownloadRequestItems()
        {
            if (!IsInitialized) { return; }

            if (_CacheDownloadPendingVideos.Count > 0)
            {
                await _CacheRequestedItemsFileAccessor.Save(_CacheDownloadPendingVideos.ToList());

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

        private async Task<IList<NicoVideoCacheRequest>> LoadDownloadRequestItems()
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
            if (!IsInitialized) { return; }

            if (!HohoemaApp.IsLoggedIn) { return; }


            NicoVideoCacheRequest nextDownloadItem = null;
            if (CanAddDownloadLine)
            {
                using (var releaser = await _CacheDownloadPendingVideosLock.LockAsync())
                {
                    nextDownloadItem = _CacheDownloadPendingVideos
                        .FirstOrDefault();
                }
            }

            if (nextDownloadItem != null)
            {
                var op = await DonwloadVideoInBackgroundTask(nextDownloadItem);
            }
        }

        internal async Task<bool> AddCacheRequest(NicoVideoCacheRequest req)
        {
            NicoVideoCacheRequest already = null;

            using (var releaser = await _CacheDownloadPendingVideosLock.LockAsync())
            {
                already = _CacheDownloadPendingVideos.FirstOrDefault(x => x.RawVideoId == req.RawVideoId && x.Quality == req.Quality);
            }

            if (already != null)
            {
                req.RequestAt = already.RequestAt;
            }
            else
            {
                Requested?.Invoke(this, req);
            }

            await SaveDownloadRequestItems();

            await TryNextCacheRequestedVideoDownload();

            return true;
        }

        /// <summary>
		/// キャッシュリクエストをキューの最後尾に積みます
		/// 通常のダウンロードリクエストではこちらを利用します
		/// </summary>
		/// <param name="req"></param>
		/// <returns></returns>
		internal Task AddCacheRequest(string rawVideoId, NicoVideoQuality quality, bool forceUpdate = false)
        {
            var req = new NicoVideoCacheRequest()
            {
                RawVideoId = rawVideoId,
                Quality = quality,
                RequestAt = DateTime.Now
            };

            return AddCacheRequest(req);
        }

        public async Task RemoveCacheRequest(string rawVideoId)
        {
            List<NicoVideoCacheRequest> targets = null;
            using (var releaser = await _CacheDownloadPendingVideosLock.LockAsync())
            {
                targets = _CacheDownloadPendingVideos.Where(x => x.RawVideoId == rawVideoId).ToList();
            }

            foreach (var target in targets)
            {
                await RemoveCacheRequest(target.RawVideoId, target.Quality);
            }
        }

        public async Task<bool> RemoveCacheRequest(string rawVideoId, NicoVideoQuality quality)
        {
            bool removed = false;

            using (var releaser = await _CacheDownloadPendingVideosLock.LockAsync())
            {
                var removeTarget = _CacheDownloadPendingVideos.SingleOrDefault(x => x.RawVideoId == rawVideoId && x.Quality == quality);
                
                // ダウンロード中タスクを削除（DLのキャンセル）
                if (removeTarget != null)
                {
                    _CacheDownloadPendingVideos.Remove(removeTarget);
                }
                else
                {
                    var cancelDlOp = _DownloadOperations.FirstOrDefault(x => x.RawVideoId == rawVideoId && x.Quality == quality);
                    await RemoveDownloadOperation(cancelDlOp);
                }


                /*
                using (var pendingVideoLockReleaser = await _CacheDownloadPendingVideosLock.LockAsync())
                {
                    _CacheDownloadPendingVideos.Remove(removeTarget);
                }

                await SaveDownloadRequestItems();

                RequestCanceled?.Invoke(this, removeTarget);

                */
                removed = true;
            }

            await TryNextCacheRequestedVideoDownload();

            return removed;
        }



        NicoVideoCacheRequest _CurrentRequest;


        // バックグラウンドで動画キャッシュのダウンロードを行うタスクを作成
        private async Task<DownloadOperation> DonwloadVideoInBackgroundTask(NicoVideoCacheRequest req)
        {
            using (var bgTaskLock = await _RegistrationBackgroundTaskLock.LockAsync())
            {
                if (_DownloadOperations.Any(x => x.RawVideoId == req.RawVideoId && x.Quality == req.Quality))
                {
                    return null;
                }

                Debug.WriteLine($"キャッシュ準備を開始: {req.RawVideoId} {req.Quality}");

                // 動画ダウンロードURLを取得
                var nicoVideo = new NicoVideo(req.RawVideoId, HohoemaApp.ContentProvider, HohoemaApp.NiconicoContext, HohoemaApp.CacheManager);
                var videoInfo = await HohoemaApp.ContentProvider.GetNicoVideoInfo(req.RawVideoId);

                // TODO: DownloadSessionを保持して、再生完了時にDisposeさせる必要がある
                var downloadSession = await nicoVideo.CreateVideoStreamingSession(req.Quality);

                var uri = await downloadSession.GetDownloadUrlAndSetupDonwloadSession();

                var downloader = await ResetDownloader();

                // 保存先ファイルの確保
                var filename = VideoCacheManager.MakeCacheVideoFileName(
                    videoInfo.Title,
                    req.RawVideoId,
                    videoInfo.MovieType,
                    req.Quality
                    );

                var videoFolder = await HohoemaApp.GetVideoCacheFolder();
                var videoFile = await videoFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);

                // ダウンロード操作を作成
                var operation = downloader.CreateDownload(uri, videoFile);

                downloader.CompletionGroup?.Enable();

                var progress = new NicoVideoCacheProgress(req, operation);
                await AddDownloadOperation(progress);

                Debug.WriteLine($"キャッシュ準備完了: {req.RawVideoId} {req.Quality}");


                // ダウンロードを開始
                var action = operation.StartAsync();
                action.Progress = OnDownloadProgress;
                var task = action.AsTask().ContinueWith(OnDownloadCompleted).ConfigureAwait(false);

                Debug.WriteLine($"キャッシュ開始: {req.RawVideoId} {req.Quality}");

                _CurrentRequest = req;
                SendUpdatableToastWithProgress(videoInfo.Title, req);



                return operation;
            }
        }


        /*
         *https://blogs.msdn.microsoft.com/tiles_and_toasts/2017/02/01/progress-ui-and-data-binding-inside-toast-notifications-windows-10-creators-update/
         */
        ToastNotification _ProgressToast;
        private void SendUpdatableToastWithProgress(string title, NicoVideoCacheRequest req)
        {
            if (!ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4))
            { 
                return;
            }

            // Define a tag value and a group value to uniquely identify a notification, in order to target it to apply the update later;
            string toastTag = $"{req.RawVideoId}_{req.Quality}";
            string toastGroup = "hohoema_cache_dl";

            // Construct the toast content with updatable data fields inside;
            var content = new ToastContent()
            {
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                {
                    new AdaptiveText()
                    {
                        Text = title,
                        HintStyle = AdaptiveTextStyle.Header
                    },

                    new AdaptiveProgressBar()
                    {
                        Value = new BindableProgressBarValue("progressValue"),
                        ValueStringOverride = new BindableString("progressString"),
                        Status = new BindableString("progressStatus")
                    }
                }
                    }
                },
                Actions = new ToastActionsCustom()
                {
                    Buttons =
                    {
                        new ToastButton("キャンセル", $"cache_cancel?id={req.RawVideoId}&quality={req.Quality}")
                        {
                            
                        }
                    }
                },
            };

            // Generate the toast notification;
            var toast = new ToastNotification(content.GetXml());

            // Assign the tag and group properties;
            toast.Tag = toastTag;
            toast.Group = toastGroup;

            // Define NotificationData property and add it to the toast notification to bind the initial data;
            // Data.Values are assigned with string values;
            toast.Data = new NotificationData();
            toast.Data.Values["progressValue"] = "0";
            toast.Data.Values["progressString"] = $"";
            toast.Data.Values["progressStatus"] = "download started";

            // Provide sequence number to prevent updating out-of-order or assign it with value 0 to indicate "always update";
            toast.Data.SequenceNumber = 1;

            toast.SuppressPopup = true;
            
            // Show the toast notification to the user;
            ToastNotificationManager.CreateToastNotifier().Show(toast);

            _ProgressToast = toast;
        }


        private void UpdateProgressToast(NicoVideoCacheRequest req, DownloadOperation op)
        {
            if (!ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4))
            {
                return;
            }

            // Construct a NotificationData object;
            string toastTag = $"{req.RawVideoId}_{req.Quality}";
            string toastGroup = "hohoema_cache_dl";

            var progress = op.Progress.BytesReceived / (double)op.Progress.TotalBytesToReceive;
            var progressText = (progress * 100).ToString("F0");
            // Create NotificationData with new values;
            // Make sure that sequence number is incremented since last update, or assign with value 0 for updating regardless of order;
            var data = new NotificationData { SequenceNumber = 0 };
            
            data.Values["progressValue"] = progress.ToString("F1"); // 固定小数点、整数部と小数一桁までを表示
            data.Values["progressString"] = $"{progressText}%";
            data.Values["progressStatus"] = "donwloading";

            // Updating a previously sent toast with tag, group, and new data;
            NotificationUpdateResult updateResult = ToastNotificationManager.CreateToastNotifier().Update(data, toastTag, toastGroup);
        }

        private void RemoveProgressToast()
        {
            if (!ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4))
            {
                return;
            }

            // Construct a NotificationData object;
            //            string toastTag = $"{req.RawVideoId}_{req.Quality}";
            //            string toastGroup = "hohoema_cache_dl";

            // Updating a previously sent toast with tag, group, and new data;
            if (_ProgressToast != null)
            {
                ToastNotificationManager.CreateToastNotifier().Hide(_ProgressToast);
                _ProgressToast = null;
            }
        }


        private void OnDownloadProgress(object sender, DownloadOperation op)
        {
            Debug.WriteLine($"{op.RequestedUri}:{op.Progress.TotalBytesToReceive}");
            var req = VideoCacheManager.CacheRequestInfoFromFileName(op.ResultFile);
            var progress = new NicoVideoCacheProgress(req, op);
            DownloadProgress?.Invoke(this, progress);

            UpdateProgressToast(req, op);
        }

        // ダウンロード完了
        private async Task OnDownloadCompleted(Task<DownloadOperation> prevTask)
        {
            // 進捗付きトースト表示を削除
            RemoveProgressToast();

            
            if (prevTask.IsFaulted)
            {
                try
                {
                    Debug.WriteLine("キャッシュ失敗");

                    var op = prevTask.Result;
                    var info = VideoCacheManager.CacheRequestInfoFromFileName(op.ResultFile);
                    var progress = new NicoVideoCacheProgress(info, op);
                    await RemoveDownloadOperation(progress);
                    DownloadCanceled?.Invoke(this, info);
                    return;
                }
                catch
                {

                }
                finally
                {
                    _CurrentRequest = null;
                }
            }

            Debug.WriteLine("キャッシュ完了");

            
            if (prevTask.Result != null)
            {
                var op = prevTask.Result;
                var req = VideoCacheManager.CacheRequestInfoFromFileName(op.ResultFile);
                var progress = new NicoVideoCacheProgress(req, op);
                await RemoveDownloadOperation(progress);

                if (op.Progress.Status == BackgroundTransferStatus.Completed)
                {
                    if (op.Progress.TotalBytesToReceive == op.Progress.BytesReceived)
                    {
                        Debug.WriteLine("キャッシュ済み: " + op.ResultFile.Name);
                        var cacheInfo = new NicoVideoCacheInfo(req, op.ResultFile.Path);
                        DownloadCompleted?.Invoke(this, cacheInfo);
                    }
                    else
                    {
                        Debug.WriteLine("キャッシュキャンセル: " + op.ResultFile.Name);
                        DownloadCanceled?.Invoke(this, req);
                    }
                }
                else
                {
                    Debug.WriteLine($"キャッシュ失敗: {op.ResultFile.Name} （再ダウンロードします）");
                    DownloadCanceled?.Invoke(this, req);

                    await AddCacheRequest(req.RawVideoId, req.Quality, forceUpdate: true);
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
                    var _info = VideoCacheManager.CacheRequestInfoFromFileName(task.ResultFile);
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

        private async Task RestoreDonloadOperation(NicoVideoCacheRequest _info, DownloadOperation operation)
        {
            var progress = new NicoVideoCacheProgress(_info, operation);
            await AddDownloadOperation(progress);

            var action = operation.AttachAsync();
            action.Progress = OnDownloadProgress;
            var task = action.AsTask()
                .ContinueWith(OnDownloadCompleted)
                .ConfigureAwait(false);
        }

        private async Task AddDownloadOperation(NicoVideoCacheProgress req)
        {
            using (var releaser = await _DownloadOperationsLock.LockAsync())
            {
                _DownloadOperations.Add(req);
                ++CurrentDownloadTaskCount;

                // ダウンロード開始イベントをトリガー
                DownloadStarted?.Invoke(this, req);
            }
        }

        private async Task RemoveDownloadOperation(NicoVideoCacheProgress req)
        {
            using (var releaser = await _DownloadOperationsLock.LockAsync())
            {
                if (_DownloadOperations.Remove(req))
                {
                    var op = req.DownloadOperation;
                    if (op.Progress.BytesReceived != op.Progress.TotalBytesToReceive)
                    {
                        op.AttachAsync().Cancel();
                        await op.ResultFile.DeleteAsync();
                    }

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
