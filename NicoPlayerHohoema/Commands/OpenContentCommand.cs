using NicoPlayerHohoema.Models;
using Prism.Commands;
using NicoPlayerHohoema.Services.Page;

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
            bool isPlayerShowWithSmallMode = true;
            if (parameter is Interfaces.IVideoContent)
            {
                var content = parameter as Interfaces.IVideoContent;

                var player = HohoemaCommnadHelper.GetHohoemaPlaylist();
                player.PlayVideo(content);

                isPlayerShowWithSmallMode = false;
            }
            else if (parameter is Interfaces.ILiveContent)
            {
                var content = parameter as Interfaces.ILiveContent;

                var player = HohoemaCommnadHelper.GetHohoemaPlaylist();
                player.PlayLiveVideo(content);

                isPlayerShowWithSmallMode = false;
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
            else if (parameter is Interfaces.ISearchHistory)
            {
                var history = parameter as Interfaces.ISearchHistory;                
                var pageManager = HohoemaCommnadHelper.GetPageManager();
                pageManager.Search(SearchPagePayloadContentHelper.CreateDefault(history.Target, history.Keyword));
            }
            else if (parameter is Interfaces.IChannel)
            {
                var channel = parameter as Interfaces.IChannel;
                var pageManager = HohoemaCommnadHelper.GetPageManager();
                pageManager.OpenPage(HohoemaPageType.ChannelVideo, channel.Id);
            }


            if (isPlayerShowWithSmallMode)
            {
                var playlist = HohoemaCommnadHelper.GetHohoemaPlaylist();
                if (playlist.IsDisplayMainViewPlayer)
                {
                    playlist.PlayerDisplayType = PlayerDisplayType.PrimaryWithSmall;
                }
            }
            // TODO: マイリストやユーザーIDを開けるように

        }
    }
}
