using Hohoema.Models.Niconico.Video;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.Playlist;

public record PlaylistId
{
    public PlaylistId() { }

    public PlaylistId(PlaylistItemsSourceOrigin origin, string id)
    {
        Origin = origin;
        Id = id;
    }

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

public record PlaylistItemToken(IPlaylist Playlist, IPlaylistSortOption SortOptions, IVideoContent Video);


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
    int OneTimeLoadItemsCount { get; }
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
