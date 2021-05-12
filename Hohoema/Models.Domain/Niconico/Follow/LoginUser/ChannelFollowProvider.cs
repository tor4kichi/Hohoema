using Mntone.Nico2;
using Mntone.Nico2.Users.Follow;
using Hohoema.Models.Infrastructure;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Hohoema.Models.Domain.Niconico.Follow.LoginUser
{
    public sealed class ChannelFollowProvider : ProviderBase, IFollowProvider
    {
        public ChannelFollowProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }

        public async Task<FollowChannelResponse> GetChannelsAsync(uint pageSize, uint offset)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                throw new InvalidOperationException();
            }

            return await ContextActionWithPageAccessWaitAsync(context =>
            {
                return context.User.GetFollowChannelAsync(pageSize, offset);
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

        public async Task<bool> IsFollowingAsync(string channelId)
        {
            var res = await ContextActionAsync(async context =>
            {
                var channelInfo = await context.Channel.GetChannelInfo(channelId);
                return await context.User.GetChannelAuthorityAsync((uint)channelInfo.ChannelId);
            });

            return res.Data?.Session?.IsFollowing ?? false;
        }

        public async Task<bool> IsFollowingAsync(uint numberId)
        {
            var res = await ContextActionAsync(async context =>
            {
                return await context.User.GetChannelAuthorityAsync(numberId);
            });

            return res.Data?.Session?.IsFollowing ?? false;
        }

        public async Task<ChannelAuthorityResponse> GetChannelAuthorityAsync(uint numberId)
        {
            var res = await ContextActionAsync(async context =>
            {
                return await context.User.GetChannelAuthorityAsync(numberId);
            });

            return res;
        }

        public async Task<ChannelAuthorityResponse> GetChannelAuthorityAsync(string channelScreenName)
        {
            var res = await ContextActionAsync(async context =>
            {
                var channelInfo = await context.Channel.GetChannelInfo(channelScreenName);                
                return await context.User.GetChannelAuthorityAsync((uint)channelInfo.ChannelId);
            });

            return res;
        }
    }

}
