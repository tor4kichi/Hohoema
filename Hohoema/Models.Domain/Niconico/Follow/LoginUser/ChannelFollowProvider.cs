using Mntone.Nico2;
using Mntone.Nico2.Users.Follow;
using Hohoema.Models.Infrastructure;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Hohoema.Models.Domain.Niconico.Channel;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using Microsoft.Toolkit.Mvvm.Messaging;

namespace Hohoema.Models.Domain.Niconico.Follow.LoginUser
{
    public sealed class ChannelFollowAddedMessage : ValueChangedMessage<IChannel>
    {
        public ChannelFollowAddedMessage(IChannel value) : base(value)
        {
        }
    }


    public sealed class ChannelFollowRemovedMessage : ValueChangedMessage<IChannel>
    {
        public ChannelFollowRemovedMessage(IChannel value) : base(value)
        {
        }
    }


    public sealed class ChannelFollowRemoveConfirmingAsyncRequestMessage : AsyncRequestMessage<bool>
    {
        public ChannelFollowRemoveConfirmingAsyncRequestMessage(IChannel channel)
        {
            Target = channel;
        }

        public IChannel Target { get; }
    }



    public sealed class ChannelFollowProvider : ProviderBase, IFollowProvider<IChannel>
    {
        private readonly IMessenger _messenger;

        public ChannelFollowProvider(NiconicoSession niconicoSession, IMessenger messenger)
            : base(niconicoSession)
        {
            _messenger = messenger;
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

        public Task<bool> IsFollowingAsync(IChannel channel) => IsFollowingAsync(channel.Id);

        public async Task<ContentManageResult> AddFollowAsync(IChannel channel)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            var result = await ContextActionAsync(async context =>
            {
                return await context.User.AddFollowChannelAsync(channel.Id);
            });

            if (result.IsSucceed)
            {
                _messenger.Send<ChannelFollowAddedMessage>(new (channel));
            }

            return result.IsSucceed ? ContentManageResult.Success : ContentManageResult.Failed;
        }


        public async Task<ContentManageResult> RemoveFollowAsync(IChannel channel)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            if (!await _messenger.Send<ChannelFollowRemoveConfirmingAsyncRequestMessage>(new(channel)))
            {
                return ContentManageResult.Exist; 
            }

            var result = await ContextActionAsync(async context =>
            {
                return await context.User.DeleteFollowChannelAsync(channel.Id);
            });

            if (result.IsSucceed)
            {
                _messenger.Send<ChannelFollowRemovedMessage>(new(channel));
            }

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
