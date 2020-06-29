using Hohoema.Models;
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
using Windows.UI.Core;
using Hohoema.ViewModels.Pages;
using System.Reactive.Concurrency;
using Prism.Commands;
using Prism.Unity;
using I18NPortable;
using Prism.Events;
using System.Threading;
using Hohoema.Models.Repository;
using Hohoema.UseCase.VideoCache;
using Hohoema.Models.Repository.App;
using Hohoema.Models.Repository.Niconico.NicoVideo;
using Hohoema.Models.Repository.Niconico.Mylist;
using Hohoema.UseCase.Events;
using Hohoema.Models.Repository.VideoCache;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.VideoCache;
using NGResult = Hohoema.Models.Repository.App.NGResult;
using Hohoema.Database;

namespace Hohoema.ViewModels
{

    public class VideoInfoControlViewModel : HohoemaListingPageItemBase, IVideoContentWritable
    {
        public VideoInfoControlViewModel(Database.NicoVideo data)            
        {
            VideoCacheManager = App.Current.Container.Resolve<VideoCacheManager>();
            NicoVideoProvider = App.Current.Container.Resolve<NicoVideoProvider>();
            _ngSettings = App.Current.Container.Resolve<VideoListFilterSettings>();
            _cacheManager = App.Current.Container.Resolve<VideoCacheManager>();
            _scheduler = App.Current.Container.Resolve<IScheduler>();

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

            //_ngSettings.NGVideoOwnerUserIds.CollectionChanged += NGVideoOwnerUserIds_CollectionChanged;
        }



        protected override void OnDispose()
        {
            //_ngSettings.NGVideoOwnerUserIds.CollectionChanged -= NGVideoOwnerUserIds_CollectionChanged;
            UnsubscriptionWatched();

            base.OnDispose();
        }



        private void NGVideoOwnerUserIds_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateIsHidenVideoOwner(Data);
        }


