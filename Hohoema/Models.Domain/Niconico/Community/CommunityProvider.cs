using Mntone.Nico2;
using Mntone.Nico2.Communities.Detail;
using Mntone.Nico2.Communities.Info;
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

        public async Task<NicovideoCommunityResponse> GetCommunityInfo(
            string communityId
            )
        {
            return await ContextActionAsync(async context =>
            {
                return await context.Community.GetCommunifyInfoAsync(communityId);
            });
            
        }


        public async Task<CommunityDetailResponse> GetCommunityDetail(
            string communityId
            )
        {
            return await ContextActionWithPageAccessWaitAsync(async context =>
            {
                return await context.Community.GetCommunityDetailAsync(communityId);
            });
        }


        public async Task<(CommunityVideoResponse, CommunityVideoListItemsResponse)> GetCommunityVideoAsync(
            string communityId,
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
