using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Channel;
using Hohoema.Models.Niconico.Live;
using Hohoema.Models.Niconico.Mylist;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.PageNavigation;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Navigation.Commands;
public sealed class OpenVideoListPageCommand : CommandBase
{
    private readonly IMessenger _messenger;

    public OpenVideoListPageCommand(IMessenger messenger) 
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
            case string s:
                _ = _messenger.OpenVideoListPageAsync(s);
                break;

            case IVideoContentProvider videoContent:
                _ = _messenger.OpenVideoListPageAsync(videoContent);
                break;
            case Models.Niconico.Community.ICommunity communityContent:
                _ = _messenger.OpenVideoListPageAsync(communityContent);
                break;
            case IMylist mylistContent:
                _ = _messenger.OpenVideoListPageAsync(mylistContent);
                break;
            case IUser user:
                _ = _messenger.OpenVideoListPageAsync(user);
                break;
            case ITag tag:
                _ = _messenger.OpenVideoListPageAsync(tag);
                break;
            case ISearchHistory history:
                _ = _messenger.OpenVideoListPageAsync(history);
                break;
            case IChannel channel:
                _ = _messenger.OpenVideoListPageAsync(channel);
                break;
        }
    }
}
