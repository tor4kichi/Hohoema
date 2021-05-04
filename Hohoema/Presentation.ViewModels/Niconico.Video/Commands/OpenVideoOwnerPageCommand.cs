using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.UseCase.PageNavigation;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Pages.VideoListPage.Commands
{
    public sealed class OpenVideoOwnerPageCommand : DelegateCommandBase
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
                if (video.ProviderType == NicoVideoUserType.User)
                {
                    _pageManager.OpenPageWithId(Models.Domain.PageNavigation.HohoemaPageType.UserInfo, video.ProviderId);
                }
                else if (video.ProviderType == NicoVideoUserType.Channel)
                {
                    // TODO: チャンネル情報ページを開く
                }
            }
        }
    }
}