        void UpdateIsHidenVideoOwner(IVideoContent video)
        {
            if (video != null && _ngSettings.IsNgVideo(video, out var ngResult))
            {
                VideoHiddenInfo = ngResult;
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
                _ngSettings.RemoveNgVideoOwner(Data.ProviderId);
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


        public NicoVideoProvider NicoVideoProvider { get; }
        public VideoCacheManager VideoCacheManager { get; }
        private VideoListFilterSettings _ngSettings { get; }
        private VideoCacheManager _cacheManager;
        private readonly IScheduler _scheduler;

        public string RawVideoId { get; }
        public Database.NicoVideo Data { get; private set; }

        public string Id => RawVideoId;


        public string ProviderId { get; set; }
        public string ProviderName { get; set; }
        public Database.NicoVideoUserType ProviderType { get; set; }

        public IMylist OnwerPlaylist { get; }

        public VideoStatus VideoStatus { get; private set; }

        public TimeSpan Length { get; set; }

        public DateTime PostedAt { get; set; }

        public int ViewCount { get; set; }

        public int MylistCount { get; set; }

        public int CommentCount { get; set; }

        public string ThumbnailUrl { get; set; }
        public bool IsDeleted { get; set; }

        bool IVideoContent.IsDeleted => IsDeleted;

        public NGResult VideoHiddenInfo { get; private set; }


        public bool IsWatched { get; private set; }
        SubscriptionToken _watchedDisposable;

        public bool IsInitialized { get; private set; }

        static Models.Helpers.AsyncLock _initializeLock = new Models.Helpers.AsyncLock();

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
                var watched = Database.VideoPlayedHistoryDb.IsVideoPlayed(video.Id);
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


        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (Data?.Title != null)
            {
                SetTitle(Data.Title);
            }

            if (Data == null)
            {
                var data = await Task.Run(async () =>
                {
                    if (IsDisposed)
                    {
                        Debug.WriteLine("skip thumbnail loading: " + RawVideoId);
                        return null;
                    }

                    if (NicoVideoProvider != null)
                    {
                        return await NicoVideoProvider.GetNicoVideoInfo(RawVideoId);
                    }

                    // オフライン時はローカルDBの情報を利用する
                    if (Data == null)
                    {
                        return Database.NicoVideoDb.Get(RawVideoId);
                    }

                    return null;
                });

                if (data == null) { return; }

                Data = data;
            }

            cancellationToken.ThrowIfCancellationRequested();

            SubscriptionWatchedIfNotWatch(Data);
            UpdateIsHidenVideoOwner(Data);
            SubscribeCacheState(Data);

            if (IsDisposed)
            {
                Debug.WriteLine("skip thumbnail loading: " + RawVideoId);
                return;
            }

            SetupFromThumbnail(Data);

            IsInitialized = true;
        }


        bool _isTitleNgCheckProcessed = false;
        bool _isOwnerIdNgCheckProcessed = false;

        public void SetupFromThumbnail(Database.NicoVideo info)
        {
            Debug.WriteLine("thumbnail reflect : " + info.RawVideoId);
            
            Label = info.Title;
            PostedAt = info.PostedAt;
            Length = info.Length;
            ViewCount = info.ViewCount;
            MylistCount = info.MylistCount;
            CommentCount = info.CommentCount;
            ThumbnailUrl = info.ThumbnailUrl;

            // NG判定
            if (_ngSettings != null)
            {
                NGResult ngResult = null;

                // タイトルをチェック
                if (!_isTitleNgCheckProcessed && !string.IsNullOrEmpty(info.Title))
                {
                    _ngSettings.IsNgVideoTitle(info.Title, out ngResult);
                    _isTitleNgCheckProcessed = true;
                }

                // 投稿者IDをチェック
                if (ngResult == null && 
                    !_isOwnerIdNgCheckProcessed && 
                    !string.IsNullOrEmpty(info.Owner?.OwnerId)
                    )
                {
                    _ngSettings.IsNgVideoOwner(info.Owner.OwnerId, out ngResult);
                    _isOwnerIdNgCheckProcessed = true;
                }

                if (ngResult != null)
                {
                    IsVisible = false;
                    var ngDesc = !string.IsNullOrWhiteSpace(ngResult.NGDescription) ? ngResult.NGDescription : ngResult.Content;
                    InvisibleDescription = $"NG動画";
                }
            }
                        
            SetTitle(info.Title);
            SetThumbnailImage(info.ThumbnailUrl);
            SetSubmitDate(info.PostedAt);
            SetVideoDuration(info.Length);
            if (!info.IsDeleted)
            {
                SetDescription(info.ViewCount, info.CommentCount, info.MylistCount);
            }
            else
            {
                if (info.PrivateReasonType != PrivateReasonType.None)
                {
                    Description = info.PrivateReasonType.Translate();
                }
                else
                {
                    Description = "視聴不可（配信終了など）";
                }
            }

            if (info.Owner != null)
            {
                ProviderId = info.Owner.OwnerId;
                ProviderName = info.Owner.ScreenName;
                ProviderType = info.Owner.UserType;
            }

        }

        internal void SetDescription(int viewcount, int commentCount, int mylistCount)
        {
            Description = $"再生:{viewcount.ToString("N0")} コメ:{commentCount.ToString("N0")} マイ:{mylistCount.ToString("N0")}";
        }

        internal void SetTitle(string title)
        {
            Label = title;
        }
        internal void SetSubmitDate(DateTime submitDate)
        {
            OptionText = submitDate.ToString("yyyy/MM/dd HH:mm");
            PostedAt = submitDate;
        }

        internal void SetVideoDuration(TimeSpan duration)
        {
            Length = duration;
            string timeText;
            if (duration.Hours > 0)
            {
                timeText = duration.ToString(@"hh\:mm\:ss");
            }
            else
            {
                timeText = duration.ToString(@"mm\:ss");
            }
            ImageCaption = timeText;
        }

        internal void SetThumbnailImage(string thumbnailImage)
        {
            if (!string.IsNullOrWhiteSpace(thumbnailImage))
            {
                AddImageUrl(thumbnailImage);
            }
        }



		protected virtual VideoPlayPayload MakeVideoPlayPayload()
		{
			return new VideoPlayPayload()
			{
				VideoId = RawVideoId,
				Quality = null,
			};
		}


    }






    [Flags]
    public enum VideoStatus
    {
        Watched = 0x0001,
        Filtered = 0x1000,
    }
}
