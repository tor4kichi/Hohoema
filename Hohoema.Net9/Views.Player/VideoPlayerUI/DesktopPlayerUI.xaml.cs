#nullable enable
using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.Models.Application;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Player;
using Hohoema.ViewModels.Player;
using Hohoema.ViewModels.PrimaryWindowCoreLayout;
using Hohoema.Views.Converters;
using Hohoema.Views.Helpers;
using I18NPortable;
using Microsoft.Toolkit.Uwp.Helpers;
using NiconicoToolkit.FollowingsActivity;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using Windows.Media.Playback;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace Hohoema.Views.Player.VideoPlayerUI;

public sealed partial class DesktopPlayerUI : UserControl, IDraggableAreaAware
{
    UIElement IDraggableAreaAware.GetDraggableArea()
    {
        if (_vm.PlayerView.IsCompactOverlay)
        {
            PlayerTopDraggableArea_Compact.IsHitTestVisible = true;
            PlayerTopDraggableArea_FullScreen.IsHitTestVisible = false;
            PlayerTopDraggableArea_FillWindow.IsHitTestVisible = false;
            return PlayerTopDraggableArea_Compact;
        }
        else if (_vm.PlayerView.IsFullScreen)
        {
            PlayerTopDraggableArea_Compact.IsHitTestVisible = false;
            PlayerTopDraggableArea_FullScreen.IsHitTestVisible = true;
            PlayerTopDraggableArea_FillWindow.IsHitTestVisible = false;
            return PlayerTopDraggableArea_FullScreen;
        }
        else
        {
            PlayerTopDraggableArea_Compact.IsHitTestVisible = false;
            PlayerTopDraggableArea_FullScreen.IsHitTestVisible = false;
            PlayerTopDraggableArea_FillWindow.IsHitTestVisible = true;
            return PlayerTopDraggableArea_FillWindow;
        }
    }

    private readonly DispatcherQueue _dispatcherQueue;

    public DesktopPlayerUI()
    {
        this.InitializeComponent();

        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _soundVolumeManager = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<MediaPlayerSoundVolumeManager>();
        VolumeSlider.Value = _soundVolumeManager.Volume;

        CommentTextBox.GotFocus += CommentTextBox_GotFocus;
        CommentTextBox.LostFocus += CommentTextBox_LostFocus;


        Loaded += DesktopPlayerUI_Loaded;
        Unloaded += DesktopPlayerUI_Unloaded;

        DataContext = _vm = Ioc.Default.GetRequiredService<VideoPlayerPageViewModel>();

        _positionUpdateTimer = _dispatcherQueue.CreateTimer();
        _positionUpdateTimer.Interval = TimeSpan.FromMilliseconds(250);
        _positionUpdateTimer.IsRepeating = true;
        _positionUpdateTimer.Tick += (t, s) => 
        {
            if (NowVideoPositionChanging || _nowSeekBarUpdating) { return; }

            var playbackSession = _vm.MediaPlayer.PlaybackSession;
            if (playbackSession.PlaybackState == MediaPlaybackState.None) { return; }

            // ユーザー操作ではないスライダー値の変更時はシーク処理を行わないようにしたい
            _nowSeekBarUpdating = true;
            try
            {
                var time = VideoPosition = playbackSession.Position;
                SeekBarSlider.Value = time.TotalSeconds;                
            }
            finally
            {
                _nowSeekBarUpdating = false;
            }
        };
    }

    private bool _nowSeekBarUpdating;

    DispatcherQueueTimer _positionUpdateTimer;

    private readonly VideoPlayerPageViewModel _vm;


    public bool IsVisibleUI
    {
        get { return (bool)GetValue(IsVisibleUIProperty); }
        set { SetValue(IsVisibleUIProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsVisibleUI.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsVisibleUIProperty =
        DependencyProperty.Register(nameof(IsVisibleUI), typeof(bool), typeof(DesktopPlayerUI), new PropertyMetadata(true));




    private void DesktopPlayerUI_Loaded(object sender, RoutedEventArgs e)
    {
        _vm.MediaPlayer.VolumeChanged += OnMediaPlayerVolumeChanged;

        SeekBarSlider.ValueChanged += SeekBarSlider_ValueChanged;

        _compositeDisposable = new CompositeDisposable();
        var appearanceSettings = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<AppearanceSettings>();
        appearanceSettings.ObserveProperty(x => x.ApplicationTheme)
            .Subscribe(theme =>
            {
                ThemeChanged(theme);
            }).AddTo(_compositeDisposable);

        _positionUpdateTimer.Start();
    }

    private void DesktopPlayerUI_Unloaded(object sender, RoutedEventArgs e)
    {
        _positionUpdateTimer.Stop();
        _vm.MediaPlayer.VolumeChanged -= OnMediaPlayerVolumeChanged;

        SeekBarSlider.ValueChanged -= SeekBarSlider_ValueChanged;
        SeekBarSlider.Value = 0.0;

        _compositeDisposable?.Dispose();        
    }

    #region Theme Changed

    CompositeDisposable _compositeDisposable;

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


    #endregion Theme Changed

    public double MediaControlWidth
    {
        get { return (double)GetValue(MediaControlWidthProperty); }
        set { SetValue(MediaControlWidthProperty, value); }
    }

    // Using a DependencyProperty as the backing store for MediaControlWidth.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty MediaControlWidthProperty =
        DependencyProperty.Register("MediaControlWidth", typeof(double), typeof(DesktopPlayerUI), new PropertyMetadata(0.0));


    private void MediaControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        MediaControlWidth = e.NewSize.Width;

        HideControlUI();
    }

    #region Comment Textbox


    public void ShowControlUI()
    {
        IsVisibleUI = true;
    }

    public void HideControlUI()
    {
        IsVisibleUI = false;
    }

    public void ToggleControlUI()
    {
        IsVisibleUI = !IsVisibleUI;
    }



    public bool NowCommentEditting
    {
        get { return (bool)GetValue(NowCommentEdittingProperty); }
        set { SetValue(NowCommentEdittingProperty, value); }
    }

    // Using a DependencyProperty as the backing store for NowCommentEditting.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty NowCommentEdittingProperty =
        DependencyProperty.Register(nameof(NowCommentEditting), typeof(bool), typeof(DesktopPlayerUI), new PropertyMetadata(false));





    bool _prevMediaPlayerPlaying;
    private void CommentTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        NowCommentEditting = true;
        (sender as TextBox).SelectAll();

        if (DataContext is ViewModels.Player.VideoPlayerPageViewModel vm)
        {
            if (vm.PlayerSettings.PauseWithCommentWriting)
            {
                _prevMediaPlayerPlaying = _vm.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing;
                _vm.MediaPlayer.Pause();
            }
        }
    }

    private void CommentTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        NowCommentEditting = false;
        if (DataContext is ViewModels.Player.VideoPlayerPageViewModel vm)
        {
            if (vm.PlayerSettings.PauseWithCommentWriting)
            {
                if (_prevMediaPlayerPlaying)
                {
                    _vm.MediaPlayer.Play();
                }
            }
        }
    }


