using Hohoema.Interfaces;
using Hohoema.Models;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Repository;
using Hohoema.UseCase.VideoCache;
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
            VideoCacheManager videoCacheManager
            )
        {
            VideoCacheManager = videoCacheManager;
        }

        public VideoCacheManager VideoCacheManager { get; }

        public NicoVideoQuality VideoQuality { get; set; } = NicoVideoQuality.Unknown;

        protected override bool CanExecute(object parameter)
        {
            return parameter is IVideoContent;
        }

        protected override void Execute(IVideoContent content)
        {
            VideoCacheManager.RequestCache(content.Id, VideoQuality);
        }
    }
}
