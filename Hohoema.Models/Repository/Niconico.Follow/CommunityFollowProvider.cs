using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Follow;
using Mntone.Nico2;
using Mntone.Nico2.Users.FollowCommunity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.Follow
{
    using NiconicoSession = Hohoema.Models.Niconico.NiconicoSession;

    public sealed class CommunityFollowProvider : ProviderBase, IFollowProvider
    {
        public CommunityFollowProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }


        public static CommunituFollowAdditionalInfo CommunituFollowAdditionalInfo { get; set; }

        public async Task<List<FollowCommunityInfo>> GetAllAsync()
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return new List<FollowCommunityInfo>();
            }

            var items = new List<FollowCommunityInfo>();
            bool needMore = true;
            int page = 0;

            while (needMore)
            {
                try
                {
                    var res = await ContextActionWithPageAccessWaitAsync(async context =>
                    {
                        return await context.User.GetFollowCommunityAsync(page);
                    });

                    items.AddRange(res.Items);

                    // フォローコミュニティページの一画面での最大表示数10個と同数の場合は追加で取得
                    needMore = res.Items.Count == 10;
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
            if (CommunituFollowAdditionalInfo == null)
            {
                return ContentManageResult.Failed;
            }
            var title = CommunituFollowAdditionalInfo.Title;
            var comment = CommunituFollowAdditionalInfo.Comment;
            var notify = CommunituFollowAdditionalInfo.Notify;

            var result = await ContextActionAsync(async context =>
            {
                return await context.User.AddFollowCommunityAsync(id, title, comment, notify);
            });

            return result ? ContentManageResult.Success : ContentManageResult.Failed;
        }

        public async Task<ContentManageResult> RemoveFollowAsync(string id)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            var result = await ContextActionAsync(async context =>
            {
                var leaveToken = await context.User.GetFollowCommunityLeaveTokenAsync(id);
                return await context.User.RemoveFollowCommunityAsync(leaveToken);
            });

            return result ? ContentManageResult.Success : ContentManageResult.Failed;
        }
    }
}
