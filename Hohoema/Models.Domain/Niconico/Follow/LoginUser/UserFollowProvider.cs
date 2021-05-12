using Mntone.Nico2;
using Mntone.Nico2.Users.Follow;
using Hohoema.Models.Infrastructure;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Hohoema.Models.Domain.Niconico.Follow.LoginUser
{
    public sealed class UserFollowProvider : ProviderBase, IFollowProvider
    {
        public UserFollowProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }

        public async Task<List<UserFollowItem>> GetAllAsync()
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return new List<UserFollowItem>();
            }

            const int PageSize = 100;

            List<UserFollowItem> followers = new List<UserFollowItem>();
            var res = await ContextActionWithPageAccessWaitAsync(async context =>
            {
                return await context.User.GetFollowUsersAsync(PageSize);
            });

            followers.AddRange(res.Data.Items);
            while (res.Data.Summary.HasNext)
            {
                res = await ContextActionWithPageAccessWaitAsync(async context =>
                {
                    return await context.User.GetFollowUsersAsync(PageSize, res);
                });

                followers.AddRange(res.Data.Items);
            }

            return followers;
        }

        public Task<FollowUsersResponse> GetItemsAsync(int pageSize, FollowUsersResponse lastRes = null)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                throw new InvalidOperationException();
            }

            return ContextActionWithPageAccessWaitAsync(async context =>
            {
                return await context.User.GetFollowUsersAsync((uint)pageSize, lastRes);
            });
        }

        public async Task<ContentManageResult> AddFollowAsync(string id)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            return await ContextActionAsync(async context => 
            {
                return await context.User.AddFollowUserAsync(id);
            });
        }

        public async Task<ContentManageResult> RemoveFollowAsync(string id)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            return await ContextActionAsync(async context =>
            {
                return await context.User.RemoveFollowUserAsync(id);
            });
        }

        public Task<bool> IsFollowingAsync(string id)
        {
            return IsFollowingAsync(uint.Parse(id));
        }


        public async Task<bool> IsFollowingAsync(uint id)
        {
            return await ContextActionAsync(async context =>
            {
                return await context.User.IsFollowingUserAsync(id);
            });
        }
    }

}
