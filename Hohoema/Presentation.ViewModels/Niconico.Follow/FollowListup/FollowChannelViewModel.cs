using Hohoema.Models.Domain.Niconico.Channel;
using NiconicoToolkit.Follow;
using System;

namespace Hohoema.Presentation.ViewModels.Niconico.Follow
{
    public sealed class FollowChannelViewModel : IChannel
    {
        private readonly IChannelItem _followChannel;

        public FollowChannelViewModel(IChannelItem followChannel)
        {
            _followChannel = followChannel;
        }
        public string Id => _followChannel.Id.ToString();

        public string Label => _followChannel.Name;

        public Uri ThumbnailUrl => _followChannel.ThumbnailUrl;

        public string Description => _followChannel.Description;
    }
}
