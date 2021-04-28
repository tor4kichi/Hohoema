using Mntone.Nico2;
using Hohoema.Models.Infrastructure;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.LoginUser.Follow
{
    public sealed class CommunityFollowProvider : ProviderBase, IFollowProvider
    {
        public CommunityFollowProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }


        public static CommunituFollowAdditionalInfo CommunituFollowAdditionalInfo { get; set; }

        public async Task<List<Mntone.Nico2.Users.Follow.FollowCommunityResponse.FollowCommunity>> GetAllAsync()
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return new List<Mntone.Nico2.Users.Follow.FollowCommunityResponse.FollowCommunity>();
            }

            var items = new List<Mntone.Nico2.Users.Follow.FollowCommunityResponse.FollowCommunity>();
            bool needMore = true;
            uint page = 0;

            while (needMore)
            {
                try
                {
                    var res = await ContextActionWithPageAccessWaitAsync(async context =>
                    {
                        return await context.User.GetFollowCommunityAsync(25, page);
                    });

                    items.AddRange(res.Data);

                    // フォローコミュニティページの一画面での最大表示数10個と同数の場合は追加で取得
                    needMore = res.Meta.Count != res.Meta.Total;
                }
                catch
                {
                    needMore = false;
                }

                page++;
            }

            return items;
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
    }

}
