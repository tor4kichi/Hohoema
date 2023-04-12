#nullable enable
using Hohoema.Models.Niconico.Video;
using NiconicoToolkit.Video;

namespace Hohoema.ViewModels.Pages.VideoListPage.Commands;

public sealed class OpenVideoOwnerPageCommand : CommandBase
{
    private readonly PageManager _pageManager;

    public OpenVideoOwnerPageCommand(PageManager pageManager)
    {
        _pageManager = pageManager;
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
                _pageManager.OpenPageWithId(Models.PageNavigation.HohoemaPageType.UserInfo, video.ProviderId);
            }
            else if (video.ProviderType == OwnerType.Channel)
            {
                // TODO: チャンネル情報ページを開く
            }
        }
    }
}
