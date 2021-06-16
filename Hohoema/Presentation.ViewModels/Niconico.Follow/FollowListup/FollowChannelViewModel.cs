using Hohoema.Models.Domain.Niconico.Channel;
using NiconicoToolkit;
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

        public Uri ThumbnailUrl => _followChannel.ThumbnailUrl;

        public string Description => _followChannel.Description;

        public NiconicoId ChannelId => (int)_followChannel.Id;

        public string Name => _followChannel.Name;
    }
}
