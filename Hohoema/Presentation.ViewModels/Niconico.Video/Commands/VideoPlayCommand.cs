using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.UseCase;
using Microsoft.Toolkit.Mvvm.Messaging;
using NiconicoToolkit.Video;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Niconico.Video.Commands
{
    public sealed class VideoPlayWithQueueCommand : DelegateCommandBase
    {
        private readonly IMessenger _messenger;

        public VideoPlayWithQueueCommand(IMessenger messenger)
        {
            _messenger = messenger;
        }

        protected override bool CanExecute(object item)
        {
            return item is string or IVideoContent or VideoId;
        }

        protected override void Execute(object item)
        {
            try
            {
                if (item is string contentId)
                {
                    _messenger.Send(VideoPlayRequestMessage.PlayVideo(contentId));
                }
                else if (item is VideoId videoId)
                {
                    _messenger.Send(VideoPlayRequestMessage.PlayVideo(videoId));
                }
                else if (item is IVideoContent videoContent)
                {
                    _messenger.Send(VideoPlayRequestMessage.PlayVideo(videoContent.VideoId));
                }
            }
            catch (Exception e)
            {
                ErrorTrackingManager.TrackError(e);
            }
        }
    }
}
