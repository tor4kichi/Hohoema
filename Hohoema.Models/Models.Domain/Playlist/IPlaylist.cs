using Hohoema.Models.Domain.Niconico.Video;
using Microsoft.Toolkit.Collections;
using Microsoft.Toolkit.Diagnostics;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Playlist
{
    public record PlaylistId
    {
        public PlaylistItemsSourceOrigin Origin { get; init; }
        public string Id { get; init; }
    }

    public interface IPlaylistSortOptions
    {
        string Serialize();
    }

    public interface IPlaylistPlayableItem
    {
        PlaylistItemToken? PlaylistItemToken { get; }
    }

    public record PlaylistItemToken(PlaylistId PlaylistId, IPlaylistSortOptions SortOptions, VideoId VideoId);

    public interface IPlaylist : IIncrementalSource<IVideoContent>
    {
        string Name { get; }
        public PlaylistId PlaylistId { get; }
        public IPlaylistSortOptions SortOptions { get; set; }

        int IndexOf(IVideoContent videoId);
        bool Contains(IVideoContent video);
    }

    public interface IUserManagedPlaylist : IPlaylist, INotifyCollectionChanged
    {
        public int TotalCount { get; }
    }

}
