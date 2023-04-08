using Hohoema.Models.Niconico.Follow;
using NiconicoToolkit.Channels;

namespace Hohoema.Models.Niconico.Channel;

public interface IChannel : INiconicoGroup, IFollowable
{
    public ChannelId ChannelId { get; }
    public new string Name { get; }
}
