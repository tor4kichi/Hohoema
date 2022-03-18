using Hohoema.Models.UseCase.Niconico.Player;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.System;
using Hohoema.Models.Domain.Player;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace Hohoema.Presentation.Views.Player.VideoPlayerUI
{
    public sealed partial class NallowPlayerUI : UserControl
    {
        private readonly DispatcherQueue _dispatcherQueue;
        public NallowPlayerUI()
        {
            this.InitializeComponent();

            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            _soundVolumeManager = Microsoft.Toolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<MediaPlayerSoundVolumeManager>();

            CommentTextBox.GotFocus += CommentTextBox_GotFocus;
            CommentTextBox.LostFocus += CommentTextBox_LostFocus;

            Loaded += NallowPlayerUI_Loaded;
            Unloaded += NallowPlayerUI_Unloaded;
        }


        private void NallowPlayerUI_Loaded(object sender, RoutedEventArgs e)
        {
            MediaPlayer.VolumeChanged += OnMediaPlayerVolumeChanged;
            MediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;

        }

        private void NallowPlayerUI_Unloaded(object sender, RoutedEventArgs e)
        {
            MediaPlayer.VolumeChanged -= OnMediaPlayerVolumeChanged;
            MediaPlayer.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;
        }




        public MediaPlayer MediaPlayer
        {
            get { return (MediaPlayer)GetValue(MediaPlayerProperty); }
            set { SetValue(MediaPlayerProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MediaPlayer.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MediaPlayerProperty =
            DependencyProperty.Register("MediaPlayer", typeof(MediaPlayer), typeof(NallowPlayerUI), new PropertyMetadata(null));




        public double MediaControlWidth
        {
            get { return (double)GetValue(MediaControlWidthProperty); }
            set { SetValue(MediaControlWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MediaControlWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MediaControlWidthProperty =
            DependencyProperty.Register("MediaControlWidth", typeof(double), typeof(NallowPlayerUI), new PropertyMetadata(0));



        #region Comment Textbox

        bool _prevMediaPlayerPlaying;
        private void CommentTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Player.VideoPlayerPageViewModel vm)
            {
                if (vm.PlayerSettings.PauseWithCommentWriting)
                {
                    _prevMediaPlayerPlaying = MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing;
                    MediaPlayer.Pause();
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
                        MediaPlayer.Play();
                    }
                }
            }
        }


        #endregion Comment Textbox

        #region Seekbar Slider

        public TimeSpan VideoPosition
        {
            get { return (TimeSpan)GetValue(VideoPositionProperty); }
            set { SetValue(VideoPositionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for VideoPositionSeconds.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VideoPositionProperty =
            DependencyProperty.Register("VideoPosition", typeof(TimeSpan), typeof(NallowPlayerUI), new PropertyMetadata(TimeSpan.Zero));


        private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            _ = _dispatcherQueue.TryEnqueue(() =>
            {
                VideoPosition = sender.Position;
            });
        }

        #endregion Seekbar Slider


        


        #region Sound Volume
        private readonly MediaPlayerSoundVolumeManager _soundVolumeManager;
        bool _nowVolumeChanging;

        private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_nowVolumeChanging) { return; }

            _soundVolumeManager.Volume = e.NewValue;
        }

        private void OnMediaPlayerVolumeChanged(MediaPlayer sender, object args)
        {
            _nowVolumeChanging = true;
            _dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    VolumeSlider.Value = _soundVolumeManager.Volume;
                }
                finally
                {
                    _nowVolumeChanging = false;
                }
            });
        }

        #endregion Sound Volume
    }
}
