using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.UseCase;
using Hohoema.Presentation.ViewModels.VideoListPage;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Messaging;
using NiconicoToolkit.Video;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZLogger;

namespace Hohoema.Presentation.ViewModels.Niconico.Video.Commands
{
    public sealed class VideoPlayWithQueueCommand : CommandBase
    {
        private readonly ILogger<VideoPlayWithQueueCommand> _logger;
        private readonly IMessenger _messenger;

        public VideoPlayWithQueueCommand(ILoggerFactory loggerFactory, IMessenger messenger)
        {
            _logger = loggerFactory.CreateLogger<VideoPlayWithQueueCommand>();
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
                if (item is VideoListItemControlViewModel itemVM && (itemVM.VideoHiddenInfo != null || itemVM.IsSensitiveContent || itemVM.IsDeleted))
                {
                    return;
                }

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
                _logger.ZLogError(e, "video play faield");
            }
        }
    }
}
