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

    public interface IPlaylistSortOption : IEquatable<IPlaylistSortOption>
    {
        string Label { get; }
        string Serialize();
    }

    public interface IPlaylistItemPlayable
    {
        PlaylistItemToken? PlaylistItemToken { get; }
    }

    public record PlaylistItemToken(IPlaylist Playlist, IPlaylistSortOption SortOptions, IVideoContent Video, int Index);


    public interface IPlaylistPlayable
    {
        PlaylistToken PlaylistToken { get; }
    }

    public record PlaylistToken(IPlaylist Playlist, IPlaylistSortOption SortOptions);

    public interface IPlaylist
    {
        string Name { get; }
        public PlaylistId PlaylistId { get; }
        IPlaylistSortOption[] SortOptions { get; }

        IPlaylistSortOption DefaultSortOption { get; }
    }

    public interface IUnlimitedPlaylist : IPlaylist
    {
        Task<IEnumerable<IVideoContent>> GetPagedItemsAsync(int pageIndex, int pageSize, IPlaylistSortOption sortOption, CancellationToken cancellationToken = default);
    }

    public interface ISortablePlaylist : IPlaylist
    {
        public int TotalCount { get; }

        Task<IEnumerable<IVideoContent>> GetAllItemsAsync(IPlaylistSortOption sortOption, CancellationToken cancellationToken = default);
    }

    public interface IUserManagedPlaylist : ISortablePlaylist, INotifyCollectionChanged
    {
    }

}
