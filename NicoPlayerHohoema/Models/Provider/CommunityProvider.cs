using Mntone.Nico2.Communities.Detail;
using Mntone.Nico2.Communities.Info;
using Mntone.Nico2.Videos.Ranking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Provider
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
            return await Context.Community.GetCommunifyInfoAsync(communityId);
        }


        public async Task<CommunityDetailResponse> GetCommunityDetail(
            string communityId
            )
        {
            await WaitNicoPageAccess();

            return await Context.Community.GetCommunityDetailAsync(communityId);
        }


        public async Task<NiconicoVideoRss> GetCommunityVideo(
            string communityId,
            uint page
            )
        {
            return await Context.Community.GetCommunityVideoAsync(communityId, page);
        }




    }
}
