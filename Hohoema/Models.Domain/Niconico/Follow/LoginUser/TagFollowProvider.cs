using Mntone.Nico2;
using Mntone.Nico2.Users.Follow;
using Hohoema.Models.Infrastructure;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Hohoema.Models.Domain.Niconico.Follow.LoginUser
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

            var res = await ContextActionWithPageAccessWaitAsync(context =>
            {
                return context.User.GetFollowTagsAsync();
            });

            return res.Data.Tags;
        }

        public async Task<ContentManageResult> AddFollowAsync(string id)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            return await ContextActionAsync(context =>
            {
                return context.User.AddFollowTagAsync(id);
            });
            
        }

        public async Task<ContentManageResult> RemoveFollowAsync(string id)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            return await ContextActionAsync(context =>
            {
                return context.User.RemoveFollowTagAsync(id);
            });
        }

        public Task<bool> IsFollowingAsync(string tag)
        {
            return ContextActionAsync(async context =>
            {
//                return context.User.IsFollowingTagAsync(tag);

                var res = await context.User.GetFollowTagsAsync();
                return res.Data.Tags.Any(t => t.Name == tag);
            });
        }
    }

}
