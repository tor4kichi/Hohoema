using Mntone.Nico2;
using Mntone.Nico2.Users.Follow;
using Hohoema.Models.Infrastructure;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.Follow.LoginUser
{
    public sealed class MylistFollowProvider : ProviderBase, IFollowProvider
    {
        public MylistFollowProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }

        public async Task<List<FollowMylist>> GetAllAsync()
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return new List<FollowMylist>();
            }


            var res = await ContextActionWithPageAccessWaitAsync(async context =>
            {
                return await context.User.GetFollowMylistsAsync();
            });

            return res.Data.Mylists;
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
    }

}
