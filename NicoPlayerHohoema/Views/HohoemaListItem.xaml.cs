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

namespace NicoPlayerHohoema.Views.Controls
{
    public sealed partial class HohoemaListItem : UserControl
    {
        public HohoemaListDisplayType DisplayType { get; set; } = HohoemaListDisplayType.Video;

        public bool IsVideo => DisplayType == HohoemaListDisplayType.Video;
        public bool IsMiniCard => DisplayType == HohoemaListDisplayType.MiniCard;
        public bool IsCard => DisplayType == HohoemaListDisplayType.Card;

        public HohoemaListItem()
        {
            this.InitializeComponent();
        }
    }

    public enum HohoemaListDisplayType
    {
        Video,
        MiniCard,
        Card,

    }
}
