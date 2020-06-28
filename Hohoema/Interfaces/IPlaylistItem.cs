using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Interfaces
{
    public interface IPlaylistItem : IVideoContent
    {
        public PlaylistOrigin PlaylistOrigin { get; }
        public string PlaylistId { get; }
    }

    public enum PlaylistOrigin
    {
        Mylist,
        Local,
        ChannelVideos,
        UserVideos,
    }
}
