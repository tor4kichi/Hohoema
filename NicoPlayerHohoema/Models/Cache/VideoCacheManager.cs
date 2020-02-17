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
using Unity;
using System.Collections.Concurrent;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using Microsoft.Toolkit.Uwp.Helpers;
using System.Reactive.Concurrency;
using Prism.Commands;
using NicoPlayerHohoema.Models.Niconico;
using System.Collections.Immutable;
using NicoPlayerHohoema.Models.Niconico.Video;
using NicoPlayerHohoema.Repository.VideoCache;
using I18NPortable;
using System.Reactive.Subjects;
using System.Reactive;

namespace NicoPlayerHohoema.Models.Cache
{
    public struct CacheSaveFolderChangedEventArgs
    {
        public StorageFolder OldFolder { get; set; }
        public StorageFolder NewFolder { get; set; }
    }

    public struct CacheRequestRejectedEventArgs
    {
        public string Reason { get; set; }
        public CacheRequest Request { get; set; }
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


        public event EventHandler<CacheSaveFolderChangedEventArgs> SaveFolderChanged;

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
            var oldSaveFolder = _DownloadFolder;
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

            SaveFolderChanged?.Invoke(this, new CacheSaveFolderChangedEventArgs()
            {
                OldFolder = oldSaveFolder,
                NewFolder = _DownloadFolder
            });

