using Hohoema.Models;
using Hohoema.Models.Niconico.Video;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Services;
using Hohoema.ViewModels.Niconico.Video.Commands;
using Hohoema.Models.VideoCache;

namespace Hohoema.ViewModels.VideoCache.Commands
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
            _ = _videoCacheManager.PushCacheRequestAsync(content.VideoId, VideoQuality);
        }
    }
}
