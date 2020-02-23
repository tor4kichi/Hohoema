using I18NPortable;
using NicoPlayerHohoema.UseCase.Playlist;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Prism.Ioc;
using Prism.Events;
using System.Reactive.Disposables;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Cache;
using Windows.UI.Core;
using System.Threading.Tasks;
using NicoPlayerHohoema.Repository.NicoVideo;
using NicoPlayerHohoema.Models.Helpers;
using NicoPlayerHohoema.Repository.VideoCache;
using Reactive.Bindings.Extensions;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace NicoPlayerHohoema.Views.Pages.VideoListPage
{
    public sealed partial class VideoListItemControl : UserControl
    {
        static public string LocalizedText_PostAt_Short = "VideoPostAt_Short".Translate();
        static public string LocalizedText_ViewCount_Short = "ViewCount_Short".Translate();
        static public string LocalizedText_CommentCount_Short = "CommentCount_Short".Translate();
        static public string LocalizedText_MylistCount_Short = "MylistCount_Short".Translate();

        public VideoListItemControl()
        {
            this.InitializeComponent();

            Loaded += VideoListItemControl_Loaded;
            Unloaded += VideoListItemControl_Unloaded;

            DataContextChanged += VideoListItemControl_DataContextChanged;

            _dispatcher = Dispatcher;

            _cacheManager = App.Current.Container.Resolve<Models.Cache.VideoCacheManager>();
            
            _ngSettings = App.Current.Container.Resolve<Models.NGSettings>();
        }

        static VideoListItemControl()
        {
            _videoInfoRepository = App.Current.Container.Resolve<Repository.NicoVideo.VideoInfoRepository>();
        }

        NGSettings _ngSettings;

        Interfaces.IVideoContent _context;

        static VideoInfoRepository _videoInfoRepository;
        static AsyncLock _updateLock = new AsyncLock();

        private void VideoListItemControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (args.NewValue != null)
            {
                _ = InitializeAsync();
            }
            else
            {
                UnsubscriptionWatched();
                UnsubscribeCacheState();
                UnsubscribeNGVideoOwnerChanged();
            }
        }

        private void VideoListItemControl_Loaded(object sender, RoutedEventArgs e)
        {
            _ = InitializeAsync();
        }

        private async void VideoListItemControl_Unloaded(object sender, RoutedEventArgs e)
        {
            using (await _updateLock.LockAsync())
            {
                UnsubscriptionWatched();
                UnsubscribeCacheState();
                UnsubscribeNGVideoOwnerChanged();
            }
        }

        async Task InitializeAsync()
        {
            using (await _updateLock.LockAsync())
            {
                if (DataContext is Interfaces.IVideoContent video)
                {
                    if (_context == video)
                    {
                        return;
                    }

                    IsInitialized = false;

                    if (video is Interfaces.IVideoContentWritable videoContent)
                    {
                        await _videoInfoRepository.UpdateAsync(videoContent);
                    }

                    await Task.Delay(25);

                    SubscriptionWatchedIfNotWatch(video);
                    SubscribeCacheState(video);
                    SubscribeNGVideoOwnerChanged(video);

                    _context = video;

                    IsInitialized = true;
                }
            }
        }





        public bool IsThumbnailUseCache
        {
            get { return (bool)GetValue(IsThumbnailUseCacheProperty); }
            set { SetValue(IsThumbnailUseCacheProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsThumbnailUseCache.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsThumbnailUseCacheProperty =
            DependencyProperty.Register("IsThumbnailUseCache", typeof(bool), typeof(VideoListItemControl), new PropertyMetadata(true));






        public bool IsInitialized
        {
            get { return (bool)GetValue(IsInitializedProperty); }
            set { SetValue(IsInitializedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsInitialized.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsInitializedProperty =
            DependencyProperty.Register("IsInitialized", typeof(bool), typeof(VideoListItemControl), new PropertyMetadata(false));




        #region NG Video Owner

        private void SubscribeNGVideoOwnerChanged(Interfaces.IVideoContent video)
        {
            UnsubscribeNGVideoOwnerChanged();

            UpdateIsHidenVideoOwner(video);

            _ngSettings.NGVideoOwnerUserIds.CollectionChanged += NGVideoOwnerUserIds_CollectionChanged;
        }

        private void UnsubscribeNGVideoOwnerChanged()
        {
            _ngSettings.NGVideoOwnerUserIds.CollectionChanged -= NGVideoOwnerUserIds_CollectionChanged;
        }

        private void NGVideoOwnerUserIds_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var video = DataContext as Interfaces.IVideoContent;
            UpdateIsHidenVideoOwner(video);
        }


        void UpdateIsHidenVideoOwner(Interfaces.IVideoContent video)
        {
            if (video != null)
            {
                VideoHiddenInfo = _ngSettings.IsNgVideo(video);
            }
            else
            {
                VideoHiddenInfo = null;
            }
        }




        public NGResult VideoHiddenInfo
        {
            get { return (NGResult)GetValue(VideoHiddenInfoProperty); }
            set { SetValue(VideoHiddenInfoProperty, value); }
        }

        // Using a DependencyProperty as the backing store for VideoHiddenInfo.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VideoHiddenInfoProperty =
            DependencyProperty.Register("VideoHiddenInfo", typeof(NGResult), typeof(VideoListItemControl), new PropertyMetadata(null));




        public bool IsRevealHiddenVideo
        {
            get { return (bool)GetValue(IsRevealHiddenVideoProperty); }
            set { SetValue(IsRevealHiddenVideoProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsRevealHiddenVideo.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsRevealHiddenVideoProperty =
            DependencyProperty.Register("IsRevealHiddenVideo", typeof(bool), typeof(VideoListItemControl), new PropertyMetadata(false));


        private void HiddenVideoOnceRevealButton_Click(object sender, RoutedEventArgs e)
        {
            IsRevealHiddenVideo = true;
        }

        private void ExitRevealButton_Click(object sender, RoutedEventArgs e)
        {
            IsRevealHiddenVideo = false;
        }

        private void UnregistrationHiddenVideoOwnerButton_Click(object sender, RoutedEventArgs e)
        {
            IsRevealHiddenVideo = false;
            VideoHiddenInfo = null;

            if (DataContext is Interfaces.IVideoContent video)
            {
                _ngSettings.RemoveNGVideoOwnerId(video.ProviderId);
            }
        }
        #endregion

        #region Cache



        public CacheRequest CacheRequest
        {
            get { return (CacheRequest)GetValue(CacheRequestProperty); }
            set { SetValue(CacheRequestProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CacheRequests.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CacheRequestProperty =
            DependencyProperty.Register("CacheRequest", typeof(CacheRequest), typeof(VideoListItemControl), new PropertyMetadata(default(CacheRequest)));

        public bool HasCacheProgress
        {
            get { return (bool)GetValue(HasCacheProgressProperty); }
            set { SetValue(HasCacheProgressProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HasCacheProgress.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HasCacheProgressProperty =
            DependencyProperty.Register("HasCacheProgress", typeof(bool), typeof(VideoListItemControl), new PropertyMetadata(false));



        public double DownloadProgress
        {
            get { return (double)GetValue(DownloadProgressProperty); }
            set { SetValue(DownloadProgressProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DownloadProgress.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DownloadProgressProperty =
            DependencyProperty.Register("DownloadProgress", typeof(double), typeof(VideoListItemControl), new PropertyMetadata(0.0));




        public bool IsProgressUnknown
        {
            get { return (bool)GetValue(IsProgressUnknownProperty); }
            set { SetValue(IsProgressUnknownProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsProgressUnknown.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsProgressUnknownProperty =
            DependencyProperty.Register("IsProgressUnknown", typeof(bool), typeof(VideoListItemControl), new PropertyMetadata(false));




        public NicoVideoQuality? CacheProgressQuality
        {
            get { return (NicoVideoQuality?)GetValue(CacheProgressQualityProperty); }
            set { SetValue(CacheProgressQualityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CacheProgressQuality.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CacheProgressQualityProperty =
            DependencyProperty.Register("CacheProgressQuality", typeof(NicoVideoQuality?), typeof(VideoListItemControl), new PropertyMetadata(default(NicoVideoQuality?)));



        private VideoCacheManager _cacheManager;



        private void SubscribeCacheState(Interfaces.IVideoContent video)
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

        void ResetCacheRequests(Interfaces.IVideoContent video, CacheRequest cacheRequest)
        {
            ClearHandleProgress();

            _ = _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
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
            if (DataContext is Interfaces.IVideoContent video)
            {
                if (e.Request.VideoId == video.Id)
                {
                    _ = _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => 
                    {
                        ResetCacheRequests(video, e.Request);
                    });
                }
            }
        }

        private void UnsubscribeCacheState()
        {
            CacheRequest = null;
            _cacheManager.VideoCacheStateChanged -= _cacheManager_VideoCacheStateChanged;
        }

        Models.Cache.NicoVideoCacheProgress _progress;
        private CoreDispatcher _dispatcher;

        IDisposable _progressObserver;
        double _totalSizeInverted;
        private void HandleProgress(Models.Cache.NicoVideoCacheProgress progress)
        {
            HasCacheProgress = true;
            DownloadProgress = default; // nullの時はゲージ表示を曖昧に表現する
            CacheProgressQuality = progress.Quality;
            IsProgressUnknown = double.IsInfinity(progress.DownloadOperation.Progress.TotalBytesToReceive);
            _progress = progress;
            _progressObserver = _progress.ObserveProperty(x => x.Progress)
                .Subscribe(async x => 
                {
                    await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
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


        #region Video Watched

        public bool IsWatched
        {
            get { return (bool)GetValue(IsWatchedProperty); }
            set { SetValue(IsWatchedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsWatched.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsWatchedProperty =
            DependencyProperty.Register("IsWatched", typeof(bool), typeof(VideoListItemControl), new PropertyMetadata(false));

        SubscriptionToken _watchedDisposable;

        void Watched(UseCase.Playlist.Events.VideoPlayedEvent.VideoPlayedEventArgs args)
        {
            if (DataContext is Interfaces.IVideoContent video
                && video.Id == args.ContentId
                )
            {
                IsWatched = true;
                var eventAggregator = App.Current.Container.Resolve<IEventAggregator>();
                var palyedEvent = eventAggregator.GetEvent<UseCase.Playlist.Events.VideoPlayedEvent>();
                palyedEvent.Unsubscribe(_watchedDisposable);
                _watchedDisposable = null;
            }
        }

        void SubscriptionWatchedIfNotWatch(Interfaces.IVideoContent video)
        {
            UnsubscriptionWatched();

            if (video != null)
            {
                var watched = Database.VideoPlayedHistoryDb.IsVideoPlayed(video.Id);
                IsWatched = watched;
                if (!watched)
                {
                    var eventAggregator = App.Current.Container.Resolve<IEventAggregator>();
                    var palyedEvent = eventAggregator.GetEvent<UseCase.Playlist.Events.VideoPlayedEvent>();
                    _watchedDisposable =  palyedEvent.Subscribe(Watched, ThreadOption.UIThread);
                }
            }
        }

        void UnsubscriptionWatched()
        {
            _watchedDisposable?.Dispose();
        }

        #endregion

    }
}
