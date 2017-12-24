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

        public VideoPlayerPage()
        {
            this.InitializeComponent();
        }
    }


    public class PlayerSidePaneContentTemplateSelecter : DataTemplateSelector
    {
        public DataTemplate Empty { get; set; }
        public DataTemplate Playlist { get; set; }
        public DataTemplate Settings { get; set; }
        public DataTemplate Comments { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is ViewModels.PlayerSidePaneContent.PlaylistSidePaneContentViewModel)
            {
                return Playlist;
            }
            else if (item is ViewModels.PlayerSidePaneContent.CommentSidePaneContentViewModel)
            {
                return Comments;
            }
            else if (item is ViewModels.PlayerSidePaneContent.SettingsSidePaneContentViewModel)
            {
                return Settings;
            }
            else
            {
                return Empty;
            }

            return base.SelectTemplateCore(item, container);
        }
    }
}
