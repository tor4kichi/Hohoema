using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using Mntone.Nico2.Searches.Video;
using Hohoema.Models.Domain;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Unity;
using System.Diagnostics;
using Hohoema.Models.Domain.Player.Video.Cache;
using Hohoema.Presentation.Services.Page;
using System.Reactive.Concurrency;
using Prism.Commands;
using Prism.Unity;
using I18NPortable;
using Prism.Events;
using System.Threading;
using Uno.Threading;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Niconico.UserFeature.Mylist;
using Hohoema.Models.UseCase.Playlist.Events;
using Hohoema.Models.Domain.Niconico.UserFeature;

namespace Hohoema.Presentation.ViewModels
{
    public class VideoInfoControlViewModel : BindableBase, IVideoContent, IDisposable
    {
        static VideoInfoControlViewModel()
        {
            _nicoVideoProvider = App.Current.Container.Resolve<NicoVideoProvider>();
            _ngSettings = App.Current.Container.Resolve<VideoFilteringSettings>();
            _cacheManager = App.Current.Container.Resolve<VideoCacheManager>();
            _scheduler = App.Current.Container.Resolve<IScheduler>();
            _nicoVideoRepository = App.Current.Container.Resolve<NicoVideoCacheRepository>();
            _videoPlayedHistoryRepository = App.Current.Container.Resolve<VideoPlayedHistoryRepository>();
        }

        public VideoInfoControlViewModel(NicoVideo data)            
        {
            RawVideoId = data?.RawVideoId ?? RawVideoId;
            Data = data;

            if (Data != null)
            {
                Label = Data.Title;
                PostedAt = Data.PostedAt;
                Length = Data.Length;
                ViewCount = Data.ViewCount;
                MylistCount = Data.MylistCount;
                CommentCount = Data.CommentCount;
                ThumbnailUrl = Data.ThumbnailUrl;
            }

            _ngSettings.VideoOwnerFilterAdded += _ngSettings_VideoOwnerFilterAdded;
            _ngSettings.VideoOwnerFilterRemoved += _ngSettings_VideoOwnerFilterRemoved;
        }

        private void _ngSettings_VideoOwnerFilterRemoved(object sender, VideoOwnerFilteringRemovedEventArgs e)
        {
            if (e.OwnerId == this.ProviderId)
            {
                UpdateIsHidenVideoOwner(Data);
            }
        }

        private void _ngSettings_VideoOwnerFilterAdded(object sender, VideoOwnerFilteringAddedEventArgs e)
        {
            if (e.OwnerId == this.ProviderId)
            {
                UpdateIsHidenVideoOwner(Data);
            }
        }

        void IDisposable.Dispose()
        {
            _ngSettings.VideoOwnerFilterAdded -= _ngSettings_VideoOwnerFilterAdded;
            _ngSettings.VideoOwnerFilterRemoved -= _ngSettings_VideoOwnerFilterRemoved;
            UnsubscriptionWatched();
        }



        private void NGVideoOwnerUserIds_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateIsHidenVideoOwner(Data);
        }


        void UpdateIsHidenVideoOwner(IVideoContent video)
        {
            if (video != null)
            {
                _ngSettings.TryGetHiddenReason(video, out var result);
                VideoHiddenInfo = result;
            }
            else
            {
                VideoHiddenInfo = null;
            }
        }

        private DelegateCommand _UnregistrationHiddenVideoOwnerCommand;
        public DelegateCommand UnregistrationHiddenVideoOwnerCommand =>
            _UnregistrationHiddenVideoOwnerCommand ?? (_UnregistrationHiddenVideoOwnerCommand = new DelegateCommand(ExecuteUnregistrationHiddenVideoOwnerCommand));

        void ExecuteUnregistrationHiddenVideoOwnerCommand()
        {
            if (Data != null)
            {
                _ngSettings.RemoveHiddenVideoOwnerId(Data.ProviderId);
            }

        }