    #endregion Comment Textbox


    #region Seekbar

    public List<double> PlaybackRateList { get; } = new List<double>
    {
        0.25,
        0.5,
        0.75,
        1.0,
        1.25,
        1.5,
        1.75,
        2.0,
    };


    public TimeSpan ForwardSeekTime => TimeSpan.FromSeconds(30);
    public TimeSpan PreviewSeekTime => TimeSpan.FromSeconds(-10);



    public TimeSpan VideoPosition
    {
        get { return (TimeSpan)GetValue(VideoPositionProperty); }
        set { SetValue(VideoPositionProperty, value); }
    }

    public static readonly DependencyProperty VideoPositionProperty =
        DependencyProperty.Register("VideoPosition", typeof(TimeSpan), typeof(DesktopPlayerUI), new PropertyMetadata(TimeSpan.Zero));

        
    private void SeekBarSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (NowVideoPositionChanging || _nowSeekBarUpdating) { return; }
        if (_vm.MediaPlayer.PlaybackSession.PlaybackState is not MediaPlaybackState.Playing and not MediaPlaybackState.Paused) { return; }
        if (Math.Abs(_vm.MediaPlayer.PlaybackSession.Position.TotalSeconds - e.NewValue) < 1.0) { return; }
        
        var session = _vm.MediaPlayer.PlaybackSession;
        NowVideoPositionChanging = true;
        session.SeekCompleted -= PlaybackSession_SeekCompleted;
        session.SeekCompleted += PlaybackSession_SeekCompleted;        
        session.Position = TimeSpan.FromSeconds(SeekBarSlider.Value);
                
        void PlaybackSession_SeekCompleted(MediaPlaybackSession sender, object args)
        {
            _dispatcherQueue.TryEnqueue(() => 
            {
                session.SeekCompleted -= PlaybackSession_SeekCompleted;
                NowVideoPositionChanging = false;
            });
        }
    }



    public bool NowVideoPositionChanging
    {
        get { return (bool)GetValue(NowVideoPositionChangingProperty); }
        set { SetValue(NowVideoPositionChangingProperty, value); }
    }

    // Using a DependencyProperty as the backing store for NowVideoPositionChanging.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty NowVideoPositionChangingProperty =
        DependencyProperty.Register("NowVideoPositionChanging", typeof(bool), typeof(DesktopPlayerUI), new PropertyMetadata(false));

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

    private void PlayPauseToggleKeyboardTrigger_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (_vm.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
        {
            _vm.MediaPlayer.Pause();
        }
        else if (_vm.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Paused)
        {
            _vm.MediaPlayer.Play();
        }
    }

    


    bool IsSamePlaybackRate(double value, double to)
    {
        return value == to;
    }

    bool IsSameQuality(NicoVideoQuality qualityValue, NicoVideoQuality qualityTo)
    {
        return qualityValue == qualityTo;
    }

    Visibility ToAvalirableQualityAsVisibility(IReadOnlyCollection<NicoVideoQualityEntity> qualities, NicoVideoQuality quality)
    {
        return qualities is null ? Visibility.Collapsed : qualities.Any(x => x.Quality == quality && x.IsAvailable).ToVisibility();
    }

    string ToQualityString(IReadOnlyCollection<NicoVideoQualityEntity> qualities, NicoVideoQuality quality)
    {
        var conv = new ToKMGTPEZYConverter();
        if (qualities?.FirstOrDefault(x => x.Quality == quality && x.IsAvailable) is { } qualityEntity)
        {
            return $"{quality.Translate()} {conv.Convert(qualityEntity.Bitrate!, typeof(string), null, Language)}bps";
        }
        else
        {
            return quality.Translate();
        }
    }

    private void ChangeQualityMenuFlyout_Opening(object sender, object e)
    {
        var flyout = (MenuFlyout)sender;
        foreach (var item in flyout.Items.Cast<ToggleMenuFlyoutItem>())
        {
            var qualityEntity = (NicoVideoQualityEntity)item.DataContext;
            item.IsChecked = qualityEntity.QualityId == _vm.CurrentQuality?.QualityId;
        }
    }    
}
