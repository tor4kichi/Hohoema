using Mntone.Nico2;
using Mntone.Nico2.Users.Follow;
using Mntone.Nico2.Users.FollowCommunity;
using Mntone.Nico2.Videos.Histories;
using Mntone.Nico2.Videos.Recommend;
using Mntone.Nico2.Videos.RemoveHistory;
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

            var res = await ContextActionWithPageAccessWaitAsync(async context =>
            {
                return await context.Video.GetHistoriesAsync();
            });

            foreach (var history in res?.Histories ?? Enumerable.Empty<History>())
            {
                Database.VideoPlayedHistoryDb.VideoPlayed(history.Id);
            }

            return res;
        }


        public Task<RemoveHistoryResponse> RemoveAllHistoriesAsync(string token)
        {
            return ContextActionAsync(async context =>
            {
                return await context.Video.RemoveAllHistoriesAsync(token);
            });
            
        }

        public Task<RemoveHistoryResponse> RemoveHistoryAsync(string token, string videoId)
        {
            return ContextActionAsync(async context =>
            {
                return await context.Video.RemoveHistoryAsync(token, videoId);
            });
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

        public async Task<List<UserFollowItem>> GetAllAsync()
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return new List<UserFollowItem>();
            }

            var res = await ContextActionWithPageAccessWaitAsync(async context =>
            {
                return await context.User.GetFollowUsersAsync();
            });

            return res.Data.Items;
        }

        public async Task<ContentManageResult> AddFollowAsync(string id)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            return await ContextActionAsync(async context => 
            {
                return await context.User.AddUserFollowAsync(NiconicoItemType.User, id);
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
                return await context.User.RemoveUserFollowAsync(NiconicoItemType.User, id);
            });
            
        }
    }


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
                return await context.User.AddUserFollowAsync(NiconicoItemType.Mylist, id);
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
                return await context.User.RemoveUserFollowAsync(NiconicoItemType.Mylist, id);
            });
        }
    }

    public sealed class ChannelFollowProvider : ProviderBase, IFollowProvider
    {
        public ChannelFollowProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }

        public async Task<List<FollowChannelResponse.FollowChannel>> GetAllAsync()
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return new List<FollowChannelResponse.FollowChannel>();
            }


            var res =  await ContextActionWithPageAccessWaitAsync(async context =>
            {
                return await context.User.GetFollowChannelAsync();
            });

            return res.Data;
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
