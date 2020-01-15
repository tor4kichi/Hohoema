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
            CacheRequests = new List<Models.Cache.NicoVideoCacheRequest>();
        }

        private void VideoListItemControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (args.NewValue != null)
            {
                SubscriptionWatchedIfNotWatch();
                SubscribeCacheState();
            }
            else
            {
                UnsubscriptionWatched();
                UnsubscribeCacheState();
            }
        }

        private void VideoListItemControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void VideoListItemControl_Unloaded(object sender, RoutedEventArgs e)
        {
            UnsubscriptionWatched();
            UnsubscribeCacheState();
        }


        #region Cache



        public List<Models.Cache.NicoVideoCacheRequest> CacheRequests
        {
            get { return (List<Models.Cache.NicoVideoCacheRequest>)GetValue(CacheRequestsProperty); }
            set { SetValue(CacheRequestsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CacheRequests.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CacheRequestsProperty =
            DependencyProperty.Register("CacheRequests", typeof(List<Models.Cache.NicoVideoCacheRequest>), typeof(VideoListItemControl), new PropertyMetadata(null));

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




        public NicoVideoQuality? CacheProgressQuality
        {
            get { return (NicoVideoQuality?)GetValue(CacheProgressQualityProperty); }
            set { SetValue(CacheProgressQualityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CacheProgressQuality.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CacheProgressQualityProperty =
            DependencyProperty.Register("CacheProgressQuality", typeof(NicoVideoQuality?), typeof(VideoListItemControl), new PropertyMetadata(default(NicoVideoQuality?)));



        private VideoCacheManager _cacheManager;



        private void SubscribeCacheState()
        {
            UnsubscribeCacheState();

            System.Diagnostics.Debug.Assert(DataContext != null);

            if (DataContext is Interfaces.IVideoContent video)
            {
                _cacheManager.VideoCacheStateChanged += _cacheManager_VideoCacheStateChanged;

                ResetIsCached(video);
                ResetCacheRequests(video);
            }
        }

        void ResetIsCached(Interfaces.IVideoContent video)
        {
            var cached = _cacheManager.CheckCached(video.Id);
        }

        void ResetCacheRequests(Interfaces.IVideoContent video)
        {
            ClearHandleProgress();

            _cacheManager.GetCacheRequest(video.Id)
                .ContinueWith(async prevTask => 
                {
                    await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => 
                    {
                        var cacheRequests = await prevTask;
                        CacheRequests = cacheRequests;

                        var progressRequest = cacheRequests.FirstOrDefault(x => x is Models.Cache.NicoVideoCacheProgress);
                        if (progressRequest is Models.Cache.NicoVideoCacheProgress progress)
                        {
                            HandleProgress(progress);
                        }
                    });
                });
        }

        private void _cacheManager_VideoCacheStateChanged(object sender, VideoCacheStateChangedEventArgs e)
        {
            if (DataContext is Interfaces.IVideoContent video)
            {
                if (e.Request.RawVideoId == video.Id)
                {
                    _ = _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => 
                    {
                        ResetIsCached(video);
                        ResetCacheRequests(video);
                    });
                }
            }
        }

        private void UnsubscribeCacheState()
        {
            _cacheManager.VideoCacheStateChanged -= _cacheManager_VideoCacheStateChanged;
        }

        Models.Cache.NicoVideoCacheProgress _progress;
        private CoreDispatcher _dispatcher;

        double _totalSizeInverted;
        private void HandleProgress(Models.Cache.NicoVideoCacheProgress progress)
        {
            HasCacheProgress = true;
            DownloadProgress = default; // nullの時はゲージ表示を曖昧に表現する
            CacheProgressQuality = progress.Quality;

            var ranges = progress.DownloadOperation.GetDownloadedRanges();

            progress.DownloadOperation.RangesDownloaded += DownloadOperation_RangesDownloaded;
            _progress = progress;
            _totalSizeInverted = 1.0 / progress.DownloadOperation.Progress.TotalBytesToReceive;
        }

        private void DownloadOperation_RangesDownloaded(Windows.Networking.BackgroundTransfer.DownloadOperation sender, Windows.Networking.BackgroundTransfer.BackgroundTransferRangesDownloadedEventArgs args)
        {
            var byteRecieved = sender.Progress.BytesReceived;
            _ = _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => 
            {
                DownloadProgress = byteRecieved * _totalSizeInverted;
            });
        }


        private void ClearHandleProgress()
        {
            if (_progress != null)
            {
                _progress.DownloadOperation.RangesDownloaded -= DownloadOperation_RangesDownloaded;
                _progress = null;
            }

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

        void SubscriptionWatchedIfNotWatch()
        {
            UnsubscriptionWatched();

            if (DataContext is Interfaces.IVideoContent video)
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
