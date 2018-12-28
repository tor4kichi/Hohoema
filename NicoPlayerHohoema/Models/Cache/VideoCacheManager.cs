using Mntone.Nico2.Videos.Thumbnail;
using Mntone.Nico2.Videos.WatchAPI;
using NicoPlayerHohoema.Models.Helpers;
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
using Windows.Storage.Streams;
using Microsoft.Practices.Unity;
using System.Collections.Concurrent;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using Microsoft.Toolkit.Uwp.Helpers;
using System.Reactive.Concurrency;
using Prism.Commands;

namespace NicoPlayerHohoema.Models.Cache
{
    static public class CacheRequestHelper
    {
        static readonly private string VideoCacheRequestItemsKey = "VideoCacheRequestItems";
        static public List<NicoVideoCacheRequest> LoadCacheRequest()
        {
            var localObjectStorageHelper = new LocalObjectStorageHelper();
            return localObjectStorageHelper.Read<List<NicoVideoCacheRequest>>(VideoCacheRequestItemsKey) ?? new List<NicoVideoCacheRequest>();
        }

        static public void SaveCacheRequest(IEnumerable<NicoVideoCacheRequest> requests)
        {
            var localObjectStorageHelper = new LocalObjectStorageHelper();
            localObjectStorageHelper.Save(VideoCacheRequestItemsKey, requests.ToList());
        }
    }


    public class CacheSaveFolder
    {
        public CacheSaveFolder(CacheSettings cacheSettings)
        {
            CacheSettings = cacheSettings;
        }

        /// <summary>
		/// 動画キャッシュ保存先フォルダをチェックします
		/// 選択済みだがフォルダが見つからない場合に、トースト通知を行います。
		/// </summary>
		/// <returns></returns>
        /*
		public async Task CheckVideoCacheFolderState()
        {
            var cacheFolderState = await GetVideoCacheFolderState();

            if (cacheFolderState == CacheFolderAccessState.SelectedButNotExist)
            {
                var toastService = Container.Resolve<Services.NotificationService>();
                toastService.ShowToast(
                    "キャッシュが利用できません"
                    , "キャッシュ保存先フォルダが見つかりません。（ここをタップで設定画面を表示）"
                    , duration: Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Long
                    , toastActivatedAction: async () =>
                    {
                        await HohoemaApp.UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            var pm = Container.Resolve<PageManager>();
                            pm.OpenPage(HohoemaPageType.CacheManagement);
                        });
                    });
            }
        }
        */

        static StorageFolder _DownloadFolder;

        const string FolderAccessToken = "HohoemaVideoCache";

        // 旧バージョンで指定されたフォルダーでも動くようにするためにFolderAccessTokenを動的に扱う
        // 0.4.0以降はFolderAccessTokenで指定したトークンだが、
        // それ以前では ログインユーザーIDをトークンとして DL/Hohoema/ログインユーザーIDフォルダ/ をDLフォルダとして指定していた
        static string CurrentFolderAccessToken = null;

        public string PrevCacheFolderAccessToken { get; private set; }
        public CacheSettings CacheSettings { get; }

        static private async Task<StorageFolder> GetEnsureVideoFolder()
        {
            if (_DownloadFolder == null)
            {
                try
                {
                    // 既にフォルダを指定済みの場合
                    if (Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.ContainsItem(FolderAccessToken))
                    {
                        _DownloadFolder = await Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.GetFolderAsync(FolderAccessToken);
                        CurrentFolderAccessToken = FolderAccessToken;
                    }
                }
                catch (FileNotFoundException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    //					Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Remove(FolderAccessToken);
                    Debug.WriteLine(ex.ToString());
                }
            }

            return _DownloadFolder;
        }


        public async Task<bool> CanReadAccessVideoCacheFolder()
        {
            var status = await GetVideoCacheFolderState();
            return status == CacheFolderAccessState.Exist || status == CacheFolderAccessState.NotEnabled;
        }

        public async Task<bool> CanWriteAccessVideoCacheFolder()
        {
            var status = await GetVideoCacheFolderState();
            return status == CacheFolderAccessState.Exist;
        }

        public async Task<CacheFolderAccessState> GetVideoCacheFolderState()
        {
            if (false == CacheSettings.IsUserAcceptedCache)
            {
                return CacheFolderAccessState.NotAccepted;
            }

            try
            {
                var videoFolder = await GetEnsureVideoFolder();

                if (videoFolder == null)
                {
                    return CacheFolderAccessState.NotSelected;
                }
            }
            catch (FileNotFoundException)
            {
                return CacheFolderAccessState.SelectedButNotExist;
            }

            if (false == CacheSettings.IsEnableCache)
            {
                return CacheFolderAccessState.NotEnabled;
            }
            else
            {
                return CacheFolderAccessState.Exist;
            }
        }



