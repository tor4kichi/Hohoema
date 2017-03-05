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

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is ViewModels.VideoPlayerControlViewModel)
            {
                return VideoPlayer;
            }
            else if (item is ViewModels.LiveVideoPlayerControlViewModel)
            {
                return LiveVideoPlayer;
            }

            if (Empty != null)
            {
                return Empty;
            }


            return base.SelectTemplateCore(item, container);
        }
    }

}
