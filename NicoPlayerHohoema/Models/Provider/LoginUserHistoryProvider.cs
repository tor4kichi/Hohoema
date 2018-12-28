using Mntone.Nico2;
using Mntone.Nico2.Users.Follow;
using Mntone.Nico2.Users.FollowCommunity;
using Mntone.Nico2.Videos.Histories;
using Mntone.Nico2.Videos.Recommend;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Provider
{
    public sealed class LoginUserHistoryProvider : ProviderBase
    {
        public LoginUserHistoryProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }

        public async Task<HistoriesResponse> GetHistory()
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return null;
            }

            await WaitNicoPageAccess();

            var res = await Context.Video.GetHistoriesFromMyPageAsync();

            foreach (var history in res?.Histories ?? Enumerable.Empty<History>())
            {
                Database.VideoPlayedHistoryDb.VideoPlayed(history.Id);
            }

            return res;
        }


        public async Task RemoveAllHistoriesAsync(string token)
        {
            await Context.Video.RemoveAllHistoriesAsync(token);
        }

        public async Task RemoveHistoryAsync(string token, string videoId)
        {
            await Context.Video.RemoveHistoryAsync(token, videoId);
        }
    }

    interface IFollowProvider
    {
        Task<ContentManageResult> AddFollowAsync(string id);
        Task<ContentManageResult> RemoveFollowAsync(string id);
    }

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

            await WaitNicoPageAccess();

            return await NiconicoSession.Context.User.GetFollowUsersAsync();
        }

        public async Task<ContentManageResult> AddFollowAsync(string id)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            return await NiconicoSession.Context.User.AddUserFollowAsync(NiconicoItemType.User, id);
        }

        public async Task<ContentManageResult> RemoveFollowAsync(string id)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            return await NiconicoSession.Context.User.RemoveUserFollowAsync(NiconicoItemType.User, id);
        }
    }


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

            await WaitNicoPageAccess();

            return await NiconicoSession.Context.User.GetFollowTagsAsync();
        }

        public async Task<ContentManageResult> AddFollowAsync(string id)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            return await NiconicoSession.Context.User.AddFollowTagAsync(id);
        }

        public async Task<ContentManageResult> RemoveFollowAsync(string id)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            return await NiconicoSession.Context.User.RemoveFollowTagAsync(id);
        }
    }


    public sealed class MylistFollowProvider : ProviderBase, IFollowProvider
    {
        public MylistFollowProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }

        public async Task<List<FollowData>> GetAllAsync()
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return new List<FollowData>();
            }

            await WaitNicoPageAccess();

            return await NiconicoSession.Context.User.GetFollowMylistsAsync();
        }

        public async Task<ContentManageResult> AddFollowAsync(string id)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            return await NiconicoSession.Context.User.AddUserFollowAsync(NiconicoItemType.Mylist, id);
        }

        public async Task<ContentManageResult> RemoveFollowAsync(string id)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            return await NiconicoSession.Context.User.RemoveUserFollowAsync(NiconicoItemType.Mylist, id);
        }
    }

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

            await WaitNicoPageAccess();

            return await NiconicoSession.Context.User.GetFollowChannelAsync();
        }

        public async Task<ContentManageResult> AddFollowAsync(string id)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            var result = await NiconicoSession.Context.User.AddFollowChannelAsync(id);
            return result.IsSucceed ? ContentManageResult.Success : ContentManageResult.Failed;
        }

        public async Task<ContentManageResult> RemoveFollowAsync(string id)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            var result = await NiconicoSession.Context.User.DeleteFollowChannelAsync(id);
            return result.IsSucceed ? ContentManageResult.Success : ContentManageResult.Failed;
        }
    }


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

            await WaitNicoPageAccess();

            var items = new List<FollowCommunityInfo>();
            bool needMore = true;
            int page = 0;

            while (needMore)
            {
                try
                {
                    var res = await NiconicoSession.Context.User.GetFollowCommunityAsync(page);
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

            var result = await NiconicoSession.Context.User.AddFollowCommunityAsync(id, title, comment, notify);

            return result ? ContentManageResult.Success : ContentManageResult.Failed;
        }

        public async Task<ContentManageResult> RemoveFollowAsync(string id)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            var leaveToken = await NiconicoSession.Context.User.GetFollowCommunityLeaveTokenAsync(id);
            var result = await NiconicoSession.Context.User.RemoveFollowCommunityAsync(leaveToken);

            return result ? ContentManageResult.Success : ContentManageResult.Failed;
        }
    }

}
