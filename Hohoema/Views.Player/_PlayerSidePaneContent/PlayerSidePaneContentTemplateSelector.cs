using Hohoema.Views.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Hohoema.Views.Player
{
    public class PlayerSidePaneContentTemplateSelecter : DataTemplateSelector
    {
        public Windows.UI.Xaml.DataTemplate Empty { get; set; }
        public Windows.UI.Xaml.DataTemplate Playlist { get; set; }
        public Windows.UI.Xaml.DataTemplate Settings { get; set; }
        public Windows.UI.Xaml.DataTemplate Comments { get; set; }
        public Windows.UI.Xaml.DataTemplate RelatedVideos { get; set; }

        protected override Windows.UI.Xaml.DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return item switch
            {
                ViewModels.Player.PlayerSidePaneContent.VideoCommentSidePaneContentViewModel => Comments,
                ViewModels.Player.PlayerSidePaneContent.LiveCommentsSidePaneContentViewModel => Comments,
                ViewModels.Player.PlayerSidePaneContent.SettingsSidePaneContentViewModel => Settings,
                ViewModels.Player.PlayerSidePaneContent.RelatedVideosSidePaneContentViewModel => RelatedVideos,
                ViewModels.Player.PlayerSidePaneContent.PlaylistSidePaneContentViewModel => Playlist,
                _ => Empty,
            };
        }
    }
}
