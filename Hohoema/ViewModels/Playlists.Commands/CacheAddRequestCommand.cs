using Hohoema.Interfaces;
using Hohoema.Models;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.UseCase.Playlist.Commands
{
    public sealed class CacheAddRequestCommand : VideoContentSelectionCommandBase
    {
        public CacheAddRequestCommand(
            Models.Cache.VideoCacheManager videoCacheManager,
            Services.DialogService dialogService
            )
        {
            VideoCacheManager = videoCacheManager;
            DialogService = dialogService;
        }

        public Models.Cache.VideoCacheManager VideoCacheManager { get; }
        public Services.DialogService DialogService { get; }

        public NicoVideoQuality VideoQuality { get; set; } = NicoVideoQuality.Unknown;

        protected override bool CanExecute(object parameter)
        {
            return parameter is Interfaces.IVideoContent;
        }

        protected override void Execute(IVideoContent content)
        {
            VideoCacheManager.RequestCache(content.Id, VideoQuality);
        }
    }
}
