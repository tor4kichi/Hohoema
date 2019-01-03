using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NicoPlayerHohoema.Views.TemplateSelector
{
    public class PlayerSidePaneContentTemplateSelecter : DataTemplateSelector
    {
        public DataTemplate Empty { get; set; }
        public DataTemplate Playlist { get; set; }
        public DataTemplate Settings { get; set; }
        public DataTemplate Comments { get; set; }
        public DataTemplate RelatedVideos { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is ViewModels.PlayerSidePaneContent.PlaylistSidePaneContentViewModel)
            {
                return Playlist;
            }
            else if (item is ViewModels.PlayerSidePaneContent.LiveCommentSidePaneContentViewModel)
            {
                return Comments;
            }
            else if (item is ViewModels.PlayerSidePaneContent.SettingsSidePaneContentViewModel)
            {
                return Settings;
            }
            else if (item is ViewModels.PlayerSidePaneContent.RelatedVideosSidePaneContentViewModel)
            {
                return RelatedVideos;
            }
            else
            {
                return Empty;
            }
        }
    }
}
