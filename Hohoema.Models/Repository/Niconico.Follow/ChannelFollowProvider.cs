using Hohoema.Models.Niconico;
using Mntone.Nico2;
using Mntone.Nico2.Users.Follow;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.Follow
{
    using NiconicoSession = Hohoema.Models.Niconico.NiconicoSession;

    public sealed class ChannelFollowProvider : ProviderBase, IFollowProvider
    {
        public ChannelFollowProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }

        public async Task<List<ChannelFollowData>> GetAllAsync()
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return new List<ChannelFollowData>();
            }


            return await ContextActionWithPageAccessWaitAsync(async context =>
            {
                return await context.User.GetFollowChannelAsync();
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
                return await context.User.AddFollowChannelAsync(id);
            });

            return result.IsSucceed ? ContentManageResult.Success : ContentManageResult.Failed;
        }

        public async Task<ContentManageResult> RemoveFollowAsync(string id)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            var result = await ContextActionAsync(async context =>
            {
                return await context.User.DeleteFollowChannelAsync(id);
            });

            return result.IsSucceed ? ContentManageResult.Success : ContentManageResult.Failed;
        }
    }
}
