using Hohoema.Models.Niconico.Video;
using Hohoema.Services.PageNavigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Navigation.Commands
{
    public sealed class OpenVideoOwnerSeriesListCommand : CommandBase
    {
        private readonly PageManager _pageManager;
        private readonly NicoVideoProvider _nicoVideoProvider;

        public OpenVideoOwnerSeriesListCommand(
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
                if (parameter is IVideoContentProvider provider && provider.ProviderType is NiconicoToolkit.Video.OwnerType.User && provider.ProviderId is not null)
                {
                    _pageManager.OpenPageWithId(Models.PageNavigation.HohoemaPageType.UserSeries, provider.ProviderId);
                    return;
                }
            }
            catch { }

            if (parameter is IVideoContent content)
            {
                var video = await _nicoVideoProvider.GetCachedVideoInfoAsync(content.VideoId);
                if (video.ProviderType is NiconicoToolkit.Video.OwnerType.User)
                {
                    _pageManager.OpenPageWithId(Models.PageNavigation.HohoemaPageType.UserSeries, video.ProviderId);
                }
            }
        }
    }
}
