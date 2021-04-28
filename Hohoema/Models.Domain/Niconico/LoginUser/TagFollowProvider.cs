using Mntone.Nico2;
using Mntone.Nico2.Users.Follow;
using Hohoema.Models.Infrastructure;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.LoginUser
{
    public sealed class TagFollowProvider : ProviderBase, IFollowProvider
    {
        public TagFollowProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }

        public async Task<List<FollowTagsResponse.Tag>> GetAllAsync()
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return new List<FollowTagsResponse.Tag>();
            }

            var res = await ContextActionWithPageAccessWaitAsync(async context =>
            {
                return await context.User.GetFollowTagsAsync();
            });

            return res.Data.Tags;
        }

        public async Task<ContentManageResult> AddFollowAsync(string id)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            return await ContextActionAsync(async context =>
            {
                return await context.User.AddFollowTagAsync(id);
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
                return await context.User.RemoveFollowTagAsync(id);
            });
            
        }
    }

}