        public VideoInfoControlViewModel(
            string rawVideoId
            )
            : this(data: null)
        {
            RawVideoId = rawVideoId;
        }

        public bool Equals(IVideoContent other)
        {
            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }


        private static readonly NicoVideoProvider _nicoVideoProvider;
        private static readonly NicoVideoCacheRepository _nicoVideoRepository;
        private static readonly VideoPlayedHistoryRepository _videoPlayedHistoryRepository;
        private static readonly VideoFilteringSettings _ngSettings;
        private static readonly VideoCacheManager _cacheManager;
        private static readonly IScheduler _scheduler;

        public string RawVideoId { get; }
        public NicoVideo Data { get; private set; }

        public string Id => RawVideoId;


        public string ProviderId { get; set; }
        public string ProviderName { get; set; }
        public NicoVideoUserType ProviderType { get; set; }

        public IMylist OnwerPlaylist { get; }

        public VideoStatus VideoStatus { get; private set; }


        private string _title;
        public string Label
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private TimeSpan _length;
        public TimeSpan Length
        {
            get { return _length; }
            set { SetProperty(ref _length, value); }
        }


        private string _Description;
        public string Description
        {
            get { return _Description; }
            set { SetProperty(ref _Description, value); }
        }

        private DateTime _postedAt;
        public DateTime PostedAt
        {
            get { return _postedAt; }
            set { SetProperty(ref _postedAt, value); }
        }


        private int _viewCount;
        public int ViewCount
        {
            get { return _viewCount; }
            set { SetProperty(ref _viewCount, value); }
        }


        private int _MylistCount;
        public int MylistCount
        {
            get { return _MylistCount; }
            set { SetProperty(ref _MylistCount, value); }
        }

        private int _CommentCount;
        public int CommentCount
        {
            get { return _CommentCount; }
            set { SetProperty(ref _CommentCount, value); }
        }

        private string _ThumbnailUrl;
        public string ThumbnailUrl
        {
            get { return _ThumbnailUrl; }
            set { SetProperty(ref _ThumbnailUrl, value); }
        }


        private bool _isDeleted;
        public bool IsDeleted
        {
            get { return _isDeleted; }
            set { SetProperty(ref _isDeleted, value); }
        }


        private FilteredResult _VideoHiddenInfo;
        public FilteredResult VideoHiddenInfo
        {
            get { return _VideoHiddenInfo; }
            set { SetProperty(ref _VideoHiddenInfo, value); }
        }

        private bool _isWatched;
        public bool IsWatched
        {
            get { return _isWatched; }
            set { SetProperty(ref _isWatched, value); }
        }


        SubscriptionToken _watchedDisposable;

        private bool _IsInitialized;
        public bool IsInitialized
        {
            get { return _IsInitialized; }
            set { SetProperty(ref _IsInitialized, value); }
        }

        private PrivateReasonType? _PrivateReason;
        public PrivateReasonType? PrivateReason
        {
            get { return _PrivateReason; }
            set { SetProperty(ref _PrivateReason, value); }
        }


        #region 

        public CacheRequest CacheRequest { get; private set; }
        public bool HasCacheProgress { get; private set; }
        public double DownloadProgress { get; private set; }
        public bool IsProgressUnknown { get; private set; }
        public NicoVideoQuality? CacheProgressQuality { get; private set; }
        public bool IsRequirePayment { get; internal set; }

        private void SubscribeCacheState(IVideoContent video)
        {
            UnsubscribeCacheState();

            //            System.Diagnostics.Debug.Assert(DataContext != null);

            if (video != null)
            {
                _cacheManager.VideoCacheStateChanged += _cacheManager_VideoCacheStateChanged;

                var cacheRequest = _cacheManager.GetCacheRequest(video.Id);
                ResetCacheRequests(video, cacheRequest);
            }
        }

