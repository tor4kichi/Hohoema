using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Niconico.Live;
using Hohoema.Models.Niconico.Mylist;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.PageNavigation;
using NiconicoToolkit.Live;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Navigation.Commands;
public sealed partial class OpenContentOwnerPageCommand : CommandBase
{
    private readonly IMessenger _messenger;

    public OpenContentOwnerPageCommand(IMessenger messenger)
    {
        _messenger = messenger;
    }

    protected override bool CanExecute(object parameter)
    {
        return true;
    }

    protected override void Execute(object parameter)
    {
        switch (parameter)
        {
            case IVideoContentProvider videoContent:
                if (videoContent.ProviderType == OwnerType.User)
                {
                    var p = new NavigationParameters();
                    p.Add("id", videoContent.ProviderId);
                    _ = _messenger.OpenPageAsync(HohoemaPageType.UserInfo, p);
                }
                else if (videoContent.ProviderType == OwnerType.Channel)
                {
                    var p = new NavigationParameters();
                    p.Add("id", videoContent.ProviderId);
                    _ = _messenger.OpenPageAsync(HohoemaPageType.ChannelVideo, p);
                }

                break;
            case ILiveContentProvider liveContent:
                if (liveContent.ProviderType == ProviderType.Community)
                {
                    var p = new NavigationParameters();
                    p.Add("id", liveContent.ProviderId);
                    _ = _messenger.OpenPageAsync(HohoemaPageType.Community, p);
                }
                break;
            case IMylist mylist:
                {
                    _ = _messenger.OpenPageWithIdAsync(HohoemaPageType.Mylist, mylist.PlaylistId.Id);
                    break;

                }
        }
    }
}
