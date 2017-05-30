using NicoPlayerHohoema.Models;
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
using Microsoft.Practices.Unity;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace NicoPlayerHohoema.Views
{
    public sealed partial class PlayerWithPageContainer : ContentControl
    {
        public PlayerWithPageContainer()
        {
            this.InitializeComponent();
        }
    }

    public sealed class PlayerContentTemplateSelecter : DataTemplateSelector
    {
        public DataTemplate Empty { get; set; }
        public DataTemplate VideoPlayer { get; set; }
        public DataTemplate LiveVideoPlayer { get; set; }
        public DataTemplate VideoPlayer_TV { get; set; }
        public DataTemplate LiveVideoPlayer_TV { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            var hohoema = App.Current.Container.Resolve<HohoemaApp>();
            var tvMode = hohoema.UserSettings.AppearanceSettings.IsForceTVModeEnable || Util.DeviceTypeHelper.IsXbox;

            if (item is ViewModels.VideoPlayerControlViewModel)
            {
                if (VideoPlayer_TV == null) { return VideoPlayer; }
                return tvMode ? VideoPlayer_TV : VideoPlayer;
            }
            else if (item is ViewModels.LiveVideoPlayerControlViewModel)
            {
                if (LiveVideoPlayer_TV == null) { return LiveVideoPlayer; }
                return tvMode ? LiveVideoPlayer_TV : LiveVideoPlayer;
            }

            if (Empty != null)
            {
                return Empty;
            }


            return base.SelectTemplateCore(item, container);
        }
    }

}