        void ResetCacheRequests(IVideoContent video, CacheRequest cacheRequest)
        {
            ClearHandleProgress();

            _scheduler.Schedule(async () =>
            {
                // Note: 表示バグのワークアラウンドのため必要
                CacheRequest = null;

                if (cacheRequest?.CacheState == NicoVideoCacheState.Downloading)
                {
                    var progress = await _cacheManager.GetCacheProgress(video.Id);
                    if (progress != null)
                    {
                        HandleProgress(progress);
                    }

                    cacheRequest = new CacheRequest(cacheRequest, cacheRequest.CacheState)
                    {
                        PriorityQuality = progress.Quality
                    };
                }

                if (cacheRequest?.CacheState == NicoVideoCacheState.Cached
                && cacheRequest.PriorityQuality == NicoVideoQuality.Unknown)
                {
                    var cached = await _cacheManager.GetCachedAsync(video.Id);
                    if (cached?.Any() ?? false)
                    {
                        cacheRequest = new CacheRequest(cacheRequest, cacheRequest.CacheState)
                        {
                            PriorityQuality = cached.First().Quality
                        };
                    }
                }

                CacheRequest = cacheRequest;
            });
        }

        private void _cacheManager_VideoCacheStateChanged(object sender, VideoCacheStateChangedEventArgs e)
        {
            if (Data is IVideoContent video)
            {
                if (e.Request.VideoId == video.Id)
                {
                    _scheduler.Schedule(() =>
                    {
                        ResetCacheRequests(video, e.Request);
                    });
                }
            }
        }

        private void UnsubscribeCacheState()
        {
            _cacheManager.VideoCacheStateChanged -= _cacheManager_VideoCacheStateChanged;
            ClearHandleProgress();
        }

        NicoVideoCacheProgress _progress;
        
        IDisposable _progressObserver;
        double _totalSizeInverted;
        private void HandleProgress(NicoVideoCacheProgress progress)
        {
            HasCacheProgress = true;
            DownloadProgress = default; // nullの時はゲージ表示を曖昧に表現する
            CacheProgressQuality = progress.Quality;
            IsProgressUnknown = double.IsInfinity(progress.DownloadOperation.Progress.TotalBytesToReceive);
            _progress = progress;
            _progressObserver = _progress.ObserveProperty(x => x.Progress)
                .Subscribe(x =>
                {
                    _scheduler.Schedule(() =>
                    {
                        DownloadProgress = x;
                    });
                });
        }

        private void ClearHandleProgress()
        {
            if (_progress != null)
            {
                _progress = null;
            }
            _progressObserver?.Dispose();

            _totalSizeInverted = 0.0;
            DownloadProgress = default;
            HasCacheProgress = false;
            CacheProgressQuality = default;
        }


        #endregion


        void Watched(VideoPlayedEvent.VideoPlayedEventArgs args)
        {
            if (Data is IVideoContent video
                && video.Id == args.ContentId
                )
            {
                IsWatched = true;
                var eventAggregator = App.Current.Container.Resolve<IEventAggregator>();
                var palyedEvent = eventAggregator.GetEvent<VideoPlayedEvent>();
                palyedEvent.Unsubscribe(_watchedDisposable);
                _watchedDisposable = null;
            }
        }

        void SubscriptionWatchedIfNotWatch(IVideoContent video)
        {
            UnsubscriptionWatched();

            if (video != null)
            {
                var watched = _videoPlayedHistoryRepository.IsVideoPlayed(video.Id);
                IsWatched = watched;
                if (!watched)
                {
                    var eventAggregator = App.Current.Container.Resolve<IEventAggregator>();
                    var palyedEvent = eventAggregator.GetEvent<VideoPlayedEvent>();
                    _watchedDisposable = palyedEvent.Subscribe(Watched, ThreadOption.UIThread);
                }
            }
        }

        void UnsubscriptionWatched()
        {
            _watchedDisposable?.Dispose();
        }


