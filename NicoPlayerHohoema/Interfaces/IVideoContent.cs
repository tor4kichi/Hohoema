using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Interfaces
{
    public interface INiconicoContent
    {
        string Id { get; }
        string Label { get; }
    }

    public interface IVideoContent : INiconicoContent
    {
        string OwnerUserId { get; }
        string OwnerUserName { get; }

        Models.IPlayableList Playlist { get; }
    }

    public interface ILiveContent : INiconicoContent
    {
        string BroadcasterId { get; }
    }

    public interface ICommunity : INiconicoContent, IFollowable
    {
        
    }

    public interface IMylist : INiconicoContent, IFollowable
    {

    }

    public interface IUser : INiconicoContent, IFollowable
    {

    }

    public interface IFeedGroup : INiconicoContent
    {

    }
}