        public async Task<StorageFolder> GetVideoCacheFolder()
        {
            try
            {
                return await GetEnsureVideoFolder();
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }


        public async Task<bool> ChangeUserDataFolder()
        {
            try
            {
                var folderPicker = new Windows.Storage.Pickers.FolderPicker();
                folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
                folderPicker.FileTypeFilter.Add("*");

                var folder = await folderPicker.PickSingleFolderAsync();
                if (folder != null && folder.Path != _DownloadFolder?.Path)
                {
                    if (false == String.IsNullOrWhiteSpace(CurrentFolderAccessToken))
                    {
                        Windows.Storage.AccessCache.StorageApplicationPermissions.
                        FutureAccessList.Remove(CurrentFolderAccessToken);
                        CurrentFolderAccessToken = null;
                    }

                    Windows.Storage.AccessCache.StorageApplicationPermissions.
                    FutureAccessList.AddOrReplace(FolderAccessToken, folder);

                    _DownloadFolder = folder;

                    CurrentFolderAccessToken = FolderAccessToken;
                }
                else
                {
                    return false;
                }
            }
            catch
            {

            }
            finally
            {
            }

            return true;
        }

    }

    public struct VideoCacheStateChangedEventArgs
    {
        public NicoVideoCacheRequest Request { get; set; }
        public NicoVideoCacheState CacheState { get; set; }
    }

    /// <summary>
    /// ニコニコ動画の動画やサムネイル画像、
    /// 動画情報など動画に関わるメディアを管理します
    /// </summary>
    public class VideoCacheManager : AsyncInitialize, IDisposable
	{
        public VideoCacheManager(
            IScheduler scheduler,
            NiconicoSession niconicoSession,
            Provider.NicoVideoProvider nicoVideoProvider,
            CacheSaveFolder cacheSaveFolder,
            CacheSettings cacheSettings
            )
        {
            Scheduler = scheduler;
            NiconicoSession = niconicoSession;
            NicoVideoProvider = nicoVideoProvider;
            CacheSaveFolder = cacheSaveFolder;
            CacheSettings = cacheSettings;
            NiconicoSession.LogIn += (sender, e) =>
            {
                _ = TryNextCacheRequestedVideoDownload();
            };


            Observable.Merge(
                _DownloadOperations.ObserveRemoveChanged().ToUnit(),
                _CacheDownloadPendingVideos.ObserveAddChanged().ToUnit()
                )
                .Throttle(TimeSpan.FromSeconds(1))
                .Subscribe(_ =>
                {
                    TryNextCacheRequestedVideoDownload().ConfigureAwait(false);
                });

        }

        public IScheduler Scheduler { get; }

        public NiconicoSession NiconicoSession { get; }
        public Provider.NicoVideoProvider NicoVideoProvider { get; }
        public CacheSaveFolder CacheSaveFolder { get; }
        public CacheSettings CacheSettings { get; }


        static readonly Regex NicoVideoIdRegex = new Regex("\\[((?:sm|so|lv)\\d+)\\]");

        static readonly Regex ExternalCachedNicoVideoIdRegex = new Regex("(?>sm|so|lv)\\d*");

        private const string TransferGroupName = @"hohoema_video";
        BackgroundTransferGroup _NicoCacheVideoBGTransferGroup = BackgroundTransferGroup.CreateGroup(TransferGroupName);

        private CacheManagerState State { get; set; } = CacheManagerState.NotInitialize;


        public event EventHandler<NicoVideoCacheProgress> DownloadProgress;

        public event EventHandler<NicoVideoCacheRequest> Requested;
        public event EventHandler<NicoVideoCacheRequest> RequestCanceled;

        public event EventHandler<VideoCacheStateChangedEventArgs> VideoCacheStateChanged;

        Helpers.AsyncLock _CacheRequestProcessingLock = new Helpers.AsyncLock();
        ConcurrentDictionary<string, List<NicoVideoCacheInfo>> _CacheVideos = new ConcurrentDictionary<string, List<NicoVideoCacheInfo>>();
        ObservableCollection<NicoVideoCacheRequest> _CacheDownloadPendingVideos = new ObservableCollection<NicoVideoCacheRequest>();
        ObservableCollection<NicoVideoCacheProgress> _DownloadOperations = new ObservableCollection<NicoVideoCacheProgress>();


        #region Commands


        private DelegateCommand<Interfaces.IVideoContent> _AddCacheRequestCommand;
        public DelegateCommand<Interfaces.IVideoContent> AddCacheRequestCommand => _AddCacheRequestCommand
            ?? (_AddCacheRequestCommand = new DelegateCommand<Interfaces.IVideoContent>(video => 
            {
                _ = RequestCache(video.Id);
            }));


        private DelegateCommand<Interfaces.IVideoContent> _DeleteCacheRequestCommand;
        public DelegateCommand<Interfaces.IVideoContent> DeleteCacheRequestCommand => _DeleteCacheRequestCommand
            ?? (_DeleteCacheRequestCommand = new DelegateCommand<Interfaces.IVideoContent>(video =>
            {
                _ = DeleteCachedVideo(video.Id);
            }
            , video => video != null && this.CheckCached(video.Id)
            ));


        #endregion



