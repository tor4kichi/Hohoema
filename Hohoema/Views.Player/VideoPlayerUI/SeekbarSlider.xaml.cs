using System;
using Windows.Media.Playback;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace Hohoema.Views.Player.VideoPlayerUI;

public sealed partial class SeekbarSlider : UserControl
{
    private readonly DispatcherQueue _dispatcherQueue;
    public SeekbarSlider()
    {
        this.InitializeComponent();

        SeekBarSlider.ManipulationMode = ManipulationModes.TranslateX;
        SeekBarSlider.ManipulationStarting += SeekBarSlider_ManipulationStarting;
        SeekBarSlider.ManipulationStarted += SeekBarSlider_ManipulationStarted;

        SeekBarSlider.FocusEngaged += SeekBarSlider_FocusEngaged;
        SeekBarSlider.FocusDisengaged += SeekBarSlider_FocusDisengaged;

        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        Loaded += SeekbarSlider_Loaded;
        Unloaded += SeekbarSlider_Unloaded;
    }

    private void SeekbarSlider_Loaded(object sender, RoutedEventArgs e)
    {
        MediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
    }

    private void SeekbarSlider_Unloaded(object sender, RoutedEventArgs e)
    {
        MediaPlayer.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;

        Window.Current.CoreWindow.PointerReleased -= CoreWindow_PointerReleased;
    }




    public MediaPlayer MediaPlayer
    {
        get { return (MediaPlayer)GetValue(MediaPlayerProperty); }
        set { SetValue(MediaPlayerProperty, value); }
    }

    // Using a DependencyProperty as the backing store for MediaPlayer.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty MediaPlayerProperty =
        DependencyProperty.Register("MediaPlayer", typeof(MediaPlayer), typeof(SeekbarSlider), new PropertyMetadata(null));




    public TimeSpan VideoLength
    {
        get { return (TimeSpan)GetValue(VideoLengthProperty); }
        set { SetValue(VideoLengthProperty, value); }
    }

    // Using a DependencyProperty as the backing store for VideoLength.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty VideoLengthProperty =
        DependencyProperty.Register("VideoLength", typeof(TimeSpan), typeof(SeekbarSlider), new PropertyMetadata(TimeSpan.Zero));




    public TimeSpan VideoPosition
    {
        get { return (TimeSpan)GetValue(VideoPositionProperty); }
        set { SetValue(VideoPositionProperty, value); }
    }

    // Using a DependencyProperty as the backing store for VideoPositionSeconds.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty VideoPositionProperty =
        DependencyProperty.Register("VideoPosition", typeof(TimeSpan), typeof(SeekbarSlider), new PropertyMetadata(TimeSpan.Zero));




    public bool NowVideoPositionChanging
    {
        get { return (bool)GetValue(NowVideoPositionChangingProperty); }
        set { SetValue(NowVideoPositionChangingProperty, value); }
    }

    // Using a DependencyProperty as the backing store for NowVideoPositionChanging.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty NowVideoPositionChangingProperty =
        DependencyProperty.Register("NowVideoPositionChanging", typeof(bool), typeof(SeekbarSlider), new PropertyMetadata(false));

    void RefrectSliderPositionToPlaybackPosition()
    {
        MediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(SeekBarSlider.Value);
    }

    private void SeekBarSlider_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
    {
        e.Complete();
    }

    private void SeekBarSlider_ManipulationStarting(object sender, ManipulationStartingRoutedEventArgs e)
    {
        NowVideoPositionChanging = true;

        Window.Current.CoreWindow.PointerReleased -= CoreWindow_PointerReleased;
        Window.Current.CoreWindow.PointerReleased += CoreWindow_PointerReleased;
    }

    private void CoreWindow_PointerReleased(CoreWindow sender, PointerEventArgs args)
    {
        Window.Current.CoreWindow.PointerReleased -= CoreWindow_PointerReleased;

        NowVideoPositionChanging = false;

        RefrectSliderPositionToPlaybackPosition();
    }

    private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
    {
        _ = _dispatcherQueue.TryEnqueue(() =>
        {
            VideoPosition = sender.Position;
            if (!this.NowVideoPositionChanging)
            {
                SeekBarSlider.Value = VideoPosition.TotalSeconds;
            }
        });
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
}
