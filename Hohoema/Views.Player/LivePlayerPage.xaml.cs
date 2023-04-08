using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.Models.Application;
using Hohoema.ViewModels.Player;
using Microsoft.Toolkit.Uwp.Helpers;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Disposables;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Hohoema.Views.Player;


public sealed partial class LivePlayerPage : Page
{
    public TimeSpan ForwardSeekTime => TimeSpan.FromSeconds(30);
    public TimeSpan PreviewSeekTime => TimeSpan.FromSeconds(-10);

    CompositeDisposable _compositeDisposable;


    public bool NowCommentEditting
    {
        get { return (bool)GetValue(NowCommentEdittingProperty); }
        set { SetValue(NowCommentEdittingProperty, value); }
    }

    // Using a DependencyProperty as the backing store for NowCommentEditting.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty NowCommentEdittingProperty =
        DependencyProperty.Register("NowCommentEditting", typeof(bool), typeof(LivePlayerPage), new PropertyMetadata(false));





    public LivePlayerPage()
    {
        this.InitializeComponent();

        _UIdispatcher = Dispatcher;
        DataContext = _vm = Ioc.Default.GetRequiredService<LivePlayerPageViewModel>();
        _mediaPlayer = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<MediaPlayer>();

        _mediaPlayer.VolumeChanged += OnMediaPlayerVolumeChanged;
        VolumeSlider.Value = _mediaPlayer.Volume;
        VolumeSlider.ValueChanged += VolumeSlider_ValueChanged;            
        Loaded += LivePlayerPage_Loaded;
        Unloaded += LivePlayerPage_Unloaded;
    }

    private readonly LivePlayerPageViewModel _vm;


    private void LivePlayerPage_Loaded(object sender, RoutedEventArgs e)
    {
        _compositeDisposable = new CompositeDisposable();
        var appearanceSettings = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<AppearanceSettings>();
        appearanceSettings.ObserveProperty(x => x.ApplicationTheme)
            .Subscribe(theme =>
            {
                ThemeChanged(theme);
            })
            .AddTo(_compositeDisposable);

    }

    private void LivePlayerPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _compositeDisposable.Dispose();
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

    public bool IsDisplayControlUI
    {
        get { return (bool)GetValue(IsDisplayControlUIProperty); }
        set { SetValue(IsDisplayControlUIProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsDisplayControlUI.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsDisplayControlUIProperty =
        DependencyProperty.Register("IsDisplayControlUI", typeof(bool), typeof(LivePlayerPage), new PropertyMetadata(true));



    public bool NowVideoPositionChanging
    {
        get { return (bool)GetValue(NowVideoPositionChangingProperty); }
        set { SetValue(NowVideoPositionChangingProperty, value); }
    }

    // Using a DependencyProperty as the backing store for NowVideoPositionChanging.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty NowVideoPositionChangingProperty =
        DependencyProperty.Register("NowVideoPositionChanging", typeof(bool), typeof(LivePlayerPage), new PropertyMetadata(false));




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




    public double MediaControlActualWidth
    {
        get { return (double)GetValue(MediaControlActualWidthProperty); }
        set { SetValue(MediaControlActualWidthProperty, value); }
    }

    // Using a DependencyProperty as the backing store for MediaControlActualWidth.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty MediaControlActualWidthProperty =
        DependencyProperty.Register("MediaControlActualWidth", typeof(double), typeof(LivePlayerPage), new PropertyMetadata(0.0));



    private void LayoutRoot_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        MediaControlActualWidth = e.NewSize.Width;
    }
}