        static public NicoVideoQuality GetQualityFromFileName(string fileName)
        {
            var split = fileName.Split('.').Skip(1).Reverse().Take(2);

            if (!split.Any()) { throw new NotSupportedException("filename not contain extension. "); }

            if (split.Count() == 1)
            {
                return NicoVideoQuality.Smile_Original;
            }
            else
            {
                var qualityTypeName = split.ElementAt(1);
                switch (qualityTypeName)
                {
                    case "low":
                        return NicoVideoQuality.Smile_Low;
                    case "dmc_high":
                        return NicoVideoQuality.Dmc_High;
                    case "dmc_midium":
                        return NicoVideoQuality.Dmc_Midium;
                    case "dmc_low":
                        return NicoVideoQuality.Dmc_Low;
                    case "dmc_mobile":
                        return NicoVideoQuality.Dmc_Mobile;
                    default:
                        return NicoVideoQuality.Smile_Original;
                }
            }
        }

        static public string MakeCacheVideoFileName(string title, string videoId, MovieType videoType, NicoVideoQuality quality)
        {
            string toQualityNameExtention;
            // Note: 後尾に.mp4はダミー拡張子
            // Path.ChangeExtention実行時に動画タイトルにドットが含まれている場合に問題が発生しないようにするためのもの
            var filename = $"{title.ToSafeFilePath()} - [{videoId}].mp4"; 
            switch (quality)
            {
                case NicoVideoQuality.Smile_Original:
                    toQualityNameExtention = Path.ChangeExtension(filename, $".{videoType.ToString().ToLower()}");
                    break;
                case NicoVideoQuality.Smile_Low:
                    toQualityNameExtention = Path.ChangeExtension(filename, $".low.{videoType.ToString().ToLower()}");
                    break;
                case NicoVideoQuality.Dmc_High:
                    toQualityNameExtention = Path.ChangeExtension(filename, $".dmc_high.{videoType.ToString().ToLower()}");
                    break;
                case NicoVideoQuality.Dmc_Midium:
                    toQualityNameExtention = Path.ChangeExtension(filename, $".dmc_midium.{videoType.ToString().ToLower()}");
                    break;
                case NicoVideoQuality.Dmc_Low:
                    toQualityNameExtention = Path.ChangeExtension(filename, $".dmc_low.{videoType.ToString().ToLower()}");
                    break;
                case NicoVideoQuality.Dmc_Mobile:
                    toQualityNameExtention = Path.ChangeExtension(filename, $".dmc_mobile.{videoType.ToString().ToLower()}");
                    break;
                default:
                    throw new NotSupportedException(quality.ToString());
            }

            return toQualityNameExtention;
        }

        static public NicoVideoCacheRequest CacheRequestInfoFromFileName(IStorageFile file)
        {
            // キャッシュリクエストを削除
            // 2重に拡張子を利用しているので二回GetFileNameWithoutExtensionを掛けることでIDを取得
            var match = NicoVideoIdRegex.Match(file.Name);
            if (match != null)
            {
                var id = match.Groups[1].Value;
                var quality = GetQualityFromFileName(file.Name);

                return new NicoVideoCacheRequest() { RawVideoId = id, Quality = quality };
            }
            else
            {
                throw new Exception();
            }
        }


        // ダウンロードライン数（再生中DLも含める）
        // 未登録ユーザー = 1
        // 通常会員       = 1
        // プレミアム会員 = 3
        public const int MaxDownloadLineCount = 1;
        public const int MaxDownloadLineCount_Premium = 3;

        public int CurrentDownloadTaskCount => _DownloadOperations.Count;

        public bool CanAddDownloadLine
        {
            get
            {
                return NiconicoSession.IsPremiumAccount
                    ? CurrentDownloadTaskCount < MaxDownloadLineCount_Premium
                    : CurrentDownloadTaskCount < MaxDownloadLineCount;
            }

        }

        

        /// <summary>
        /// ユーザーがキャッシュフォルダを変更した際に
        /// HohoemaAppから呼び出されます
        /// </summary>
        /// <returns></returns>
        internal async Task OnCacheFolderChanged()
        {
            var prevState = State;

            await SuspendCacheDownload();

            // TODO: 現在データを破棄して、変更されたフォルダの内容で初期化しなおす
            _CacheVideos.Clear();

            await RetrieveCacheCompletedVideos();

            if (prevState == CacheManagerState.Running)
            {
                await ResumeCacheDownload();
            }
        }


		public void Dispose()
		{
            RemoveProgressToast();
        }

        protected override Task OnInitializeAsync(CancellationToken token)
        {
            Debug.Write($"キャッシュ情報のリストアを開始");

            Scheduler.ScheduleAsync(async (shceduler, cancelToken) =>
            {
                await Task.Delay(3000);

                using (var releaser = await _CacheRequestProcessingLock.LockAsync())
                {
                    // ダウンロード中のアイテムをリストア
                    await RestoreBackgroundDownloadTask();

                    // キャッシュ完了したアイテムをキャッシュフォルダから検索
                    await RetrieveCacheCompletedVideos();
                }

                // ダウンロード待機中のアイテムを復元
                await RestoreCacheRequestedItems();

                State = CacheManagerState.Running;
            });

            return Task.CompletedTask;
        }


