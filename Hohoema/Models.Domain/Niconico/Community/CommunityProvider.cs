using Mntone.Nico2;
using Mntone.Nico2.Communities.Detail;
using Mntone.Nico2.Communities.Info;
using Hohoema.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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


        public async Task<RssVideoResponse> GetCommunityVideo(
            string communityId,
            uint page
            )
        {
            return await ContextActionAsync(async context =>
            {
                return await context.Community.GetCommunityVideoAsync(communityId, page);
            });
        }




    }
}
