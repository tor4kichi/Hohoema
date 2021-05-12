using Mntone.Nico2;
using Hohoema.Models.Infrastructure;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mntone.Nico2.Users.Follow;
using System;

namespace Hohoema.Models.Domain.Niconico.Follow.LoginUser
{
    public sealed class CommunityFollowProvider : ProviderBase, IFollowProvider
    {
        public CommunityFollowProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }


        public static CommunituFollowAdditionalInfo CommunituFollowAdditionalInfo { get; set; }

        public async Task<FollowCommunityResponse> GetCommunityItemsAsync(uint pageSize, uint page )
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                throw new InvalidOperationException();
            }

            return await ContextActionWithPageAccessWaitAsync(context =>
            {
                return  context.User.GetFollowCommunityAsync(pageSize, page);
            });
        }

        public async Task<ContentManageResult> AddFollowAsync(string id)
        {
            var result = await ContextActionAsync(async context =>
            {
                return await context.User.AddFollowCommunityAsync(id);
            });

            return result;
        }

        public async Task<ContentManageResult> RemoveFollowAsync(string id)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            var result = await ContextActionAsync(async context =>
            {
                return await context.User.RemoveFollowCommunityAsync(id);
            });

            return result;
        }

        public Task<bool> IsFollowingAsync(string id)
        {
            return ContextActionAsync(async context =>
            {
                try
                {
                    var res = await context.User.GetCommunityAuthorityAsync(id);
                    return res.Data?.IsMember ?? false;
                }
                catch
                {
                    return false;
                }
            });
        }

        public Task<CommunityAuthorityResponse> GetCommunityAuthorityAsync(string id)
        {
            return ContextActionAsync(async context =>
            {
                return await context.User.GetCommunityAuthorityAsync(id);
            });
        }
    }

}
