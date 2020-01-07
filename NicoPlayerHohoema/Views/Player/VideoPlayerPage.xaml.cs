using Prism.Commands;
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
using Windows.Media.Playback;
using Windows.UI.Core;
using System.Windows.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace NicoPlayerHohoema.Views
{

    public enum PlayerSidePaneContentType
    {
        Playlist,
        Comment,
        Setting,
        RelatedVideos,
    }

    public sealed partial class VideoPlayerPage : Page
    {
        public VideoPlayerPage()
        {
            this.InitializeComponent();

            _mediaPlayer = App.Current.Container.Resolve<MediaPlayer>();
            _mediaPlayer.VolumeChanged += OnMediaPlayerVolumeChanged;
            _mediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
            VolumeSlider.Value = _mediaPlayer.Volume;

            _UIdispatcher = Dispatcher;

            SeekBarSlider.ManipulationMode = ManipulationModes.TranslateX;
            SeekBarSlider.ManipulationStarting += SeekBarSlider_ManipulationStarting;
            SeekBarSlider.ManipulationStarted += SeekBarSlider_ManipulationStarted;

            SeekBarSlider.FocusEngaged += SeekBarSlider_FocusEngaged;
            SeekBarSlider.FocusDisengaged += SeekBarSlider_FocusDisengaged;
        }

        public TimeSpan ForwardSeekTime => TimeSpan.FromSeconds(30);
        public TimeSpan PreviewSeekTime => TimeSpan.FromSeconds(-10);

        public List<double> PlaybackRateList { get; } = new List<double>
        {
            2.0,
            1.75,
            1.5,
            1.25,
            1.0,
            0.75,
            0.5,
            0.25,
            0.05
        };




        public bool IsDisplayControlUI
        {
            get { return (bool)GetValue(IsDisplayControlUIProperty); }
            set { SetValue(IsDisplayControlUIProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsDisplayControlUI.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsDisplayControlUIProperty =
            DependencyProperty.Register("IsDisplayControlUI", typeof(bool), typeof(VideoPlayerPage), new PropertyMetadata(true));



        CoreDispatcher _UIdispatcher;
        private readonly MediaPlayer _mediaPlayer;
        bool _nowVolumeChanging;
        private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_nowVolumeChanging) { return; }

            _mediaPlayer.Volume = e.NewValue;
        }

        private void OnMediaPlayerVolumeChanged(MediaPlayer sender, object args)
        {
            _nowVolumeChanging = true;
            try
            {
                _ = _UIdispatcher.RunAsync(CoreDispatcherPriority.Normal, () => 
                {
                    VolumeSlider.Value = sender.Volume;
                });
            }
            finally
            {
                _nowVolumeChanging = false;
            }
        }






        public TimeSpan VideoPosition
        {
            get { return (TimeSpan)GetValue(VideoPositionProperty); }
            set { SetValue(VideoPositionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for VideoPositionSeconds.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VideoPositionProperty =
            DependencyProperty.Register("VideoPosition", typeof(TimeSpan), typeof(VideoPlayerPage), new PropertyMetadata(TimeSpan.Zero));




        public bool NowVideoPositionChanging
        {
            get { return (bool)GetValue(NowVideoPositionChangingProperty); }
            set { SetValue(NowVideoPositionChangingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NowVideoPositionChanging.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NowVideoPositionChangingProperty =
            DependencyProperty.Register("NowVideoPositionChanging", typeof(bool), typeof(VideoPlayerPage), new PropertyMetadata(false));




        void RefrectSliderPositionToPlaybackPosition()
        {
            _mediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(SeekBarSlider.Value);
        }

        private void SeekBarSlider_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            e.Complete();
        }

        private void SeekBarSlider_ManipulationStarting(object sender, ManipulationStartingRoutedEventArgs e)
        {
            NowVideoPositionChanging = true;
            SeekSwipe.IsEnabled = false;

            Window.Current.CoreWindow.PointerReleased += CoreWindow_PointerReleased;
        }

        private void CoreWindow_PointerReleased(CoreWindow sender, PointerEventArgs args)
        {
            Window.Current.CoreWindow.PointerReleased -= CoreWindow_PointerReleased;

            NowVideoPositionChanging = false;
            SeekSwipe.IsEnabled = true;

            RefrectSliderPositionToPlaybackPosition();
        }

        private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            _ = _UIdispatcher.RunAsync(CoreDispatcherPriority.Normal, (DispatchedHandler)(() => 
            {
                VideoPosition = sender.Position;
                if (!this.NowVideoPositionChanging)
                {
                    SeekBarSlider.Value = VideoPosition.TotalSeconds;
                }
            }));
        }


        private void SeekBarSlider_FocusEngaged(Control sender, FocusEngagedEventArgs args)
        {
            NowVideoPositionChanging = true;
        }

        private void SeekBarSlider_FocusDisengaged(Control sender, FocusDisengagedEventArgs args)
        {
            RefrectSliderPositionToPlaybackPosition();

            NowVideoPositionChanging = false;
        }






        #region SidePageContent



        public PlayerSidePaneContentType? SidePaneType
        {
            get { return (PlayerSidePaneContentType?)GetValue(SidePaneTypeProperty); }
            set { SetValue(SidePaneTypeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SidePaneType.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SidePaneTypeProperty =
            DependencyProperty.Register("SidePaneType", typeof(PlayerSidePaneContentType?), typeof(VideoPlayerPage), new PropertyMetadata(default(PlayerSidePaneContentType?)));


        DelegateCommand<string> _selectSidePaneCommand;
        DelegateCommand<string> SelectSidePaneCommand => _selectSidePaneCommand
            ?? (_selectSidePaneCommand = new DelegateCommand<string>(str => 
            {
                if (Enum.TryParse<PlayerSidePaneContentType>(str, out var type))
                {
                    SidePaneType = type;
                }
            }));

        #endregion
    }
}
