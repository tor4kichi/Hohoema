#nullable enable
using Hohoema.Infra;
using NiconicoToolkit.Community;
using System.Linq;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Community;

public sealed class CommunityProvider : ProviderBase
{
    public CommunityProvider(NiconicoSession niconicoSession)
        : base(niconicoSession)
    {
    }

    public async Task<(CommunityVideoResponse, CommunityVideoListItemsResponse)> GetCommunityVideoAsync(
        CommunityId communityId,
        int? offset,
        int? limit,
        CommunityVideoSortKey? sortKey,
        CommunityVideoSortOrder? sortOrder
        )
    {
        CommunityVideoResponse listRes = await _niconicoSession.ToolkitContext.Community.GetCommunityVideoListAsync(communityId, offset, limit, sortKey, sortOrder);
        if (!listRes.IsSuccess)
        {
            return (listRes, null);
        }

        CommunityVideoListItemsResponse itemsRes = await _niconicoSession.ToolkitContext.Community.GetCommunityVideoListItemsAsync(listRes.Data.Contents.Select(x => x.ContentId));
        return (listRes, itemsRes);
    }
}
