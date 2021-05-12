using Hohoema.Models.Domain.Niconico.Channel;
using System;

namespace Hohoema.Presentation.ViewModels.Niconico.Follow
{
    public sealed class FollowChannelViewModel : IChannel
    {
        private readonly Mntone.Nico2.Users.Follow.FollowChannelResponse.FollowChannel _followChannel;

        public FollowChannelViewModel(Mntone.Nico2.Users.Follow.FollowChannelResponse.FollowChannel followChannel)
        {
            _followChannel = followChannel;
        }
        public string Id => _followChannel.Id.ToString();

        public string Label => _followChannel.Name;

        public Uri ThumbnailUrl => _followChannel.ThumbnailUrl;

        public string Description => _followChannel.Description;
    }
}
