#nullable enable
using Hohoema.Models.Niconico.Community;
using Hohoema.Models.Playlist;
using Microsoft.Toolkit.Diagnostics;
using NiconicoToolkit.Community;
using System.Threading.Tasks;

namespace Hohoema.Services.Playlist.PlaylistFactory;

public sealed class CommunityVideoPlaylistFactory : IPlaylistFactory
{
    private readonly CommunityProvider _communityProvider;

    public CommunityVideoPlaylistFactory(CommunityProvider communityProvider)
    {
        _communityProvider = communityProvider;
    }

    public async ValueTask<IPlaylist> Create(PlaylistId playlistId)
    {
        CommunityId communityId = playlistId.Id;
        var communityInfo = await _communityProvider.GetCommunityInfo(communityId);
        Guard.IsTrue(communityInfo.IsOK, nameof(communityInfo.IsOK));
        return new CommunityVideoPlaylist(communityId, playlistId, communityInfo.Community.Name, _communityProvider);
    }

    public IPlaylistSortOption DeserializeSortOptions(string serializedSortOptions)
    {
        return CommunityVideoPlaylistSortOption.Deserialize(serializedSortOptions);
    }
}
