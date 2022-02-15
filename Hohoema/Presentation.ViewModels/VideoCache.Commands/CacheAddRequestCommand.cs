using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Niconico.Video;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Presentation.Services;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;
using Hohoema.Models.Domain.VideoCache;

namespace Hohoema.Presentation.ViewModels.VideoCache.Commands
{
    public sealed class CacheAddRequestCommand : VideoContentSelectionCommandBase
    {
        private readonly VideoCacheManager _videoCacheManager;
        private readonly DialogService _dialogService;

        public CacheAddRequestCommand(
            VideoCacheManager videoCacheManager,
            DialogService dialogService
            )
        {
            _videoCacheManager = videoCacheManager;
            _dialogService = dialogService;
        }

        public NicoVideoQuality VideoQuality { get; set; } = NicoVideoQuality.Unknown;

        protected override void Execute(IVideoContent content)
        {
            var currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
            //Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{currentMethod.DeclaringType.Name}#{currentMethod.Name}");

            _ = _videoCacheManager.PushCacheRequestAsync(content.VideoId, VideoQuality);
        }
    }
}