        static FastAsyncLock _initializeLock = new FastAsyncLock();

        public async Task InitializeAsync(CancellationToken ct)
        {
            using var releaser = await _initializeLock.LockAsync(ct);

            if (Data?.Title != null)
            {
                SetTitle(Data.Title);
            }

            if (Data?.Title == null || Data?.ProviderId == null)
            {
                var data = await _nicoVideoProvider.GetNicoVideoInfo(RawVideoId);

                Data = data;
            }

            if (Data != null)
            {
                SetupFromThumbnail(Data);

                SubscriptionWatchedIfNotWatch(Data);
                UpdateIsHidenVideoOwner(Data);
                SubscribeCacheState(Data);
            }

            ct.ThrowIfCancellationRequested();

            IsInitialized = true;
        }


        public void SetupFromThumbnail(NicoVideo info)
        {
//            Debug.WriteLine("thumbnail reflect : " + info.RawVideoId);
            
            Label = info.Title;
            PostedAt = info.PostedAt;
            Length = info.Length;
            ViewCount = info.ViewCount;
            MylistCount = info.MylistCount;
            CommentCount = info.CommentCount;
            ThumbnailUrl ??= info.ThumbnailUrl;
            IsDeleted = info.IsDeleted;
            PrivateReason = Data.PrivateReasonType;

            if (info.Owner != null)
            {
                ProviderId = info.Owner.OwnerId;
                ProviderName = info.Owner.ScreenName;
                ProviderType = info.Owner.UserType;
            }

        }

        internal void SetDescription(int viewcount, int commentCount, int mylistCount)
        {
            ViewCount = viewcount;
            CommentCount = commentCount;
            MylistCount = mylistCount;
        }

        internal void SetTitle(string title)
        {
            Label = title;
        }
        internal void SetSubmitDate(DateTime submitDate)
        {
            PostedAt = submitDate;
        }

        internal void SetVideoDuration(TimeSpan duration)
        {
            Length = duration;
        }

        internal void SetThumbnailImage(string thumbnailImage)
        {
            ThumbnailUrl = thumbnailImage;
        }



		protected virtual VideoPlayPayload MakeVideoPlayPayload()
		{
			return new VideoPlayPayload()
			{
				VideoId = RawVideoId,
				Quality = null,
			};
		}



        
        public void SetupDisplay(Mntone.Nico2.Users.Video.VideoData data)
        {
            if (data.VideoId != RawVideoId) { throw new Exception(); }

            SetTitle(data.Title);
            SetThumbnailImage(data.ThumbnailUrl.OriginalString);
            SetSubmitDate(data.SubmitTime);
            SetVideoDuration(data.Length);

            IsInitialized = true;
        }


        // とりあえずマイリストから取得したデータによる初期化
        public void SetupDisplay(MylistData data)
        {
            if (data.WatchId != RawVideoId) { throw new Exception(); }

            SetTitle(data.Title);
            SetThumbnailImage(data.ThumbnailUrl.OriginalString);
            SetSubmitDate(data.CreateTime);
            SetVideoDuration(data.Length);
            SetDescription((int)data.ViewCount, (int)data.CommentCount, (int)data.MylistCount);

            IsInitialized = true;
        }


        // 個別マイリストから取得したデータによる初期化
        public void SetupDisplay(VideoInfo data)
        {
            if (data.Video.Id != RawVideoId) { throw new Exception(); }


            SetTitle(data.Video.Title);
            SetThumbnailImage(data.Video.ThumbnailUrl.OriginalString);
            SetSubmitDate(data.Video.UploadTime);
            SetVideoDuration(data.Video.Length);
            SetDescription((int)data.Video.ViewCount, (int)data.Thread.GetCommentCount(), (int)data.Video.MylistCount);

            IsInitialized = true;
        }
    }






    [Flags]
    public enum VideoStatus
    {
        Watched = 0x0001,
        Filtered = 0x1000,
    }
}
