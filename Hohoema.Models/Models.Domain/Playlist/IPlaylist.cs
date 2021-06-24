using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Hohoema.Models.Domain.Playlist
{
    public interface IPlaylist
    {
        string Name { get; }
        public PlaylistId PlaylistId { get; }
    }
}
