using NiconicoToolkit.Channels;
using System;

namespace NiconicoToolkit.Follow
{
    public interface IChannelItem
    {
        long BodyPrice { get; set; }
        bool CanAdmit { get; set; }
        string Description { get; set; }
        ChannelId Id { get; set; }
        bool IsAdult { get; set; }
        bool IsFree { get; set; }
        string Name { get; set; }
        string OwnerName { get; set; }
        long Price { get; set; }
        string ScreenName { get; set; }
        Uri ThumbnailSmallUrl { get; set; }
        Uri ThumbnailUrl { get; set; }
        Uri Url { get; set; }        
    }
}