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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace NicoPlayerHohoema.Views
{
    public sealed partial class VideoPlayerPage : Page
    {
        public UINavigationButtons ShowUIUINavigationButtons =>
            UINavigationButtons.Cancel | UINavigationButtons.Accept | UINavigationButtons.Left | UINavigationButtons.Right | UINavigationButtons.Up | UINavigationButtons.Down;

        public TimeSpan ForwardSeekTime => TimeSpan.FromSeconds(30);
        public TimeSpan PreviewSeekTime => TimeSpan.FromSeconds(-10);




        public double MediaPlayerHeight
        {
            get { return (double)GetValue(MediaPlayerHeightProperty); }
            set { SetValue(MediaPlayerHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MediaPlayerHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MediaPlayerHeightProperty =
            DependencyProperty.Register("MediaPlayerHeight", typeof(double), typeof(VideoPlayerPage), new PropertyMetadata(0));




        public VideoPlayerPage()
        {
            this.InitializeComponent();
        }
    }
}
