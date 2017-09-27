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
    public sealed partial class HohoemaSecondaryViewFrame : UserControl
    {
        public Frame Frame
        {
            get { return ContentFrame; }
        }

        public HohoemaSecondaryViewFrame()
        {
            this.InitializeComponent();
        }
    }


    public sealed class PlayerDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Empty { get; set; }
        public DataTemplate VideoPlayer { get; set; }
        public DataTemplate LivePlayer { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is ViewModels.VideoPlayerPageViewModel)
            {
                return VideoPlayer;
            }
            else if (item is ViewModels.LivePlayerPageViewModel)
            {
                return LivePlayer;
            }
            else
            {
                return Empty;
            }

//            return base.SelectTemplateCore(item, container);
        }
    }
}
