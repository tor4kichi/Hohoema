using Hohoema.Models.Niconico;
using Mntone.Nico2;
using Mntone.Nico2.Users.Follow;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.Follow
{
    using NiconicoSession = Hohoema.Models.Niconico.NiconicoSession;

    public sealed class UserFollowProvider : ProviderBase, IFollowProvider
    {
        public UserFollowProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }

        public async Task<List<FollowData>> GetAllAsync()
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return new List<FollowData>();
            }

            return await ContextActionWithPageAccessWaitAsync(async context =>
            {
                return await context.User.GetFollowUsersAsync();
            });
        }

        public async Task<ContentManageResult> AddFollowAsync(string id)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            var result = await ContextActionAsync(async context =>
            {
                return await context.User.AddUserFollowAsync(NiconicoItemType.User, id);
            });

            return result.ToModelContentManageResult();
        }

        public async Task<ContentManageResult> RemoveFollowAsync(string id)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            var result = await ContextActionAsync(async context =>
            {
                return await context.User.RemoveUserFollowAsync(NiconicoItemType.User, id);
            });

            return result.ToModelContentManageResult();
        }
    }
}
