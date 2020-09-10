using Hohoema.Models.Domain.Niconico.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Playlist
{
    public interface IPlaylistItem : IVideoContent
    {
        public PlaylistOrigin PlaylistOrigin { get; }
        public string PlaylistId { get; }
    }
}
