using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NicoPlayerHohoema.Views.TemplateSelector
{
    public class HohoemaFlyoutTemplateSelector : DataTemplateSelector
    {
        public DataTemplate VideoFlyoutTemplate { get; set; }
        public DataTemplate LiveFlyoutTemplate { get; set; }
        public DataTemplate MylistFlyoutTemplate { get; set; }
        public DataTemplate UserFlyoutTemplate { get; set; }
        public DataTemplate CommunityFlyoutTemplate { get; set; }
        public DataTemplate FeedGroupFlyoutTemplate { get; set; }
        public DataTemplate FeedSourceFlyoutTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is ViewModels.NicoRepoTimelineVM)
            {
                var nicoRepoVM = (item as ViewModels.NicoRepoTimelineVM);
                switch (nicoRepoVM.ItemTopic)
                {
                    case ViewModels.NicoRepoItemTopic.Unknown:
                        break;
                    case ViewModels.NicoRepoItemTopic.NicoVideo_User_Video_Kiriban_Play:
                        return VideoFlyoutTemplate;
                    case ViewModels.NicoRepoItemTopic.NicoVideo_User_Video_Upload:
                        return VideoFlyoutTemplate;
                    case ViewModels.NicoRepoItemTopic.NicoVideo_Community_Level_Raise:
                        break;
                    case ViewModels.NicoRepoItemTopic.NicoVideo_User_Mylist_Add_Video:
                        return VideoFlyoutTemplate;
                    case ViewModels.NicoRepoItemTopic.NicoVideo_User_Community_Video_Add:
                        return VideoFlyoutTemplate;
                    case ViewModels.NicoRepoItemTopic.NicoVideo_User_Video_UpdateHighestRankings:
                        break;
                    case ViewModels.NicoRepoItemTopic.NicoVideo_User_Video_Advertise:
                        break;
                    case ViewModels.NicoRepoItemTopic.NicoVideo_Channel_Blomaga_Upload:
                        break;
                    case ViewModels.NicoRepoItemTopic.Live_User_Program_OnAirs:
                        return LiveFlyoutTemplate;
                    case ViewModels.NicoRepoItemTopic.Live_User_Program_Reserve:
                        return LiveFlyoutTemplate;
                    case ViewModels.NicoRepoItemTopic.Live_Channel_Program_Onairs:
                        return LiveFlyoutTemplate;
                    case ViewModels.NicoRepoItemTopic.Live_Channel_Program_Reserve:
                        return LiveFlyoutTemplate;
                    default:
                        break;
                }
            }
            else if (item is ViewModels.VideoInfoControlViewModel)
            {
                return VideoFlyoutTemplate;
            }
            else if (item is ViewModels.LiveInfoViewModel)
            {
                return LiveFlyoutTemplate;
            }
            else if (item is ViewModels.FeedGroupListItem)
            {
                return FeedGroupFlyoutTemplate;
            }
            else if (item is ViewModels.FeedSourceBookmark)
            {
                return FeedSourceFlyoutTemplate;
            }

            return base.SelectTemplateCore(item, container);
        }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is ViewModels.VideoInfoControlViewModel)
            {
                return VideoFlyoutTemplate;
            }

            return base.SelectTemplateCore(item);
        }
    }
}