        private async Task RetrieveCacheCompletedVideos()
		{
			var videoFolder = await CacheSaveFolder.GetVideoCacheFolder();
			if (videoFolder != null)
			{
				var files = await videoFolder.GetFilesAsync();

                foreach (var file in files)
                {
                    if (!(file.FileType == ".mp4" || file.FileType == ".flv"))
                    {
                        continue;
                    }

                    // ファイル名の最後方にある[]の中身の文字列を取得
                    // (動画タイトルに[]が含まれる可能性に配慮)
                    var match = NicoVideoIdRegex.Match(file.Name);
                    var id = match.Groups[1]?.Value;
                    NicoVideoQuality quality = NicoVideoQuality.Unknown;
                    if (string.IsNullOrEmpty(id))
                    {
                        // 外部キャッシュとして取得可能かをチェック
                        match = ExternalCachedNicoVideoIdRegex.Match(file.Name);

                        if (match.Groups.Count > 0)
                        {
                            id = match.Groups[match.Groups.Count - 1].Value;
                        }

                        // 動画IDを抽出不可だった場合はスキップ
                        if (string.IsNullOrEmpty(id))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        quality = VideoCacheManager.GetQualityFromFileName(file.Name);
                    }

                    var info = new NicoVideoCacheInfo()
                    {
                        RawVideoId = id,
                        Quality = quality,
                        FilePath = file.Path,
                        RequestAt = file.DateCreated.DateTime
                    };


                    _CacheVideos.AddOrUpdate(info.RawVideoId,
                    (x) =>
                    {
                        return new List<NicoVideoCacheInfo>() { info };
                    },
                    (x, y) =>
                    {
                        var tempinfo = y.FirstOrDefault(z => z.Quality == info.Quality);
                        if (tempinfo == null)
                        {
                            y.Add(info);
                        }
                        else
                        {
                            tempinfo.RequestAt = info.RequestAt;
                            tempinfo.FilePath = info.FilePath;
                        }
                        return y;
                    });

                    TriggerCacheStateChangedEventOnUIThread(info, NicoVideoCacheState.Cached);

                    Debug.Write(".");
                }
			}
		}


        #region Save & Load download request 

        private async Task SaveDownloadRequestItems()
        {
            if (!IsInitialized) { return; }

            CacheRequestHelper.SaveCacheRequest(_CacheDownloadPendingVideos);

            await Task.CompletedTask;
        }

        private async Task<IEnumerable<NicoVideoCacheRequest>> LoadDownloadRequestItems()
        {
            return await Task.FromResult(CacheRequestHelper.LoadCacheRequest());
        }

        private async Task RestoreCacheRequestedItems()
        {
            // ダウンロードリクエストされたアイテムのNicoVideoオブジェクトの作成
            // 及び、リクエストの再構築
            var list = await LoadDownloadRequestItems();

            foreach (var req in list)
            {
                await RequestCache(req);
            }

            Debug.WriteLine("");
            Debug.WriteLine($"{list.Count()} 件のダウンロードリクエストを復元");
        }


        #endregion

        /// <summary>
        /// キャッシュダウンロードのタスクから
        /// ダウンロードの情報を復元します
        /// </summary>
        /// <returns></returns>
        private async Task RestoreBackgroundDownloadTask()
        {
            // TODO: ユーザーのログイン情報を更新してダウンロードを再開する必要がある？
            // ユーザー情報の有効期限が切れていた場合には最初からダウンロードし直す必要があるかもしれません
            var tasks = await BackgroundDownloader.GetCurrentDownloadsForTransferGroupAsync(_NicoCacheVideoBGTransferGroup);
            foreach (var task in tasks)
            {
                NicoVideoCacheProgress info = null;
                try
                {
                    var _info = VideoCacheManager.CacheRequestInfoFromFileName(task.ResultFile);

                    var nicoVideo = new NicoVideo(NicoVideoProvider, NiconicoSession, this);
                    nicoVideo.RawVideoId = _info.RawVideoId;

                    var session = await nicoVideo.CreateVideoStreamingSession(_info.Quality, forceDownload: true);
                    if (session?.Quality == _info.Quality)
                    {
                        continue;
                    }

                    info = new NicoVideoCacheProgress(_info, task, session);

                    await RestoreDonloadOperation(info, task);

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
                        DownloadProgress?.Invoke(this, info);
                    }
                }
            }
        }

        

        Dictionary<int, NicoVideoCacheProgress> TaskIdToCacheProgress = new Dictionary<int, NicoVideoCacheProgress>();