            return true;
        }

    }

    public struct VideoCacheStateChangedEventArgs
    {
        public CacheRequest Request { get; set; }
        public NicoVideoCacheState PreviousCacheState { get; set; }
    }

    public class CachedVideoSessionProvider : INiconicoVideoSessionProvider
    {
        public string ContentId { get; }

        public ImmutableArray<NicoVideoQualityEntity> AvailableQualities { get; }

        private readonly VideoCacheManager _videoCacheManager;
        private readonly Dictionary<NicoVideoQuality, NicoVideoCached> _cachedQualities;



        public CachedVideoSessionProvider(string contentId, VideoCacheManager videoCacheManager, IEnumerable<NicoVideoCached> qualities)
        {
            ContentId = contentId;
            _videoCacheManager = videoCacheManager;
            _cachedQualities = qualities.ToDictionary(x => x.Quality);
            AvailableQualities = qualities.Select(x => new NicoVideoQualityEntity(true, x.Quality, x.Quality.ToString())).ToImmutableArray();
        }

        public bool CanPlayQuality(string qualityId)
        {
            if (!Enum.TryParse<NicoVideoQuality>(qualityId, out var quality))
            {
                return false;
            }

            return _cachedQualities.ContainsKey(quality);
        }

        public Task<IStreamingSession> CreateVideoSessionAsync(NicoVideoQuality quality)
        {
            if (_cachedQualities.ContainsKey(quality))
            {
                return _videoCacheManager.CreateStreamingSessionAsync(_cachedQualities[quality]);
            }
            else if (_cachedQualities.Any())
            {
                return _videoCacheManager.CreateStreamingSessionAsync(_cachedQualities.Last().Value);
            }
            else
            {
                throw new Exception();
            }
        }
    }

    public class CreateCachedVideoSessionProviderResult
    {
        public CreateCachedVideoSessionProviderResult(CachedVideoSessionProvider provider, IEnumerable<NicoVideoCached> qualities)
        {
            VideoSessionProvider = provider;
            AvairableCacheQualities = qualities.ToImmutableArray();
            IsSuccess = AvairableCacheQualities.Any();
        }

        public bool IsSuccess { get; }

        public CachedVideoSessionProvider VideoSessionProvider { get; }

        public ImmutableArray<NicoVideoCached> AvairableCacheQualities { get; }
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
            CacheSettings cacheSettings,
            NicoVideoSessionProvider nicoVideoSessionProvider,
            NicoVideoSessionOwnershipManager sessionOwnershipManager,
            CacheRequestRepository cacheRequestRepository
            )
        {
            Scheduler = scheduler;
            NiconicoSession = niconicoSession;
            NicoVideoProvider = nicoVideoProvider;
            CacheSaveFolder = cacheSaveFolder;
            CacheSettings = cacheSettings;
            _nicoVideoSessionProvider = nicoVideoSessionProvider;
            _sessionOwnershipManager = sessionOwnershipManager;
            _cacheRequestRepository = cacheRequestRepository;
            NiconicoSession.LogIn += async (sender, e) =>
            {
                await TryNextCacheRequestedVideoDownload();
            };

            _CachePendingItemsChangedSubject = new BehaviorSubject<Unit>(Unit.Default);
            Observable.Merge(
                _DownloadOperations.ObserveRemoveChanged().ToUnit(),
                _CachePendingItemsChangedSubject.ToUnit(),
                NiconicoSession.ObserveProperty(x => x.IsLoggedIn).ToUnit()
                )
                .Subscribe(async _ => 
                {
                    await TryNextCacheRequestedVideoDownload();
                });

            CacheSaveFolder.SaveFolderChanged += CacheSaveFolder_SaveFolderChanged;

            _sessionOwnershipManager.OwnershipRemoveRequested += _sessionOwnershipManager_OwnershipRemoveRequested;
        }

        private async void _sessionOwnershipManager_OwnershipRemoveRequested(NicoVideoSessionOwnershipManager sender, SessionOwnershipRemoveRequestedEventArgs args)
        {
            using var releaser = await _CacheRequestProcessingLock.LockAsync();

            Debug.WriteLine($"{args.VideoId} is Remove session ownership.");
            var progress = _DownloadOperations.FirstOrDefault(x => x.VideoId == args.VideoId);
            if (progress != null)
            {
                if (!_cacheRequestRepository.TryGet(progress.VideoId, out CacheRequest req))
                {
                    RemoveDownloadOperation(progress);
                    await progress.CancelAndDeleteFileAsync();
                    Debug.WriteLine($"{args.VideoId} cache progress canceled with play video on other place.");
                }
                else
                {
                    RemoveDownloadOperation(progress);
                    await progress.CancelAndDeleteFileAsync();
                    Debug.WriteLine($"{args.VideoId} cache progress canceled with play video on other place.");

                    HandleCacheStateChanged(ref req, NicoVideoCacheState.Pending);
                }
            }
        }

        ISubject<Unit> _CachePendingItemsChangedSubject;

        private void CacheSaveFolder_SaveFolderChanged(object sender, CacheSaveFolderChangedEventArgs e)
        {
            _ = CacheFolderChanged();
        }

        public IScheduler Scheduler { get; }

        public NiconicoSession NiconicoSession { get; }
        public Provider.NicoVideoProvider NicoVideoProvider { get; }
        public CacheSaveFolder CacheSaveFolder { get; }
        public CacheSettings CacheSettings { get; }
        private readonly NicoVideoSessionProvider _nicoVideoSessionProvider;
        private readonly NicoVideoSessionOwnershipManager _sessionOwnershipManager;
        private readonly CacheRequestRepository _cacheRequestRepository;
        static readonly Regex NicoVideoIdRegex = new Regex("\\[((?:sm|so|lv)\\d+)\\]");

        static readonly Regex ExternalCachedNicoVideoIdRegex = new Regex("(?>sm|so|lv)\\d*");
        private const string TransferGroupName = @"hohoema_video";
        BackgroundTransferGroup _NicoCacheVideoBGTransferGroup = BackgroundTransferGroup.CreateGroup(TransferGroupName);

        private CacheManagerState State { get; set; } = CacheManagerState.NotInitialize;

        public event EventHandler<CacheRequest> Requested;
        public event EventHandler<CacheRequest> RequestCanceled;
        public event EventHandler<CacheRequestRejectedEventArgs> Rejected;

        public event EventHandler<VideoCacheStateChangedEventArgs> VideoCacheStateChanged;

        Helpers.AsyncLock _CacheRequestProcessingLock = new Helpers.AsyncLock();
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
                _ = CancelCacheRequest(video.Id);
            }
            , video => video != null && this.CheckCachedAsyncUnsafe(video.Id)
            ));


        #endregion



        #region Helper Methods

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
                    case "dmc_superhigh":
                        return NicoVideoQuality.Dmc_SuperHigh;
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

        static public string MakeCacheVideoFileName(string title, string videoId, Database.MovieType videoType, NicoVideoQuality quality)
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
                case NicoVideoQuality.Dmc_SuperHigh:
                    toQualityNameExtention = Path.ChangeExtension(filename, $".dmc_superhigh.{videoType.ToString().ToLower()}");
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

        static public (string VideoId, NicoVideoQuality Quality) CacheRequestInfoFromFileName(IStorageFile file)
        {
            // キャッシュリクエストを削除
            // 2重に拡張子を利用しているので二回GetFileNameWithoutExtensionを掛けることでIDを取得
            var match = NicoVideoIdRegex.Match(file.Name);
            if (match != null)
            {
                var id = match.Groups[1].Value;
                var quality = GetQualityFromFileName(file.Name);

                return (VideoId: id, Quality: quality);
            }
            else
            {
                throw new Exception();
            }
        }

        #endregion 


        /// <summary>
        /// ダウンロードを停止します。
        /// 現在ダウンロード中のアイテムはキャンセルしてPendingに積み直します
        /// </summary>
        /// <returns></returns>
        public async Task SuspendCacheDownload()
        {
            using var releaser = await _CacheRequestProcessingLock.LockAsync();

            if (State != CacheManagerState.Running) { return; }

            State = CacheManagerState.SuspendDownload;

            var operations = _DownloadOperations.ToList();
            _DownloadOperations.Clear();
            foreach (var progress in operations)
            {
                RemoveDownloadOperation(progress);
                await progress.CancelAndDeleteFileAsync();
            }
        }

        /// <summary>
        /// ダウンロードを再開します
        /// </summary>
        /// <returns></returns>
        public async Task ResumeCacheDownload()
        {
            using var releaser = await _CacheRequestProcessingLock.LockAsync();
            
            if (State != CacheManagerState.SuspendDownload) { return; }

            State = CacheManagerState.Running;
            await TryNextCacheRequestedVideoDownload();
        }




        /// <summary>
        /// ユーザーがキャッシュフォルダを変更した際に
        /// HohoemaAppから呼び出されます
        /// </summary>
        /// <returns></returns>
        internal async Task CacheFolderChanged()
        {
            var prevState = State;

            await SuspendCacheDownload();

            await RetrieveCacheCompletedVideos();

            if (prevState == CacheManagerState.Running)
            {
                await ResumeCacheDownload();
            }

            return;
        }

        #region Life Time Management

        public async void Dispose()
		{
            using var releaser = await _CacheRequestProcessingLock.LockAsync();

            foreach (var op in _DownloadOperations)
            {
                (op as IDisposable)?.Dispose();
            }
            _DownloadOperations.Clear();
        }

        protected override async Task OnInitializeAsync(CancellationToken token)
        {
            using var releaser = await _CacheRequestProcessingLock.LockAsync();
            Debug.Write($"キャッシュ情報のリストアを開始");

            // ダウンロード中のアイテムをリストア
            await RestoreBackgroundDownloadTask();

            // キャッシュ完了したアイテムをキャッシュフォルダから検索
            //await RetrieveCacheCompletedVideos();

            State = CacheManagerState.Running;
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
            var tasks = await BackgroundDownloader.GetCurrentDownloadsForTransferGroupAsync(_NicoCacheVideoBGTransferGroup);
            foreach (var operation in tasks)
            {
                try
                {
                    var _info = VideoCacheManager.CacheRequestInfoFromFileName(operation.ResultFile);
                    if (!_cacheRequestRepository.TryGet(_info.VideoId, out var req))
                    {
                        // リポジトリに記録されてないリクエストはキャンセル済みとして処理
                        using (CancellationTokenSource canceledToken = new CancellationTokenSource())
                        {
                            canceledToken.Cancel();

                            try
                            {
                                await operation.AttachAsync().AsTask(canceledToken.Token);
                            }
                            catch (TaskCanceledException)
                            {

                            }

                            await operation.ResultFile.DeleteAsync();
                        }

                        continue;
                    }

                    var prepareResult = await _nicoVideoSessionProvider.PreparePlayVideoAsync(_info.VideoId, isForCacheDownload: true);
                    var session = (Models.IVideoStreamingDownloadSession)await prepareResult.CreateVideoSessionAsync(_info.Quality);
                    var progress = new NicoVideoCacheProgress(operation, session, _info.VideoId, _info.Quality, operation.ResultFile.DateCreated.DateTime);

                    // Note: AttachAsync呼び出し時にFailedイベントがトリガーする可能性あり
                    // FailedでRemoveDownloadOperation(progress) が呼ばれるため
                    // 先に_DownloadOperationsの準備が完了している必要がある
                    AddDownloadOperation(progress);                    
                    progress.AttachAsync();

                    Debug.WriteLine($"実行中のキャッシュBGDLを補足: {progress.VideoId} {progress.Quality}");
                }
                catch
                {
                    Debug.WriteLine(operation.ResultFile + "のキャッシュダウンロード操作を復元に失敗しました");
                    continue;
                }
            }
        }


        #endregion

        private async Task RetrieveCacheCompletedVideos()
        {
            var videoFolder = await CacheSaveFolder.GetVideoCacheFolder();
            if (videoFolder == null)
            {
                return;
            }

            List<CacheRequest> retrievedCacheInfoList = new List<CacheRequest>();
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

                retrievedCacheInfoList.Add(new CacheRequest(id, file.DateCreated.DateTime, NicoVideoCacheState.Cached));

                Debug.Write(".");
            }


            _cacheRequestRepository.ClearAllItems();
            foreach (var req in retrievedCacheInfoList)
            {
                var r = req;
                HandleCacheStateChanged(ref r, NicoVideoCacheState.NotCacheRequested);
            }
        }






        public async Task<CacheRequest?> GetCacheRequestAsync(string videoId)
        {
            using var releaser = await _CacheRequestProcessingLock.LockAsync();
            return GetCacheRequestAsyncUnsafe(videoId);
        }

        public CacheRequest? GetCacheRequestAsyncUnsafe(string videoId)
        {
            return _cacheRequestRepository.TryGet(videoId, out var req) ? req : default(CacheRequest?);
        }

        public int GetCacheRequestCount()
        {
            return _cacheRequestRepository.Count();
        }

        public List<CacheRequest> GetCacheRequests(int start, int length)
        {
            if (start <= -1) { throw new ArgumentOutOfRangeException(nameof(start)); }

            if (length <= -1) { throw new ArgumentOutOfRangeException(nameof(length)); }

            return _cacheRequestRepository.GetRange(start, length);
        }
        public CacheRequest? GetCacheRequest(string videoId)
        {
            return _cacheRequestRepository.TryGet(videoId, out var req) ? req : default(CacheRequest?);
        }

        public async Task<List<NicoVideoCacheProgress>> GetDownloadProgressVideosAsync()
        {
            using var releaser = await _CacheRequestProcessingLock.LockAsync();
            return _DownloadOperations.ToList();
        }


        public async Task<NicoVideoCacheProgress> GetCacheProgress(string videoId)
        {
            using var releaser = await _CacheRequestProcessingLock.LockAsync();
            return _DownloadOperations.FirstOrDefault(x => x.VideoId == videoId);
        }








        readonly string[] _VideoFileTypes = new[] { ".mp4", ".flv" };
        public async Task<List<NicoVideoCached>> GetCachedAsync(string videoId)
        {
            using var realeser = await _CacheRequestProcessingLock.LockAsync();

            var folder = await CacheSaveFolder.GetVideoCacheFolder();
            if (folder == null) { return new List<NicoVideoCached>(); }

            if (!_cacheRequestRepository.TryGet(videoId, out var request))
            {
                return new List<NicoVideoCached>();
            }

            if (request.CacheState != NicoVideoCacheState.Cached) { return new List<NicoVideoCached>(); }

            var query = folder.CreateFileQueryWithOptions(new Windows.Storage.Search.QueryOptions(Windows.Storage.Search.CommonFileQuery.DefaultQuery, _VideoFileTypes)
            {
                UserSearchFilter = $"{videoId}"
            });

            var cached = new List<NicoVideoCached>();
            var files = await query.GetFilesAsync();
            foreach (var file in files)
            {
                var quality = GetQualityFromFileName(file.Name);

                cached.Add(new NicoVideoCached(videoId, quality, request.RequestAt, file));
            }

            return cached;
        }


        /// <summary>
		/// キャッシュリクエストをキューの最後尾に積みます
		/// 通常のダウンロードリクエストではこちらを利用します
		/// </summary>
		/// <param name="req"></param>
		/// <returns></returns>
		public Task RequestCache(string videoId, NicoVideoQuality quality = NicoVideoQuality.Unknown)
        {
            return RequestCache_Internal(videoId, quality);
        }


        private async Task RequestCache_Internal(string videoId, NicoVideoQuality requestQuality)
        {
            using var releaser = await _CacheRequestProcessingLock.LockAsync();
            
            var alreadyDownload = _DownloadOperations.FirstOrDefault(x => x.VideoId == videoId);
            if (alreadyDownload != null)
            {
                // 基本的に新しいリクエストを優先する
                // 新しいリクエストがUnknown指定だった場合は、何でもいいからダウンロードしてくれてればOK
                // ただし、状態を中断して新しくDLし直して欲しいニーズがあるので、
                // DLセッションが問題ないかのチェックはしておきたい

                bool continueProgressDownload = false;
                var progress = alreadyDownload;
                if (requestQuality != NicoVideoQuality.Unknown)
                {
                    if (progress.Quality != requestQuality)
                    {
                        continueProgressDownload = true;
                    }
                }

                if (continueProgressDownload)
                {
                    return;
                }
            }

            // 既にリクエスト済み
            if (_cacheRequestRepository.TryGet(videoId, out var req))
            {
                if (req.CacheState == NicoVideoCacheState.Cached)
                {
                    var cacheInfos = await GetCachedAsync(videoId);
                    if (requestQuality != NicoVideoQuality.Unknown)
                    {
                        if (cacheInfos.Any(x => x.Quality == requestQuality))
                        {
                            // 指定画質をキャッシュ済み
                            return;
                        }
                    }
                    else
                    {
                        // 画質指定がなくキャッシュ済み
                        if (cacheInfos.Any())
                        {
                            return;
                        }
                    }
                }
                else
                {
                    req = new CacheRequest(videoId, NicoVideoCacheState.Pending, requestQuality);
                }
            }
            else
            {
                req = new CacheRequest(videoId, NicoVideoCacheState.Pending, requestQuality);
            }

            _cacheRequestRepository.UpdateItem(req);

            try
            {
                Requested?.Invoke(this, req);
                TriggerCacheStateChangedEventOnUIThread(req, req.CacheState);
            }
            catch (Exception e)
            {
                await (App.Current as App).OutputErrorFile(e);
            }

            _ = TryNextCacheRequestedVideoDownload();
        }


        public async Task<bool> CancelCacheRequest(string videoId)
        {
            using var releaser = await _CacheRequestProcessingLock.LockAsync();

            {
                var progress = _DownloadOperations.FirstOrDefault(x => x.VideoId == videoId);
                if (progress != null)
                {
                    RemoveDownloadOperation(progress);
                    await progress.CancelAndDeleteFileAsync();
                }
            }

            foreach (var cached in await GetCachedAsync(videoId))
            {
                await cached.File.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }

            if (_cacheRequestRepository.TryRemove(videoId, out var request))
            {
                var canceled = new CacheRequest(request, NicoVideoCacheState.NotCacheRequested);

                try
                {
                    RequestCanceled?.Invoke(this, request);
                    TriggerCacheStateChangedEventOnUIThread(canceled, request.CacheState);
                }
                catch (Exception e)
                {
                    await (App.Current as App).OutputErrorFile(e);
                }

                _CachePendingItemsChangedSubject.OnNext(Unit.Default);
                return true;
            }
            else
            {
                return false;
            }
        }



        // 次のキャッシュダウンロードを試行
        // ダウンロード用の本数
        private async Task TryNextCacheRequestedVideoDownload()
        {
            if (State != CacheManagerState.Running) { return; }

            using var releaser = await _CacheRequestProcessingLock.LockAsync();

            if (!_cacheRequestRepository.TryGetPendingFirstItem(out var nextRequest))
            {
                return;
            }

            // 既にダウンロード中なら
            if (_DownloadOperations.Any(x => x.VideoId == nextRequest.VideoId && x.Quality == nextRequest.PriorityQuality))
            {
                HandleCacheStateChanged(ref nextRequest, NicoVideoCacheState.Downloading);
                _CachePendingItemsChangedSubject.OnNext(Unit.Default);
                return;
            }

            Debug.WriteLine($"キャッシュ準備を開始: {nextRequest.VideoId} {nextRequest.PriorityQuality}");

            if (!Helpers.InternetConnection.IsInternet())
            {
                throw new Exception("internet not avairable");
            }

            // 動画ダウンロードURLを取得                    
            var videoInfo = await NicoVideoProvider.GetNicoVideoInfo(nextRequest.VideoId);

            if (videoInfo.RawVideoId.StartsWith("so"))
            {
                Debug.WriteLine($"キャッシュ チャンネル動画は不可 : {nextRequest.VideoId} {nextRequest.PriorityQuality}");

                Scheduler.Schedule(() =>
                {
                    HandleCacheStateChanged(ref nextRequest, NicoVideoCacheState.Failed);
                    Rejected?.Invoke(this, new CacheRequestRejectedEventArgs()
                    {
                        Request = nextRequest,
                        Reason = "Protected Content",
                    });
                });

                return;
            }

            

            var prepareResult = await _nicoVideoSessionProvider.PreparePlayVideoAsync(videoInfo.RawVideoId, isForCacheDownload: true);

            // DownloadSessionを保持して、再生完了時にDisposeさせる必要がある
            var downloadSession = await prepareResult.CreateVideoSessionAsync(nextRequest.PriorityQuality);
            if (downloadSession == null)
            {
                // TODO: 再生中のプレイヤーを閉じることでキャッシュを再開できることを確認する

                return;
            }

            var videoStreamingSession = downloadSession as Models.IVideoStreamingDownloadSession;

            try
            {
                // TODO: 優先画質がキャッシュ開始できなかった場合の処理
                if (nextRequest.PriorityQuality != NicoVideoQuality.Unknown)
                {
                    if (nextRequest.PriorityQuality != videoStreamingSession.Quality)
                    {
                        using (downloadSession)
                        {
                            // 画質がユーザーリクエストと異なる場合はDLを開始しない
                            HandleCacheStateChanged(ref nextRequest, NicoVideoCacheState.FailedWithQualityNotAvairable);
                        }
                        return;
                    }
                }

                var uri = await videoStreamingSession.GetDownloadUrlAndSetupDonwloadSession();

                var downloader = new BackgroundDownloader()
                {
                    TransferGroup = _NicoCacheVideoBGTransferGroup
                };

                downloader.SuccessToastNotification = MakeSuccessToastNotification(videoInfo);
                downloader.FailureToastNotification = MakeFailureToastNotification(videoInfo);

                // 保存先ファイルの確保
                var filename = VideoCacheManager.MakeCacheVideoFileName(
                    videoInfo.Title,
                    nextRequest.VideoId,
                    videoInfo.MovieType,
                    videoStreamingSession.Quality
                    );

                var videoFolder = await CacheSaveFolder.GetVideoCacheFolder();
                var videoFile = await videoFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);

                // ダウンロード操作を作成
                var operation = downloader.CreateDownload(uri, videoFile);

                var progress = new NicoVideoCacheProgress(operation, videoStreamingSession, nextRequest.VideoId, videoStreamingSession.Quality, nextRequest.RequestAt);
                AddDownloadOperation(progress);

                Debug.WriteLine($"キャッシュ準備完了: {progress.VideoId} {progress.Quality}");

                HandleCacheStateChanged(ref nextRequest, NicoVideoCacheState.Downloading);

                // ダウンロードを開始
                /*
                if (Helpers.ApiContractHelper.IsFallCreatorsUpdateAvailable)
                {
                    operation.IsRandomAccessRequired = true;
                }
                */

                progress.StartAsync();

                Debug.WriteLine($"キャッシュ開始: {progress.VideoId} {progress.Quality}");
            }
            catch
            {
                videoStreamingSession?.Dispose();
            }

        }

        private void HandleCacheStateChanged(ref CacheRequest cacheRequest, NicoVideoCacheState newState)
        {
            if (cacheRequest.CacheState == newState) { return; }

            var prevState = cacheRequest.CacheState;
            cacheRequest.CacheState = newState;
            _cacheRequestRepository.UpdateItem(cacheRequest);
            TriggerCacheStateChangedEventOnUIThread(cacheRequest, prevState);
        }
        

        private void AddDownloadOperation(NicoVideoCacheProgress progress)
        {
            progress.Completed += OnCacheCompleted;
            progress.Failed += OnCacheFailed;
            progress.Canceled += OnCacheCanceled;

            _DownloadOperations.Add(progress);
        }

        private void RemoveDownloadOperation(NicoVideoCacheProgress progress)
        {
            progress.Completed -= OnCacheCompleted;
            progress.Failed -= OnCacheFailed;
            progress.Canceled -= OnCacheCanceled;

            if (_DownloadOperations.Remove(progress))
            {
                (progress as IDisposable).Dispose();
            }
            else
            {
                return;
            }
        }



        async void OnCacheCompleted(NicoVideoCacheProgress progress, EventArgs args)
        {
            using var releaser = await _CacheRequestProcessingLock.LockAsync();

            if (!_cacheRequestRepository.TryGet(progress.VideoId, out var request))
            {
                Debug.WriteLine("キャッシュキャンセル: " + progress.VideoId);
                return;
            }

            var cachedRequest = new CacheRequest(request, NicoVideoCacheState.Cached);
            _cacheRequestRepository.UpdateItem(cachedRequest);
            TriggerCacheStateChangedEventOnUIThread(cachedRequest, request.CacheState);

            RemoveDownloadOperation(progress);
        }


        async void OnCacheFailed(NicoVideoCacheProgress progress, EventArgs args)
        {
            using var releaser = await _CacheRequestProcessingLock.LockAsync();

            if (!_cacheRequestRepository.TryGet(progress.VideoId, out var request))
            {
                Debug.WriteLine("キャッシュキャンセル: " + progress.VideoId);
                return;
            }

            try
            {
                var failedRequest = new CacheRequest(request, NicoVideoCacheState.Failed);
                _cacheRequestRepository.UpdateItem(failedRequest);
                TriggerCacheStateChangedEventOnUIThread(new CacheRequest(request, NicoVideoCacheState.Failed), request.CacheState);
            }
            finally
            {
                RemoveDownloadOperation(progress);
            }
        }

        async void OnCacheCanceled(NicoVideoCacheProgress progress, EventArgs args)
        {
            using var releaser = await _CacheRequestProcessingLock.LockAsync();

            // TODO: ユーザーキャンセルによる通知と、ここでの通知で二重で変更通知されないか？
            TriggerCacheStateChangedEventOnUIThread(new CacheRequest(progress.VideoId, progress.RequestAt, NicoVideoCacheState.NotCacheRequested), NicoVideoCacheState.Downloading);

            RemoveDownloadOperation(progress);
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


        public async Task<int> DeleteFromNiconicoServer(string videoId)
        {
            using var releaser = await _CacheRequestProcessingLock.LockAsync();

            if (!_cacheRequestRepository.TryGet(videoId, out var request))
            {
                throw new Exception();
            }

            var cachedItems = await GetCachedAsync(videoId);
            int deletedCount = 0;
            foreach (var cached in cachedItems)
            {
                await cached.File.DeleteAsync(StorageDeleteOption.PermanentDelete);
                deletedCount++;
            }

            HandleCacheStateChanged(ref request, NicoVideoCacheState.DeletedFromNiconicoServer);

            return deletedCount;
        }



        #region Toast Notification

        // TODO: キャッシュ完了等のトースト通知でNotificationServiceを利用する

        
        

        #endregion


        
        internal async Task<bool> CheckCachedAsync(string videoId)
        {
            using (var releaser = await _CacheRequestProcessingLock.LockAsync())
            {
                if (!CheckCachedAsyncUnsafe(videoId))
                {
                    return false;
                }

                var folder = await CacheSaveFolder.GetVideoCacheFolder();
                if (folder == null) { return false; }

                var query = folder.CreateFileQueryWithOptions(new Windows.Storage.Search.QueryOptions(Windows.Storage.Search.CommonFileQuery.OrderByDate, _VideoFileTypes)
                {
                    UserSearchFilter = $"{videoId}"
                });
                var files = await query.GetFilesAsync(0, 1);
                return files.Any();
            }
        }

        internal bool CheckCachedAsyncUnsafe(string videoId)
        {
            if (_cacheRequestRepository.TryGet(videoId, out var cacheRequest))
            {
                return cacheRequest.CacheState == NicoVideoCacheState.Cached;
            }
            else
            {
                return false;
            }
        }

        private void TriggerCacheStateChangedEventOnUIThread(CacheRequest req, NicoVideoCacheState prevCacheState)
        {
            Scheduler.Schedule(() => 
            {
                VideoCacheStateChanged?.Invoke(this, new VideoCacheStateChangedEventArgs()
                {
                    Request = req,
                    PreviousCacheState = prevCacheState
                });
            });
        }




        public async Task<CreateCachedVideoSessionProviderResult> TryCreateCachedVideoSessionProvider(string rawVideoId)
        {
            // キャッシュ済みアイテムを問い合わせ
            var cacheRequests = await GetCachedAsync(rawVideoId);

            var playableCacheQualities = cacheRequests
                .Where(x => x is NicoVideoCached)
                .OrderBy(x => x.Quality)
                .Cast<NicoVideoCached>()
                .ToArray();

            return new CreateCachedVideoSessionProviderResult(
                new CachedVideoSessionProvider(rawVideoId, this, playableCacheQualities), playableCacheQualities);
        }

        internal async Task<IStreamingSession> CreateStreamingSessionAsync(NicoVideoCached request)
        {
            if (request is NicoVideoCached cacheInfo)
            {
                try
                {
                    return new LocalVideoStreamingSession(cacheInfo.File, cacheInfo.Quality, NiconicoSession);
                }
                catch
                {
                    Debug.WriteLine("動画視聴時にキャッシュが見つかったが、キャッシュファイルを利用した再生セッションの作成に失敗。");
                }
            }

            return null;
        }

        internal async Task<IStreamingSession> CreateStreamingSessionAsync(NicoVideoCacheProgress request)
        {
            if (request is NicoVideoCacheProgress progress)
            {
                /*
                if (Helpers.ApiContractHelper.IsFallCreatorsUpdateAvailable)
                {
                    var playCandidateCacheProgress = playCandidateRequest as NicoVideoCacheProgress;
                    var op = playCandidateCacheProgress.DownloadOperation;
                    var refStream = op.GetResultRandomAccessStreamReference();
                    return new DownloadProgressVideoStreamingSession(refStream, playCandidateCacheProgress.Quality);
                }
                */
            }

            return null;
        }
    }


    public enum CacheManagerState
    {
        NotInitialize,
        Running,
        SuspendDownload,
    }








}
