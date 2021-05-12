using Mntone.Nico2;
using Mntone.Nico2.Users.Follow;
using Hohoema.Models.Infrastructure;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace Hohoema.Models.Domain.Niconico.Follow.LoginUser
{
    public sealed class MylistFollowProvider : ProviderBase, IFollowProvider
    {
        public MylistFollowProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }

        public async Task<FollowMylistResponse> GetFollowMylistsAsync(uint sampleItemsCount = 3)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                throw new InvalidOperationException();
            }


            return await ContextActionWithPageAccessWaitAsync(context =>
            {
                return context.User.GetFollowMylistsAsync(sampleItemsCount);
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
                return await context.User.AddFollowMylistAsync(id);
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
                return await context.User.RemoveFollowMylistAsync(id);
            });
        }

        public Task<bool> IsFollowingAsync(string id)
        {
            return ContextActionAsync(async context =>
            {
                var numberId = long.Parse(id);
                var res = await context.User.GetFollowMylistsAsync(0);
                return res.Data.Mylists.Any(x => x.Id == numberId);
            });
        }
    }

}
