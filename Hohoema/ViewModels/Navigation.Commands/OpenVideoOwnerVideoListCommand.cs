using Hohoema.Models.Niconico.Video;
using Hohoema.Contracts.Services.Navigations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Services.Navigations;

namespace Hohoema.ViewModels.Navigation.Commands
{
    public sealed class OpenVideoOwnerVideoListCommand : CommandBase
    {
        private readonly PageManager _pageManager;
        private readonly NicoVideoProvider _nicoVideoProvider;

        public OpenVideoOwnerVideoListCommand(
            PageManager pageManager,
            NicoVideoProvider nicoVideoProvider
            )
        {
            _pageManager = pageManager;
            _nicoVideoProvider = nicoVideoProvider;
        }

        protected override bool CanExecute(object parameter)
        {
            return parameter is IVideoContent or IVideoContentProvider;
        }

        protected override async void Execute(object parameter)
        {
            try
            {
                if (parameter is IVideoContentProvider provider && provider.ProviderType is not NiconicoToolkit.Video.OwnerType.Hidden && provider.ProviderId is not null)
                {
                    if (provider.ProviderType is NiconicoToolkit.Video.OwnerType.User)
                    {
                        _pageManager.OpenPageWithId(Models.PageNavigation.HohoemaPageType.UserVideo, provider.ProviderId);
                    }
                    else if (provider.ProviderType is NiconicoToolkit.Video.OwnerType.Channel)
                    {
                        _pageManager.OpenPageWithId(Models.PageNavigation.HohoemaPageType.ChannelVideo, provider.ProviderId);
                    }
                    return;
                }
            }
            catch { }

            if (parameter is IVideoContent content)
            {
                var video = await _nicoVideoProvider.GetCachedVideoInfoAsync(content.VideoId);
                if (video.ProviderType is NiconicoToolkit.Video.OwnerType.User)
                {
                    _pageManager.OpenPageWithId(Models.PageNavigation.HohoemaPageType.UserVideo, video.ProviderId);
                }
                else if (video.ProviderType is NiconicoToolkit.Video.OwnerType.Channel)
                {
                    _pageManager.OpenPageWithId(Models.PageNavigation.HohoemaPageType.ChannelVideo, video.ProviderId);
                }
            }

        }
    }
}
