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
                return await context.Video.GetHistoriesFromMyPageAsync();
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

        public async Task<List<FollowData>> GetAllAsync()
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return new List<FollowData>();
            }


            return await ContextActionWithPageAccessWaitAsync(async context =>
            {
                return await context.User.GetFollowMylistsAsync();
            });
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
