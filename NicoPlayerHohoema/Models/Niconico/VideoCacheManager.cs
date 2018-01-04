using Mntone.Nico2.Videos.Thumbnail;
using Mntone.Nico2.Videos.WatchAPI;
using NicoPlayerHohoema.Helpers;
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
using NicoPlayerHohoema.Models.Db;
using System.Collections.Concurrent;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;

namespace NicoPlayerHohoema.Models
{

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
        static readonly Regex NicoVideoIdRegex = new Regex("\\[((?:sm|so|lv)\\d+)\\]");

        static readonly Regex ExternalCachedNicoVideoIdRegex = new Regex("(?>sm|so|lv)\\d*");

        const string CACHE_REQUESTED_FILENAME = "cache_requested.json";
        FolderBasedFileAccessor<IList<NicoVideoCacheRequest>> _CacheRequestedItemsFileAccessor;


        private const string TransferGroupName = @"hohoema_video";
        BackgroundTransferGroup _NicoCacheVideoBGTransferGroup = BackgroundTransferGroup.CreateGroup(TransferGroupName);
            



        public event EventHandler<NicoVideoCacheProgress> DownloadProgress;

        public event EventHandler<NicoVideoCacheRequest> Requested;
        public event EventHandler<NicoVideoCacheRequest> RequestCanceled;

        public event EventHandler<VideoCacheStateChangedEventArgs> VideoCacheStateChanged;


        HohoemaApp _HohoemaApp;


        AsyncLock _CacheRequestProcessingLock = new AsyncLock();
        ConcurrentDictionary<string, List<NicoVideoCacheInfo>> _CacheVideos = new ConcurrentDictionary<string, List<NicoVideoCacheInfo>>();
        ObservableCollection<NicoVideoCacheRequest> _CacheDownloadPendingVideos = new ObservableCollection<NicoVideoCacheRequest>();
        ObservableCollection<NicoVideoCacheProgress> _DownloadOperations = new ObservableCollection<NicoVideoCacheProgress>();




        public static NicoVideoQuality GetQualityFromFileName(string fileName)
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

        public static string MakeCacheVideoFileName(string title, string videoId, MovieType videoType, NicoVideoQuality quality)
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

        public static NicoVideoCacheRequest CacheRequestInfoFromFileName(IStorageFile file)
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

        static internal async Task<VideoCacheManager> Create(HohoemaApp app)
		{
			var man = new VideoCacheManager(app);

            await man.Initialize();
//            await man.RetrieveCacheCompletedVideos();

            return man;
		}

        // ダウンロードライン数
        // 通常会員は1ライン（再生DL含む）
        // プレミアム会員は2ライン（再生DL含まず）
        public uint CurrentDownloadTaskCount { get; private set; }
        public const uint MaxDownloadLineCount_Ippan = 1;
        public const uint MaxDownloadLineCount_Premium = 1;



        public bool CanAddDownloadLine
        {
            get
            {
                return _HohoemaApp.IsPremiumUser
                    ? CurrentDownloadTaskCount < MaxDownloadLineCount_Premium
                    : CurrentDownloadTaskCount < MaxDownloadLineCount_Ippan;
            }

        }



