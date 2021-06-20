using Hohoema.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiconicoToolkit.Community;

namespace Hohoema.Models.Domain.Niconico.Community
{
    public sealed class CommunityProvider : ProviderBase
    {
        public CommunityProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }

        public Task<CommunityInfoResponse> GetCommunityInfo(CommunityId communityId)
        {
            return _niconicoSession.ToolkitContext.Community.GetCommunityInfoAsync(communityId);
        }

        public async Task<(CommunityVideoResponse, CommunityVideoListItemsResponse)> GetCommunityVideoAsync(
            CommunityId communityId,
            int? offset,
            int? limit,
            CommunityVideoSortKey? sortKey,
            CommunityVideoSortOrder? sortOrder
            )
        {
            var listRes =  await _niconicoSession.ToolkitContext.Community.GetCommunityVideoListAsync(communityId, offset, limit, sortKey, sortOrder);
            if (!listRes.IsSuccess)
            {
                return (listRes, null);
            }

            var itemsRes = await _niconicoSession.ToolkitContext.Community.GetCommunityVideoListItemsAsync(listRes.Data.Contents.Select(x => x.ContentId));
            return (listRes, itemsRes);
        }
    }
}
