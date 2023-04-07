using CommunityToolkit.Mvvm.Input;
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
using Windows.Media.Playback;
using Windows.UI.Core;
using System.Windows.Input;
using Reactive.Bindings.Extensions;
using Windows.UI;
using Microsoft.Toolkit.Uwp.Helpers;
using Hohoema.Models.Domain;
using Hohoema.Models.UseCase.Niconico.Player;
using Hohoema.Models.Domain.Application;
using System.Diagnostics;
using Hohoema.Models.Domain.Player;
using Windows.System;
using Hohoema.ViewModels.Player;
using Hohoema.Models.Domain.Niconico.Video;
using System.Reactive.Disposables;
using Hohoema.Navigations;
using CommunityToolkit.Mvvm.DependencyInjection;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Hohoema.Views.Player
{
    public sealed partial class LegacyVideoPlayerPage : Page
    {


        public bool NowCommentEditting
        {
            get { return (bool)GetValue(NowCommentEdittingProperty); }
            set { SetValue(NowCommentEdittingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NowCommentEditting.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NowCommentEdittingProperty =
            DependencyProperty.Register("NowCommentEditting", typeof(bool), typeof(LegacyVideoPlayerPage), new PropertyMetadata(false));



        private readonly DispatcherQueue _dispatcherQueue;

        public LegacyVideoPlayerPage()
        {
            this.InitializeComponent();

            _UIdispatcher = Dispatcher;

            _mediaPlayer = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<MediaPlayer>();
            _soundVolumeManager = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<MediaPlayerSoundVolumeManager>();
            VolumeSlider.Value = _soundVolumeManager.Volume;

            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();


            //SeekBarSlider.ManipulationMode = ManipulationModes.TranslateX;
            //SeekBarSlider.ManipulationStarting += SeekBarSlider_ManipulationStarting;
            //SeekBarSlider.ManipulationStarted += SeekBarSlider_ManipulationStarted;

            //SeekBarSlider.FocusEngaged += SeekBarSlider_FocusEngaged;
            //SeekBarSlider.FocusDisengaged += SeekBarSlider_FocusDisengaged;

            CommentTextBox.GotFocus += CommentTextBox_GotFocus;
            CommentTextBox.LostFocus += CommentTextBox_LostFocus;

            MediaControl.SizeChanged += MediaControl_SizeChanged;

            Loaded += VideoPlayerPage_Loaded;
            Unloaded += VideoPlayerPage_Unloaded;

            DataContext = _vm = Ioc.Default.GetRequiredService<LegacyVideoPlayerPageViewModel>();
        }

        private readonly LegacyVideoPlayerPageViewModel _vm;

        CompositeDisposable _compositeDisposable;

        private void VideoPlayerPage_Loaded(object sender, RoutedEventArgs e)
        {
            _mediaPlayer.VolumeChanged -= OnMediaPlayerVolumeChanged;
            _mediaPlayer.VolumeChanged += OnMediaPlayerVolumeChanged;
            _mediaPlayer.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;
            _mediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;

            SeekBarSlider.ValueChanged += SeekBarSlider_ValueChanged;

            _compositeDisposable = new CompositeDisposable();
            var appearanceSettings = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<AppearanceSettings>();
            appearanceSettings.ObserveProperty(x => x.ApplicationTheme)
                .Subscribe(theme =>
                {
                    ThemeChanged(theme);
                }).AddTo(_compositeDisposable);

            LayoutRoot.SizeChanged += LayoutRoot_SizeChanged;
            
            _prevPosition = 0.0;
        }

        private void LayoutRoot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            IsDisplayControlUI = false;
        }

        private void VideoPlayerPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _compositeDisposable.Dispose();
            _mediaPlayer.VolumeChanged -= OnMediaPlayerVolumeChanged;
            _mediaPlayer.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;
            
            SeekBarSlider.ValueChanged -= SeekBarSlider_ValueChanged;
            SeekBarSlider.Value = 0.0;

            LayoutRoot.SizeChanged -= LayoutRoot_SizeChanged;
        }



        /// <summary>
        /// ActualWidthは初回描画後はRaiseされないため手動で幅情報を更新する
        /// </summary>
        public double MediaControlWidth
        {
            get { return (double)GetValue(MediaControlWidthProperty); }
            set { SetValue(MediaControlWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MediaControlWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MediaControlWidthProperty =
            DependencyProperty.Register("MediaControlWidth", typeof(double), typeof(LegacyVideoPlayerPage), new PropertyMetadata(0.0));


        private void MediaControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            MediaControlWidth = e.NewSize.Width;
        }

        void ThemeChanged(ElementTheme theme)
        {
            ApplicationTheme appTheme;
            if (theme == ElementTheme.Default)
            {
                if (theme == ElementTheme.Default)
                {
                    appTheme = Views.Helpers.SystemThemeHelper.GetSystemTheme();
                }
                else if (theme == ElementTheme.Dark)
                {
                    appTheme = ApplicationTheme.Dark;
                }
                else
                {
                    appTheme = ApplicationTheme.Light;
                }
            }
            else if (theme == ElementTheme.Light)
            {
                appTheme = ApplicationTheme.Light;
            }
            else
            {
                appTheme = ApplicationTheme.Dark;
            }

            if (appTheme == ApplicationTheme.Light)
            {
                var color = "#00FFFFFF".ToColor();
                CenterTopGradientStop_End.Color = color;
                CenterBottomGradientStop_End.Color = color;
            }
            else
            {
                var color = "#00000000".ToColor();
                CenterTopGradientStop_End.Color = color;
                CenterBottomGradientStop_End.Color = color;
            }
        }


        bool _prevMediaPlayerPlaying;
        private void CommentTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Player.VideoPlayerPageViewModel vm)
            {
                if (vm.PlayerSettings.PauseWithCommentWriting)
                {
                    _prevMediaPlayerPlaying = _mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing;
                    _mediaPlayer.Pause();
                }
            }
        }

        private void CommentTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Player.VideoPlayerPageViewModel vm)
            {
                if (vm.PlayerSettings.PauseWithCommentWriting)
                {
                    if (_prevMediaPlayerPlaying)
                    {
                        _mediaPlayer.Play();
                    }
                }
            }
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
            DependencyProperty.Register("IsDisplayControlUI", typeof(bool), typeof(LegacyVideoPlayerPage), new PropertyMetadata(false));



        public void HideControlUI()
        {
            IsDisplayControlUI = false;
        }

        public void ShowControlUI()
        {
            IsDisplayControlUI = true;
        }

        public void ToggleControlUI()
        {
            IsDisplayControlUI = true;
        }


        private readonly MediaPlayerSoundVolumeManager _soundVolumeManager;

        CoreDispatcher _UIdispatcher;
        private readonly MediaPlayer _mediaPlayer;
        bool _nowVolumeChanging;
        private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_nowVolumeChanging) { return; }

            _soundVolumeManager.Volume = e.NewValue;
        }

        private void OnMediaPlayerVolumeChanged(MediaPlayer sender, object args)
        {
            _nowVolumeChanging = true;
            try
            {
                _ = _UIdispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    VolumeSlider.Value = _soundVolumeManager.Volume;
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
            DependencyProperty.Register("VideoPosition", typeof(TimeSpan), typeof(LegacyVideoPlayerPage), new PropertyMetadata(TimeSpan.Zero));




        public bool NowVideoPositionChanging
        {
            get { return (bool)GetValue(NowVideoPositionChangingProperty); }
            set { SetValue(NowVideoPositionChangingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NowVideoPositionChanging.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NowVideoPositionChangingProperty =
            DependencyProperty.Register("NowVideoPositionChanging", typeof(bool), typeof(LegacyVideoPlayerPage), new PropertyMetadata(false));
        

        void RefrectSliderPositionToPlaybackPosition()
        {
            this.NowVideoPositionChanging = true;
            if (_mediaPlayer.PlaybackSession.PlaybackState is MediaPlaybackState.Playing or MediaPlaybackState.Paused)
            {
                _mediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(SeekBarSlider.Value);
            }
            this.NowVideoPositionChanging = false;
        }
        
        //private void SeekBarSlider_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        //{
        //    e.Complete();
        //}

        //private void SeekBarSlider_ManipulationStarting(object sender, ManipulationStartingRoutedEventArgs e)
        //{
        //    NowVideoPositionChanging = true;
        //    SeekSwipe.IsEnabled = false;

        //    Window.Current.CoreWindow.PointerReleased += CoreWindow_PointerReleased;

        //    SeekBarSlider.ManipulationCompleted += SeekBarSlider_ManipulationCompleted;
        //    SeekBarSlider.ManipulationCompleted -= SeekBarSlider_ManipulationCompleted;
        //}
        

        double _prevPosition;
        private void SeekBarSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (Math.Abs(_prevPosition - e.NewValue) > 1.0)
            {
                RefrectSliderPositionToPlaybackPosition();
            }

            _prevPosition = e.NewValue;
        }

        private void SeekBarSlider_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            SeekBarSlider.ManipulationCompleted -= SeekBarSlider_ManipulationCompleted;
        }


        private void CoreWindow_PointerReleased(CoreWindow sender, PointerEventArgs args)
        {
            SeekBarSlider.ManipulationCompleted -= SeekBarSlider_ManipulationCompleted;
            Window.Current.CoreWindow.PointerReleased -= CoreWindow_PointerReleased;

            NowVideoPositionChanging = false;
            SeekSwipe.IsEnabled = true;

            RefrectSliderPositionToPlaybackPosition();
        }

        private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            _dispatcherQueue.TryEnqueue(() => 
            {
                if (sender.PlaybackState == MediaPlaybackState.None) { return; }
                VideoPosition = sender.Position;
                if (this.NowVideoPositionChanging is false)
                {
                    SeekBarSlider.Value = VideoPosition.TotalSeconds;
                }
            });
        }

        private void PlayPauseToggleKeyboardTrigger_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var currentFocus = FocusManager.GetFocusedElement() as FrameworkElement;
            if (currentFocus == PlayPauseButton) { return; }

            if (CommentTextBox.FocusState == FocusState.Unfocused && PlayPauseButton.FocusState == FocusState.Unfocused)
            {
                (_vm.VideoTogglePlayPauseCommand as ICommand).Execute(null);
            }
        }

        /*

        private void SeekBarSlider_FocusEngaged(Control sender, FocusEngagedEventArgs args)
        {
            NowVideoPositionChanging = true;
        }

        private void SeekBarSlider_FocusDisengaged(Control sender, FocusDisengagedEventArgs args)
        {
            RefrectSliderPositionToPlaybackPosition();

            NowVideoPositionChanging = false;
        }

        */




    }


    //public sealed class VideoQualityListItemContainerStyleSelector : StyleSelector
    //{
    //    public Style AvairableQuality { get; set; }
    //    public Style UnavairableQuality { get; set; }

    //    protected override Style SelectStyleCore(object item, DependencyObject container)
    //    {
    //        if (item is NicoVideoQualityEntity quality)
    //        {
    //            return quality.IsAvailable
    //                ? AvairableQuality
    //                : UnavairableQuality
    //                ;
    //        }

    //        return base.SelectStyleCore(item, container);
    //    }
    //}
}
