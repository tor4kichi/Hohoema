using Hohoema.Models.Niconico;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.Community
{
    public sealed class CommunityProvider : ProviderBase
    {
        public CommunityProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }

        public async Task<CommunityInfo> GetCommunityInfo(
            string communityId
            )
        {
            var res = await ContextActionAsync(async context =>
            {
                return await context.Community.GetCommunifyInfoAsync(communityId);
            });


            return new CommunityInfo(res.Community);
        }


        public async Task<CommunityDetail> GetCommunityDetail(
            string communityId
            )
        {
            var res = await ContextActionWithPageAccessWaitAsync(async context =>
            {
                return await context.Community.GetCommunityDetailAsync(communityId);
            });


            return new CommunityDetail(res.IsStatusOK, res?.CommunitySammary?.CommunityDetail);
        }


        public async Task<RssVideoResponse> GetCommunityVideo(
            string communityId,
            uint page
            )
        {
            var res = await ContextActionAsync(async context =>
            {
                return await context.Community.GetCommunityVideoAsync(communityId, page);
            });

            return new RssVideoResponse(res);
        }




    }
}
