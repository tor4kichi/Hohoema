using Hohoema.Models.Niconico;
using Mntone.Nico2;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.Follow
{
    using NiconicoSession = Hohoema.Models.Niconico.NiconicoSession;

    public sealed class TagFollowProvider : ProviderBase, IFollowProvider
    {
        public TagFollowProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }

        public async Task<List<string>> GetAllAsync()
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return new List<string>();
            }

            return await ContextActionWithPageAccessWaitAsync(async context =>
            {
                return await context.User.GetFollowTagsAsync();
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
                return await context.User.AddFollowTagAsync(id);
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
                return await context.User.RemoveFollowTagAsync(id);
            });

            return result.ToModelContentManageResult();
        }
    }
}