        private VideoCacheManager(HohoemaApp app)
		{
            _HohoemaApp = app;

            _HohoemaApp.OnSignin += _HohoemaApp_OnSignin;


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


        /// <summary>
        /// ユーザーがキャッシュフォルダを変更した際に
        /// HohoemaAppから呼び出されます
        /// </summary>
        /// <returns></returns>
        internal async Task OnCacheFolderChanged()
        {
            StopCacheDownload();

            // TODO: 現在データを破棄して、変更されたフォルダの内容で初期化しなおす
            _CacheVideos.Clear();

            await RetrieveCacheCompletedVideos();
        }


        private async void _HohoemaApp_OnSignin()
		{
            // 初期化をバックグラウンドタスクに登録
            //var updater = 
            //updater.Completed += (sender, item) => 
            //{
            //				IsInitialized = true;
            //};

            await TryNextCacheRequestedVideoDownload();
		}

		public void Dispose()
		{
            RemoveProgressToast();
        }

        protected override Task OnInitializeAsync(CancellationToken token)
        {
            Debug.Write($"キャッシュ情報のリストアを開始");

            return HohoemaApp.UIDispatcher.RunIdleAsync(async (_) =>
            {
                // ダウンロード中のアイテムをリストア
                await RestoreBackgroundDownloadTask();

                // キャッシュ完了したアイテムをキャッシュフォルダから検索
                await RetrieveCacheCompletedVideos();

                // キャッシュリクエストファイルのアクセサーを初期化
                var videoSaveFolder = await _HohoemaApp.GetApplicationLocalDataFolder();
                _CacheRequestedItemsFileAccessor = new FolderBasedFileAccessor<IList<NicoVideoCacheRequest>>(videoSaveFolder, CACHE_REQUESTED_FILENAME);

                // ダウンロード待機中のアイテムを復元
                await RestoreCacheRequestedItems();
            })
            .AsTask();
        }


        private async Task RetrieveCacheCompletedVideos()
		{
			var videoFolder = await _HohoemaApp.GetVideoCacheFolder();
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


                    using (var releaser = await _CacheRequestProcessingLock.LockAsync())
                    {
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
                    }

                    VideoCacheStateChanged?.Invoke(this, new VideoCacheStateChangedEventArgs()
                    {
                        Request = info,
                        CacheState = NicoVideoCacheState.Cached
                    });

                    Debug.Write(".");
                }
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

        private async Task RestoreCacheRequestedItems()
        {
            // ダウンロードリクエストされたアイテムのNicoVideoオブジェクトの作成
            // 及び、リクエストの再構築
            var list = await LoadDownloadRequestItems();

            using (var releaser = await _CacheRequestProcessingLock.LockAsync())
            {
                foreach (var req in list)
                {
                    _CacheDownloadPendingVideos.Add(req);
                }
            }

            Debug.WriteLine("");
            Debug.WriteLine($"{list.Count} 件のダウンロードリクエストを復元");
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

                    var nicoVideo = new NicoVideo(_info.RawVideoId, _HohoemaApp.ContentProvider, _HohoemaApp.NiconicoContext, _HohoemaApp.CacheManager);

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



        // 次のキャッシュダウンロードを試行
        // ダウンロード用の本数
        private async Task TryNextCacheRequestedVideoDownload()
        {
            if (!IsInitialized) { return; }

            if (!_HohoemaApp.IsLoggedIn) { return; }


            NicoVideoCacheRequest nextDownloadItem = null;
            
            using (var releaser = await _CacheRequestProcessingLock.LockAsync())
            {
                if (_DownloadOperations.Count == 0)
                {
                    nextDownloadItem = _CacheDownloadPendingVideos
                        .FirstOrDefault();
                }
            }

            if (nextDownloadItem != null)
            {
                var op = await DonwloadVideoInBackgroundTask(nextDownloadItem);

                // DL作業を作成できたらDL待ちリストから削除
                using (var releaser = await _CacheRequestProcessingLock.LockAsync())
                {
                    _CacheDownloadPendingVideos.Remove(nextDownloadItem);
                    await SaveDownloadRequestItems();
                }
            }
        }

        internal void StopCacheDownload()
        {
            // TODO: 
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

        // バックグラウンドで動画キャッシュのダウンロードを行うタスクを作成
        public async Task<NicoVideoCacheProgress> DonwloadVideoInBackgroundTask(NicoVideoCacheRequest req)
        {
            using (var bgTaskLock = await _CacheRequestProcessingLock.LockAsync())
            {
                if (_DownloadOperations.Any(x => x.RawVideoId == req.RawVideoId && x.Quality == req.Quality))
                {
                    return null;
                }
            }

            Debug.WriteLine($"キャッシュ準備を開始: {req.RawVideoId} {req.Quality}");

            // 動画ダウンロードURLを取得
            var nicoVideo = new NicoVideo(req.RawVideoId, _HohoemaApp.ContentProvider, _HohoemaApp.NiconicoContext, _HohoemaApp.CacheManager);
            var videoInfo = await _HohoemaApp.ContentProvider.GetNicoVideoInfo(req.RawVideoId);

            // DownloadSessionを保持して、再生完了時にDisposeさせる必要がある
            var downloadSession = await nicoVideo.CreateVideoStreamingSession(req.Quality, forceDownload: true);

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
                req.RawVideoId,
                videoInfo.MovieType,
                downloadSession.Quality
                );

            var videoFolder = await _HohoemaApp.GetVideoCacheFolder();
            var videoFile = await videoFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);

            // ダウンロード操作を作成
            var operation = downloader.CreateDownload(uri, videoFile);

            var progress = new NicoVideoCacheProgress(req, operation, downloadSession);
            await AddDownloadOperation(progress);

            Debug.WriteLine($"キャッシュ準備完了: {req.RawVideoId} {req.Quality}");


            // ダウンロードを開始
            /*
            if (Helpers.ApiContractHelper.IsFallCreatorsUpdateAvailable)
            {
                operation.IsRandomAccessRequired = true;
            }
            */

            var action = operation.StartAsync();
            action.Progress = OnDownloadProgress;
            var task = action.AsTask().ContinueWith(OnDownloadCompleted).ConfigureAwait(false);


            VideoCacheStateChanged?.Invoke(this, new VideoCacheStateChangedEventArgs()
            {
                CacheState = NicoVideoCacheState.Downloading,
                Request = progress
            });

            Debug.WriteLine($"キャッシュ開始: {req.RawVideoId} {req.Quality}");

            SendUpdatableToastWithProgress(videoInfo.Title, req);

            return progress;
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
                        VideoCacheStateChanged?.Invoke(this, new VideoCacheStateChangedEventArgs()
                        {
                            CacheState = NicoVideoCacheState.NotCacheRequested,
                            Request = removeCached
                        });


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
                    }

                    RequestCanceled?.Invoke(this, target);
                    VideoCacheStateChanged?.Invoke(this, new VideoCacheStateChangedEventArgs()
                    {
                        CacheState = NicoVideoCacheState.NotCacheRequested,
                        Request = target
                    });
                }
            }

            return deletedCount;
        }


        public async Task<IEnumerable<NicoVideoCacheInfo>> EnumerateCacheVideosAsync()
        {
            using (var releaser = await _CacheRequestProcessingLock.LockAsync())
            {
                return _CacheVideos.SelectMany(x => x.Value).ToArray();
            }
        }

        public async Task<IEnumerable<NicoVideoCacheRequest>> EnumerateCacheRequestedVideosAsync()
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

        public async Task<IEnumerable<NicoVideoCacheRequest>> GetCacheRequest(string videoId)
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

        public async Task<IEnumerable<NicoVideoCacheProgress>> GetDownloadProgressVideosAsync()
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
                    _CacheDownloadPendingVideos.Add(req);

                    Requested?.Invoke(this, req);
                    VideoCacheStateChanged?.Invoke(this, new VideoCacheStateChangedEventArgs()
                    {
                        CacheState = NicoVideoCacheState.Pending,
                        Request = req
                    });
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
		public Task RequestCache(string rawVideoId, NicoVideoQuality quality, bool forceUpdate = false)
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
                VideoCacheStateChanged?.Invoke(this, new VideoCacheStateChangedEventArgs()
                {
                    CacheState = NicoVideoCacheState.NotCacheRequested,
                    Request = item
                });

                return true;
            }
            else
            {
                return false;
            }
        }





        /*
         *https://blogs.msdn.microsoft.com/tiles_and_toasts/2017/02/01/progress-ui-and-data-binding-inside-toast-notifications-windows-10-creators-update/
         */
        ToastNotification _ProgressToast;
        private void SendUpdatableToastWithProgress(string title, NicoVideoCacheRequest req)
        {
            if (!Helpers.ApiContractHelper.IsCreatorsUpdateAvailable)
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
            if (!Helpers.ApiContractHelper.IsCreatorsUpdateAvailable)
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
            if (!Helpers.ApiContractHelper.IsCreatorsUpdateAvailable)
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


            if (prevTask.IsFaulted)
            {
                try
                {
                    Debug.WriteLine("キャッシュ失敗");

                    var op = prevTask.Result;
                    var info = VideoCacheManager.CacheRequestInfoFromFileName(op.ResultFile);
                    var progress = await GetCacheProgress(info.RawVideoId, info.Quality);
                    await RemoveDownloadOperation(progress);

                    VideoCacheStateChanged?.Invoke(this, new VideoCacheStateChangedEventArgs()
                    {
                        Request = progress,
                        CacheState = NicoVideoCacheState.Pending
                    });

                    return;
                }
                catch
                {

                }
                finally
                {
                }
            }

            Debug.WriteLine("キャッシュ完了");


            if (prevTask.Result != null)
            {
                var op = prevTask.Result;
                var req = VideoCacheManager.CacheRequestInfoFromFileName(op.ResultFile);
                var progress = await GetCacheProgress(req.RawVideoId, req.Quality);
                await RemoveDownloadOperation(progress);

                if (op.Progress.Status == BackgroundTransferStatus.Completed)
                {
                    if (op.Progress.TotalBytesToReceive == op.Progress.BytesReceived)
                    {
                        Debug.WriteLine("キャッシュ済み: " + op.ResultFile.Name);
                        var cacheInfo = new NicoVideoCacheInfo(req, op.ResultFile.Path);

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

                        VideoCacheStateChanged?.Invoke(this, new VideoCacheStateChangedEventArgs()
                        {
                            Request = progress,
                            CacheState = NicoVideoCacheState.Cached
                        });

                        
                    }
                    else
                    {
                        Debug.WriteLine("キャッシュキャンセル: " + op.ResultFile.Name);
                        VideoCacheStateChanged?.Invoke(this, new VideoCacheStateChangedEventArgs()
                        {
                            Request = progress,
                            CacheState = NicoVideoCacheState.NotCacheRequested
                        });
                    }
                }
                else
                {
                    Debug.WriteLine($"キャッシュ失敗: {op.ResultFile.Name} （再ダウンロードします）");
                    VideoCacheStateChanged?.Invoke(this, new VideoCacheStateChangedEventArgs()
                    {
                        Request = progress,
                        CacheState = NicoVideoCacheState.Downloading
                    });
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

            VideoCacheStateChanged?.Invoke(this, new VideoCacheStateChangedEventArgs()
            {
                Request = progress,
                CacheState = NicoVideoCacheState.NotCacheRequested
            });

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

        private async Task AddDownloadOperation(NicoVideoCacheProgress req)
        {
            using (var releaser = await _CacheRequestProcessingLock.LockAsync())
            {
                _DownloadOperations.Add(req);
            }
        }

        private async Task RemoveDownloadOperation(NicoVideoCacheProgress req)
        {
            using (var releaser = await _CacheRequestProcessingLock.LockAsync())
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
        }




        internal async Task VideoDeletedFromNiconicoServer(string videoId)
        {
            // キャッシュ登録を削除
            int deletedCount = 0;
            try
            {
                deletedCount = await DeleteCachedVideo(videoId);
            }
            catch
            {
                // 削除に失敗
            }

            if (deletedCount > 0)
            {
                var videoInfo = Database.NicoVideoDb.Get(videoId);
                var toastService = App.Current.Container.Resolve<Views.Service.ToastNotificationService>();
                toastService.ShowText("動画削除：" + videoId
                    , $"『{videoInfo?.Title ?? videoId}』 はニコニコ動画サーバーから削除されたため、キャッシュを強制削除しました。"
                    , Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Long
                    );

            }
        }





    }




    public class NicoVideoCacheProgress : NicoVideoCacheRequest
    {
        public DownloadOperation DownloadOperation { get; set; }
        public IVideoStreamingSession Session { get;  }

        public NicoVideoCacheProgress()
        {

        }

        public NicoVideoCacheProgress(NicoVideoCacheRequest req, DownloadOperation op, IVideoStreamingSession session)
        {
            RawVideoId = req.RawVideoId;
            Quality = session.Quality;
            IsRequireForceUpdate = req.IsRequireForceUpdate;
            RequestAt = req.RequestAt;
            DownloadOperation = op;
            Session = session;
        }
    }







}
