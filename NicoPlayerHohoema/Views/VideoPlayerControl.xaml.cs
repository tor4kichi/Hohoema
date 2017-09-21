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
    public sealed partial class VideoPlayerControl : UserControl
    {
        public UINavigationButtons ShowUIUINavigationButtons => 
            UINavigationButtons.Accept | UINavigationButtons.Left | UINavigationButtons.Right | UINavigationButtons.Up | UINavigationButtons.Down;

        public VideoPlayerControl()
        {
            this.InitializeComponent();
        }
    }


    public class PlayerSidePaneContentTemplateSelecter : DataTemplateSelector
    {
        public DataTemplate Playlist { get; set; }
        public DataTemplate Settings { get; set; }
        public DataTemplate Comments { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is ViewModels.PlayerSidePaneContent.PlaylistSidePaneContentViewModel)
            {
                return Playlist;
            }
            else if (item is ViewModels.PlayerSidePaneContent.CommentVideoInfoContentViewModel)
            {
                return Comments;
            }
            else if (item is ViewModels.PlayerSidePaneContent.SettingsVideoInfoContentViewModel)
            {
                return Settings;
            }

            return base.SelectTemplateCore(item, container);
        }
    }
}
