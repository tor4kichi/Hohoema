using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Player.Video.Cache;
using Hohoema.Models.Domain.Niconico.Video;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Presentation.Services;

namespace Hohoema.Presentation.ViewModels.Niconico.Video.Commands
{
    public sealed class CacheAddRequestCommand : VideoContentSelectionCommandBase
    {
        public CacheAddRequestCommand(
            VideoCacheManagerLegacy videoCacheManager,
            DialogService dialogService
            )
        {            
            VideoCacheManager = videoCacheManager;
            DialogService = dialogService;
        }

        public VideoCacheManagerLegacy VideoCacheManager { get; }
        public DialogService DialogService { get; }

        public NicoVideoQuality VideoQuality { get; set; } = NicoVideoQuality.Unknown;

        protected override void Execute(IVideoContent content)
        {
            var currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{currentMethod.DeclaringType.Name}#{currentMethod.Name}");

            VideoCacheManager.RequestCache(content.Id, VideoQuality);
        }
    }
}
