using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Commands.Cache
{
    public sealed class AddCacheRequestCommand : DelegateCommandBase
    {
        public AddCacheRequestCommand(
            Models.Cache.VideoCacheManager videoCacheManager,
            Services.DialogService dialogService
            )
        {
            VideoCacheManager = videoCacheManager;
            DialogService = dialogService;
        }

        public Models.Cache.VideoCacheManager VideoCacheManager { get; }
        public Services.DialogService DialogService { get; }

        protected override bool CanExecute(object parameter)
        {
            return parameter is Interfaces.IVideoContent;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is Interfaces.IVideoContent content)
            {
                VideoCacheManager.RequestCache(content.Id);
            }
        }
    }
}