        // 次のキャッシュダウンロードを試行
        // ダウンロード用の本数
        private async Task TryNextCacheRequestedVideoDownload()
        {
            if (State != CacheManagerState.Running) { return; }

            NicoVideoCacheRequest nextDownloadItem = null;

            using (var releaser = await _CacheRequestProcessingLock.LockAsync())
            {
                while (CanAddDownloadLine)
                {
                    if (_DownloadOperations.Count == 0)
                    {
                        nextDownloadItem = _CacheDownloadPendingVideos
                            .FirstOrDefault();
                    }

                    if (nextDownloadItem == null)
                    {
                        break;
                    }

                    if (_DownloadOperations.Any(x => x.RawVideoId == nextDownloadItem.RawVideoId && x.Quality == nextDownloadItem.Quality))
                    {
                        _CacheDownloadPendingVideos.Remove(nextDownloadItem);
                        break;
                    }

                    Debug.WriteLine($"キャッシュ準備を開始: {nextDownloadItem.RawVideoId} {nextDownloadItem.Quality}");

                    // 動画ダウンロードURLを取得                    
                    var nicoVideo = new NicoVideo(NicoVideoProvider, NiconicoSession, this);
                    nicoVideo.RawVideoId = nextDownloadItem.RawVideoId;
                    var videoInfo = await NicoVideoProvider.GetNicoVideoInfo(nextDownloadItem.RawVideoId);

                    // DownloadSessionを保持して、再生完了時にDisposeさせる必要がある
                    var downloadSession = await nicoVideo.CreateVideoStreamingSession(nextDownloadItem.Quality, forceDownload: true);

                    var uri = await downloadSession.GetDownloadUrlAndSetupDonwloadSession();

                    var downloader = new BackgroundDownloader()
                    {
                        TransferGroup = _NicoCacheVideoBGTransferGroup
                    };

                    downloader.SuccessToastNotification = MakeSuccessToastNotification(videoInfo);
                    downloader.FailureToastNotification = MakeFailureToastNotification(videoInfo);

                    // 保存先ファイルの確保
                    var filename = VideoCacheManager.MakeCacheVideoFileName(
                        videoInfo.Title,
                        nextDownloadItem.RawVideoId,
                        videoInfo.MovieType,
                        downloadSession.Quality
                        );

                    var videoFolder = await CacheSaveFolder.GetVideoCacheFolder();
                    var videoFile = await videoFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);

                    // ダウンロード操作を作成
                    var operation = downloader.CreateDownload(uri, videoFile);

                    var progress = new NicoVideoCacheProgress(nextDownloadItem, operation, downloadSession);
                    await AddDownloadOperation(progress);

                   
                    Debug.WriteLine($"キャッシュ準備完了: {nextDownloadItem.RawVideoId} {nextDownloadItem.Quality}");


                    // ダウンロードを開始
                    /*
                    if (Helpers.ApiContractHelper.IsFallCreatorsUpdateAvailable)
                    {
                        operation.IsRandomAccessRequired = true;
                    }
                    */

                    var action = operation.StartAsync();
                    action.Progress = OnDownloadProgress;
                    var task = action.AsTask();
                    TaskIdToCacheProgress.Add(task.Id, progress);
                    var _ = task.ContinueWith(OnDownloadCompleted);


                    TriggerCacheStateChangedEventOnUIThread(progress, NicoVideoCacheState.Downloading);

                    Debug.WriteLine($"キャッシュ開始: {nextDownloadItem.RawVideoId} {nextDownloadItem.Quality}");

                    SendUpdatableToastWithProgress(videoInfo.Title, nextDownloadItem);


                    // DL作業を作成できたらDL待ちリストから削除
                    _CacheDownloadPendingVideos.Remove(nextDownloadItem);

                    await SaveDownloadRequestItems();
                }
            }
        }


