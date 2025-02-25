#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Playlist;
using Hohoema.ViewModels.VideoListPage;
using Microsoft.Extensions.Logging;
using NiconicoToolkit.Video;
using System;
using ZLogger;

namespace Hohoema.ViewModels.Niconico.Video.Commands;

public sealed partial class VideoPlayWithQueueCommand : CommandBase
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
