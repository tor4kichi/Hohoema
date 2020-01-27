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
using NicoPlayerHohoema.ViewModels;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace NicoPlayerHohoema.Views
{
    public sealed partial class LivePlayerPage : Page
    {
        public TimeSpan ForwardSeekTime => TimeSpan.FromSeconds(30);
        public TimeSpan PreviewSeekTime => TimeSpan.FromSeconds(-10);

        public LivePlayerPage()
        {
            this.InitializeComponent();

            _mediaPlayer = App.Current.Container.Resolve<MediaPlayer>();
            _mediaPlayer.VolumeChanged += OnMediaPlayerVolumeChanged;
            VolumeSlider.Value = _mediaPlayer.Volume;
            VolumeSlider.ValueChanged += VolumeSlider_ValueChanged;

            _UIdispatcher = Dispatcher;
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




        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var qualityStr = (string)comboBox.SelectedValue;
            var viewModel = DataContext as LivePlayerPageViewModel;
            if (viewModel.ChangeQualityCommand.CanExecute(qualityStr))
            {
                viewModel.ChangeQualityCommand.Execute(qualityStr);
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
