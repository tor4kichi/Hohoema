#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Niconico.Video;
using NiconicoToolkit.Video;

namespace Hohoema.ViewModels.Pages.VideoListPage.Commands;

public sealed class OpenVideoOwnerPageCommand : CommandBase
{
    private readonly IMessenger _messenger;

    public OpenVideoOwnerPageCommand(IMessenger messenger)
    {
        _messenger = messenger;
    }

    protected override bool CanExecute(object parameter)
    {
        return parameter is IVideoContentProvider;
    }

    protected override void Execute(object parameter)
    {
        if (parameter is IVideoContentProvider video)
        {
            if (video.ProviderType == OwnerType.User)
            {
                _ = _messenger.OpenPageWithIdAsync(Models.PageNavigation.HohoemaPageType.UserInfo, video.ProviderId);
            }
            else if (video.ProviderType == OwnerType.Channel)
            {
                // TODO: チャンネル情報ページを開く
            }
        }
    }
}
