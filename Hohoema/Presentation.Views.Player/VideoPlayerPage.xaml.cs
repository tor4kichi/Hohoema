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
using Reactive.Bindings.Extensions;
using Windows.UI;
using Microsoft.Toolkit.Uwp.Helpers;
using Hohoema.Models.Domain;
using Hohoema.Models.UseCase.NicoVideos.Player;
using Hohoema.Models.Domain.Application;
using System.Diagnostics;
using Uno.Disposables;
using System.Threading.Tasks;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Hohoema.Presentation.Views.Player
{
    public enum PlayerUIDisplayMode
    {
        Desktop,
        DesktopNallow,
    }

    public sealed partial class VideoPlayerPage : Page
    {
        public VideoPlayerPage()
        {
            this.InitializeComponent();
            
            MediaControl.SizeChanged += MediaControl_SizeChanged;

            Loaded += VideoPlayerPage_Loaded;
        }

        private void VideoPlayerPage_Loaded(object sender, RoutedEventArgs e)
        {
            var width = MediaControlWidth;
            MediaControlWidth = width + 0.0;
        }

        public PlayerUIDisplayMode PlayerUIDisplayMode
        {
            get { return (PlayerUIDisplayMode)GetValue(PlayerUIDisplayModeProperty); }
            set { SetValue(PlayerUIDisplayModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PlayerUIDisplayMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PlayerUIDisplayModeProperty =
            DependencyProperty.Register("PlayerUIDisplayMode", typeof(PlayerUIDisplayMode), typeof(VideoPlayerPage), new PropertyMetadata(PlayerUIDisplayMode.Desktop, OnPlayerUIDisplayModePropertyChanged));

        private static void OnPlayerUIDisplayModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            
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
            DependencyProperty.Register("MediaControlWidth", typeof(double), typeof(VideoPlayerPage), new PropertyMetadata(Window.Current.CoreWindow.Bounds.Width));


        private async void MediaControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            await Task.Delay(100);

            MediaControlWidth = e.NewSize.Width;
        }
    }
}
