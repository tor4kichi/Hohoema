using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Prism.Ioc;
using Hohoema.Presentation.ViewModels;
using Microsoft.Toolkit.Uwp.Helpers;
using Hohoema.Models.Domain;
using Reactive.Bindings.Extensions;
using NiconicoLiveToolkit.Live.WatchSession;
using Hohoema.Models.Domain.Application;
using Uno.Disposables;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Hohoema.Presentation.Views.Player
{
    public sealed partial class LivePlayerPage : Page
    {
        public TimeSpan ForwardSeekTime => TimeSpan.FromSeconds(30);
        public TimeSpan PreviewSeekTime => TimeSpan.FromSeconds(-10);

        CompositeDisposable _compositeDisposable;

        public LivePlayerPage()
        {
            this.InitializeComponent();

            _mediaPlayer = App.Current.Container.Resolve<MediaPlayer>();
            _mediaPlayer.VolumeChanged += OnMediaPlayerVolumeChanged;
            VolumeSlider.Value = _mediaPlayer.Volume;
            VolumeSlider.ValueChanged += VolumeSlider_ValueChanged;

            _UIdispatcher = Dispatcher;

            
            Loaded += LivePlayerPage_Loaded;
            Unloaded += LivePlayerPage_Unloaded;
        }

 
        private void LivePlayerPage_Loaded(object sender, RoutedEventArgs e)
        {
            _compositeDisposable = new CompositeDisposable();
            var appearanceSettings = App.Current.Container.Resolve<AppearanceSettings>();
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
}
