#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Niconico.Live;
using Hohoema.Models.PageNavigation;

namespace Hohoema.ViewModels.Niconico.Live;

public sealed class OpenBroadcasterInfoCommand : CommandBase
{
    private readonly IMessenger _messenger;

    public OpenBroadcasterInfoCommand(    
        IMessenger messenger
        )
    {
        _messenger = messenger;
    }

    protected override bool CanExecute(object parameter)
    {
        return parameter is ILiveContentProvider liveContent
            && !string.IsNullOrEmpty(liveContent.ProviderId);
    }

    protected override void Execute(object parameter)
    {
        if (parameter is ILiveContentProvider content)
        {
            if (!string.IsNullOrEmpty(content.ProviderId))
            {
                _ = _messenger.OpenPageWithIdAsync(HohoemaPageType.Community, content.ProviderId);
            }
        }
    }
}
