using NicoPlayerHohoema.Helpers;
using NicoPlayerHohoema.Models;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NicoPlayerHohoema.Commands
{
    public sealed class OpenContentCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            //            return parameter is Interfaces.INiconicoContent || parameter is Interfaces.ISearchWithtag;
            return true;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is Interfaces.IVideoContent)
            {
                var content = parameter as Interfaces.IVideoContent;

                var player = HohoemaCommnadHelper.GetHohoemaPlaylist();
                player.PlayVideo(content);
            }
            else if (parameter is Interfaces.ILiveContent)
            {
                var content = parameter as Interfaces.ILiveContent;

                var player = HohoemaCommnadHelper.GetHohoemaPlaylist();
                player.PlayLiveVideo(content);
            }
            else if (parameter is Interfaces.ICommunity)
            {
                var content = parameter as Interfaces.ICommunity;
                var pageManager = HohoemaCommnadHelper.GetPageManager();
                pageManager.OpenPage(Models.HohoemaPageType.Community, content.Id);
            }
            else if (parameter is Interfaces.IMylist)
            {
                var content = parameter as Interfaces.IMylist;
                var pageManager = HohoemaCommnadHelper.GetPageManager();
                pageManager.OpenPage(Models.HohoemaPageType.Mylist, new Models.MylistPagePayload(content.Id).ToParameterString());
            }
            else if (parameter is Interfaces.IUser)
            {
                var content = parameter as Interfaces.IUser;
                var pageManager = HohoemaCommnadHelper.GetPageManager();
                pageManager.OpenPage(Models.HohoemaPageType.UserInfo, content.Id);
            }
            else if (parameter is Interfaces.ISearchWithtag)
            {
                var content = parameter as Interfaces.ISearchWithtag;
                var pageManager = HohoemaCommnadHelper.GetPageManager();
                pageManager.Search(SearchPagePayloadContentHelper.CreateDefault(SearchTarget.Tag, content.Tag));
            }
            else if (parameter is Interfaces.IFeedGroup)
            {
                var content = parameter as Interfaces.IFeedGroup;
                var pageManager = HohoemaCommnadHelper.GetPageManager();
                pageManager.OpenPage(Models.HohoemaPageType.FeedVideoList, content.Id);
            }
            else if (parameter is ViewModels.SearchHistoryListItem)
            {
                var historyVM = parameter as ViewModels.SearchHistoryListItem;
                historyVM.PrimaryCommand.Execute(null);
//                var content = parameter as Interfaces.ISearchHistory;
//                var pageManager = HohoemaCommnadHelper.GetPageManager();
//                pageManager.Search(SearchPagePayloadContentHelper.CreateDefault(SearchTarget.Tag, content.Tag));
            }

            // TODO: マイリストやユーザーIDを開けるように

        }
    }
}