        private ToastNotification MakeSuccessToastNotification(Database.NicoVideo info)
        {
            // トーストのレイアウトを作成
            ToastContent content = new ToastContent()
            {
                Launch = "niconico://" + info.RawVideoId,

                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = info.Title,
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
            return new ToastNotification(content.GetXml());
        }

        private ToastNotification MakeFailureToastNotification(Database.NicoVideo info)
        {
            // トーストのレイアウトを作成
            ToastContent content = new ToastContent()
            {
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = info.Title,
                                },

                                new AdaptiveText()
                                {
                                    Text = "キャッシュに失敗（またはキャンセル）",
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                },
                                /*
                                new AdaptiveText()
                                {
                                    Text = "再ダウンロード",
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                }
                                */
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

            return new ToastNotification(content.GetXml());
        }

        public async Task<bool> DeleteCachedVideo(string videoId, NicoVideoQuality quality)
        {
            using (var releaser = await _CacheRequestProcessingLock.LockAsync())
            {
                if (_CacheVideos.TryGetValue(videoId, out var cachedItems))
                {
                    var removeCached = cachedItems.FirstOrDefault(x => x.RawVideoId == videoId && x.Quality == quality);
                    if (removeCached != null)
                    {
                        var result = await removeCached.Delete();
                        if (result)
                        {
                            cachedItems.Remove(removeCached);
                        }

                        if (cachedItems.Count == 0)
                        {
                            _CacheVideos.TryRemove(videoId, out var list);
                        }

                        RequestCanceled?.Invoke(this, removeCached);


                        TriggerCacheStateChangedEventOnUIThread(removeCached, NicoVideoCacheState.NotCacheRequested);


                        return result;
                    }
                }

                return false;
            }
        }

        public async Task<int> DeleteCachedVideo(string videoId)
        {
            int deletedCount = 0;
            if (_CacheVideos.TryRemove(videoId, out var cachedItems))
            {
                foreach (var target in cachedItems)
                {
                    var result = await target.Delete();

                    if (cachedItems.Count == 0)
                    {
                        _CacheVideos.TryRemove(videoId, out var list);
                        break;
                    }

                    RequestCanceled?.Invoke(this, target);

                    TriggerCacheStateChangedEventOnUIThread(target, NicoVideoCacheState.NotCacheRequested);

                    if (result)
                    {
                        deletedCount++;
                    }
                }
            }

            return deletedCount;
        }


        public async Task<List<NicoVideoCacheInfo>> EnumerateCacheVideosAsync()
        {
            using (var releaser = await _CacheRequestProcessingLock.LockAsync())
            {
                return _CacheVideos.SelectMany(x => x.Value).ToList();
            }
        }

        public async Task<List<NicoVideoCacheRequest>> EnumerateCacheRequestedVideosAsync()
        {
            List<NicoVideoCacheRequest> list = new List<NicoVideoCacheRequest>();

            using (var releaser = await _CacheRequestProcessingLock.LockAsync())
            {
                list.AddRange(_CacheVideos.SelectMany(x => x.Value));
                list.AddRange(_CacheDownloadPendingVideos);
            }

            var progressItems = await GetDownloadProgressVideosAsync();
            list.AddRange(progressItems);

            return list;
        }

        public async Task<List<NicoVideoCacheRequest>> GetCacheRequest(string videoId)
        {
            using (var releaser = await _CacheRequestProcessingLock.LockAsync())
            {
                var pendingItems = _CacheDownloadPendingVideos.Where(x => x.RawVideoId == videoId);
                var downloadingItems = _DownloadOperations.Where(x => x.RawVideoId == videoId);
                if (_CacheVideos.TryGetValue(videoId, out var list))
                {
                    return downloadingItems.Concat(pendingItems).Concat(list).ToList();
                }
                else
                {
                    return downloadingItems.Concat(pendingItems).ToList();
                }
            }
        }

        public async Task<List<NicoVideoCacheProgress>> GetDownloadProgressVideosAsync()
        {
            using (var releaser2 = await _CacheRequestProcessingLock.LockAsync())
            {
                return _DownloadOperations.ToList();
            }
        }


        public async Task<NicoVideoCacheProgress> GetCacheProgress(string videoId, NicoVideoQuality quality)
        {
            using (var releaser2 = await _CacheRequestProcessingLock.LockAsync())
            {
                return _DownloadOperations.FirstOrDefault(x => x.RawVideoId == videoId && x.Quality == quality);
            }
        }





        public NicoVideoCacheInfo GetCacheInfo(string videoId, NicoVideoQuality quality)
        {
            if (_CacheVideos.TryGetValue(videoId, out var list))
            {
                return list.FirstOrDefault(x => x.Quality == quality);
            }
            else
            {
                return null;
            }
        }



        public async Task RequestCache(NicoVideoCacheRequest req)
        {
            var requests = await GetCacheRequest(req.RawVideoId);
            var already = requests.FirstOrDefault(x => x.RawVideoId == req.RawVideoId && x.Quality == req.Quality);
            if (already != null)
            {
                req.RequestAt = already.RequestAt;
            }
            else
            {
                using (var releaser2 = await _CacheRequestProcessingLock.LockAsync())
                {
                    // 画質指定が無い場合はデフォルトのキャッシュ画質を選択
                    if (req.Quality == NicoVideoQuality.Unknown)
                    {
                        req.Quality = CacheSettings.DefaultCacheQuality;
                    }

                    _CacheDownloadPendingVideos.Add(req);

                    Requested?.Invoke(this, req);
                    TriggerCacheStateChangedEventOnUIThread(req, NicoVideoCacheState.Pending);
                }
            }

            await SaveDownloadRequestItems();
        }

        /// <summary>
		/// キャッシュリクエストをキューの最後尾に積みます
		/// 通常のダウンロードリクエストではこちらを利用します
		/// </summary>
		/// <param name="req"></param>
		/// <returns></returns>
		public Task RequestCache(string rawVideoId, NicoVideoQuality quality = NicoVideoQuality.Unknown, bool forceUpdate = false)
        {
            var req = new NicoVideoCacheRequest()
            {
                RawVideoId = rawVideoId,
                Quality = quality,
                RequestAt = DateTime.Now
            };

            return RequestCache(req);
        }



        public async Task<int> CancelCacheRequest(string videoId)
        {
            var items = await GetCacheRequest(videoId);

            foreach (var item in items)
            {
                await CancelCacheRequest(item.RawVideoId, item.Quality);
            }

            return items.Count();
        }

        public async Task<bool> CancelCacheRequest(string rawVideoId, NicoVideoQuality quality)
        {
            bool removed = false;
            var items = await GetCacheRequest(rawVideoId);
            var item = items.FirstOrDefault(x => x.Quality == quality);
            if (item != null)
            {
                switch (item.ToCacheState())
                {
                    case NicoVideoCacheState.Pending:
                        using (var releaser2 = await _CacheRequestProcessingLock.LockAsync())
                        {
                            var removeTarget = _CacheDownloadPendingVideos.FirstOrDefault(x => x.RawVideoId == rawVideoId && x.Quality == quality);
                            removed = _CacheDownloadPendingVideos.Remove(removeTarget);
                            await SaveDownloadRequestItems();
                        }
                        break;
                    case NicoVideoCacheState.Downloading:
                        var canceledReq = await CancelDownload(rawVideoId, quality);

                        await Task.Delay(500);

                        using (var releaser2 = await _CacheRequestProcessingLock.LockAsync())
                        {
                            var removeTarget = _CacheDownloadPendingVideos.FirstOrDefault(x => x.RawVideoId == rawVideoId && x.Quality == quality);
                            removed = _CacheDownloadPendingVideos.Remove(removeTarget);
                            await SaveDownloadRequestItems();
                        }
                        removed = canceledReq != null;
                        break;
                    case NicoVideoCacheState.Cached:
                        removed = await DeleteCachedVideo(rawVideoId, quality);
                        break;
                    default:
                        break;
                }
            }

            if (removed)
            {
                RequestCanceled?.Invoke(this, item);
                TriggerCacheStateChangedEventOnUIThread(item, NicoVideoCacheState.NotCacheRequested);

                return true;
            }
            else
            {
                return false;
            }
        }



        /// <summary>
        /// ダウンロードを停止します。
        /// 現在ダウンロード中のアイテムはキャンセルしてPendingに積み直します
        /// </summary>
        /// <returns></returns>
        public async Task SuspendCacheDownload()
        {
            if (State != CacheManagerState.Running) { return; }

            List<NicoVideoCacheProgress> operations;
            using (var releaser2 = await _CacheRequestProcessingLock.LockAsync())
            {
                operations = _DownloadOperations.ToList();
                foreach (var progress in operations)
                {
                    await RemoveDownloadOperation(progress);
                    _CacheDownloadPendingVideos.Remove(progress);
                }

                State = CacheManagerState.SuspendDownload;
            }

            operations.Reverse();

            foreach (var progress in operations)
            {
                var cacheRequest = new NicoVideoCacheRequest()
                {
                    RawVideoId = progress.RawVideoId,
                    RequestAt = progress.RequestAt,
                    Quality = progress.Quality,
                    IsRequireForceUpdate = progress.IsRequireForceUpdate
                };
                await RequestCache(cacheRequest);
            }
        }

        /// <summary>
        /// ダウンロードを再開します
        /// </summary>
        /// <returns></returns>
        public async Task ResumeCacheDownload()
        {
            if (State != CacheManagerState.SuspendDownload) { return; }

            using (var releaser2 = await _CacheRequestProcessingLock.LockAsync())
            {
                State = CacheManagerState.Running;
            }

            await TryNextCacheRequestedVideoDownload();
        }


        #region Toast Notification

        // TODO: キャッシュ完了等のトースト通知でNotificationServiceを利用する

        /*
         *https://blogs.msdn.microsoft.com/tiles_and_toasts/2017/02/01/progress-ui-and-data-binding-inside-toast-notifications-windows-10-creators-update/
         */
        ToastNotification _ProgressToast;
        private void SendUpdatableToastWithProgress(string title, NicoVideoCacheRequest req)
        {
            if (!Services.Helpers.ApiContractHelper.IsCreatorsUpdateAvailable)
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
            if (!Services.Helpers.ApiContractHelper.IsCreatorsUpdateAvailable)
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
            if (!Services.Helpers.ApiContractHelper.IsCreatorsUpdateAvailable)
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

        #endregion


        private async void OnDownloadProgress(object sender, DownloadOperation op)
        {
            Debug.WriteLine($"{op.RequestedUri}:{op.Progress.TotalBytesToReceive}");
            var req = VideoCacheManager.CacheRequestInfoFromFileName(op.ResultFile);
            var progress = await GetCacheProgress(req.RawVideoId, req.Quality);
            progress.DownloadOperation = op;
            DownloadProgress?.Invoke(this, progress);

            UpdateProgressToast(req, op);
        }


        // ダウンロード完了
        private async Task OnDownloadCompleted(Task<DownloadOperation> prevTask)
        {
            // 進捗付きトースト表示を削除
            RemoveProgressToast();

            var progress = TaskIdToCacheProgress[prevTask.Id];

            await RemoveDownloadOperation(progress);

            TaskIdToCacheProgress.Remove(prevTask.Id);

            if (prevTask.IsFaulted)
            {
                Debug.WriteLine("キャッシュ失敗");


                TriggerCacheStateChangedEventOnUIThread(
                    new NicoVideoCacheRequest()
                    {
                        RawVideoId = progress.RawVideoId,
                        RequestAt = progress.RequestAt,
                        Quality = progress.Quality,
                        IsRequireForceUpdate = progress.IsRequireForceUpdate
                    },
                    NicoVideoCacheState.Pending
                );

                return;
            }

            Debug.WriteLine("キャッシュ完了");

            if (prevTask.Result != null)
            {

                var op = progress.DownloadOperation;

                if (op.Progress.Status == BackgroundTransferStatus.Completed)
                {
                    if (op.Progress.TotalBytesToReceive == op.Progress.BytesReceived)
                    {
                        Debug.WriteLine("キャッシュ済み: " + op.ResultFile.Name);
                        var cacheInfo = new NicoVideoCacheInfo(progress, op.ResultFile.Path);

                        _CacheVideos.AddOrUpdate(cacheInfo.RawVideoId,
                        (x) =>
                        {
                            return new List<NicoVideoCacheInfo>() { cacheInfo };
                        },
                        (x, y) =>
                        {
                            var tempinfo = y.FirstOrDefault(z => z.Quality == cacheInfo.Quality);
                            if (tempinfo == null)
                            {
                                y.Add(cacheInfo);
                            }
                            else
                            {
                                tempinfo.RequestAt = cacheInfo.RequestAt;
                                tempinfo.FilePath = cacheInfo.FilePath;
                            }
                            return y;
                        });

                        TriggerCacheStateChangedEventOnUIThread(progress, NicoVideoCacheState.Cached);

                        
                    }
                    else
                    {
                        Debug.WriteLine("キャッシュキャンセル: " + op.ResultFile.Name);

                        TriggerCacheStateChangedEventOnUIThread(
                            new NicoVideoCacheRequest()
                            {
                                RawVideoId = progress.RawVideoId,
                                RequestAt = progress.RequestAt,
                                Quality = progress.Quality,
                                IsRequireForceUpdate = progress.IsRequireForceUpdate
                            },
                            NicoVideoCacheState.Pending
                        );
                        
                    }
                }
                else
                {
                    Debug.WriteLine($"キャッシュ失敗: {op.ResultFile.Name} （再ダウンロードします）");

                    TriggerCacheStateChangedEventOnUIThread(
                        new NicoVideoCacheRequest()
                        {
                            RawVideoId = progress.RawVideoId,
                            RequestAt = progress.RequestAt,
                            Quality = progress.Quality,
                            IsRequireForceUpdate = progress.IsRequireForceUpdate
                        },
                        NicoVideoCacheState.Pending
                    );
                }
            }
        }


        private async Task<IEnumerable<NicoVideoCacheRequest>> CancelDownload(string videoId)
        {
            List<NicoVideoCacheProgress> items = null;
            using (var releaser2 = await _CacheRequestProcessingLock.LockAsync())
            {
                items = _DownloadOperations.Where(x => x.RawVideoId == videoId).ToList();
            }

            foreach (var progress in items)
            {
                await RemoveDownloadOperation(progress);
            }

            return items;
            
        }

        private async Task<NicoVideoCacheRequest> CancelDownload(string videoId, NicoVideoQuality quality)
        {
            NicoVideoCacheProgress progress = null;
            using (var releaser2 = await _CacheRequestProcessingLock.LockAsync())
            {
                progress = _DownloadOperations.FirstOrDefault(x => x.RawVideoId == videoId && x.Quality == quality);
            }

            if (progress == null) { return null; }


            await RemoveDownloadOperation(progress);

            TriggerCacheStateChangedEventOnUIThread(progress, NicoVideoCacheState.NotCacheRequested);

            return progress;
            
        }


        private async Task RestoreDonloadOperation(NicoVideoCacheProgress progress, DownloadOperation operation)
        {
            await AddDownloadOperation(progress);

            var action = operation.AttachAsync();
            action.Progress = OnDownloadProgress;
            var task = action.AsTask()
                .ContinueWith(OnDownloadCompleted)
                .ConfigureAwait(false);
        }

        private Task AddDownloadOperation(NicoVideoCacheProgress req)
        {
            _DownloadOperations.Add(req);

            return Task.CompletedTask;
        }

        private async Task RemoveDownloadOperation(NicoVideoCacheProgress req)
        {
            if (_DownloadOperations.Remove(req))
            {
                req.Session?.Dispose();

                var op = req.DownloadOperation;
                if (op.Progress.BytesReceived != op.Progress.TotalBytesToReceive)
                {
                    op.AttachAsync().Cancel();
                    await op.ResultFile.DeleteAsync();

                    
                }
            }
            else
            {
                return;
            }
        }

        
        internal async Task<bool> CheckCachedAsync(string contentId)
        {
            using (var releaser = await _CacheRequestProcessingLock.LockAsync())
            {
                if (_CacheVideos.TryGetValue(contentId, out var cacheInfoList))
                {
                    return cacheInfoList.Any(x => x.ToCacheState() == NicoVideoCacheState.Cached);
                }
                else
                {
                    return false;
                }
            }
        }

        internal bool CheckCached(string contentId)
        {
            if (_CacheVideos.TryGetValue(contentId, out var cacheInfoList))
            {
                return cacheInfoList.Any(x => x.ToCacheState() == NicoVideoCacheState.Cached);
            }
            else
            {
                return false;
            }
        }

        private void TriggerCacheStateChangedEventOnUIThread(NicoVideoCacheRequest req, NicoVideoCacheState cacheState)
        {
            Scheduler.Schedule(() => 
            {
                VideoCacheStateChanged?.Invoke(this, new VideoCacheStateChangedEventArgs()
                {
                    Request = req,
                    CacheState = cacheState
                });
            });
        }
    }


    public enum CacheManagerState
    {
        NotInitialize,
        Running,
        SuspendDownload,
    }








}
