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
        }


        private void VideoListItemControl_Loaded(object sender, RoutedEventArgs e)
        {
            SubscriptionWatchedIfNotWatch();
        }

        private void VideoListItemControl_Unloaded(object sender, RoutedEventArgs e)
        {
            UnsubscriptionWatched();
        }

        #region Video Watched

        public bool IsWatched
        {
            get { return (bool)GetValue(IsWatchedProperty); }
            set { SetValue(IsWatchedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsWatched.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsWatchedProperty =
            DependencyProperty.Register("IsWatched", typeof(bool), typeof(VideoListItemControl), new PropertyMetadata(false));

        IDisposable _watchedDisposable;

        void Watched(UseCase.Playlist.Events.VideoPlayedEvent.VideoPlayedEventArgs args)
        {
            if (DataContext is Interfaces.IVideoContent video
                && video.Id == args.ContentId
                )
            {
                IsWatched = true;
                _watchedDisposable.Dispose();
                _watchedDisposable = null;
            }
        }

        void SubscriptionWatchedIfNotWatch()
        {
            _watchedDisposable = Disposable.Empty;
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
