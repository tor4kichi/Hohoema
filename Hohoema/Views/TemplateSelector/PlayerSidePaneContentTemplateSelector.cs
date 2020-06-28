using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Hohoema.Views.TemplateSelector
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
            if (item is ViewModels.PlayerSidePaneContent.LiveCommentSidePaneContentViewModel)
            {
                return Comments;
            }
            else if (item is ViewModels.PlayerSidePaneContent.EmptySidePaneContentViewModel)
            {
                return Empty;
            }
            else if (item is ViewModels.PlayerSidePaneContent.SettingsSidePaneContentViewModel)
            {
                return Settings;
            }

            var sidePaneType = (PlayerSidePaneContentType?)item;
            if (sidePaneType == null) { return Empty; }

            switch (sidePaneType)
            {
                case PlayerSidePaneContentType.Playlist:
                    return Playlist;
                case PlayerSidePaneContentType.Comment:
                    return Comments;
                case PlayerSidePaneContentType.Setting:
                    return Settings;
                case PlayerSidePaneContentType.RelatedVideos:
                    return RelatedVideos;
                default:
                    break;
            }

            return Empty;
        }
    }
}
