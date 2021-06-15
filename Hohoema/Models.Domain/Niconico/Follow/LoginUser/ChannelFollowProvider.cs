using Hohoema.Models.Infrastructure;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Hohoema.Models.Domain.Niconico.Channel;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using Microsoft.Toolkit.Mvvm.Messaging;
using NiconicoToolkit.Account;
using NiconicoToolkit.Follow;

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

        public async Task<FollowChannelResponse> GetChannelsAsync(int offset, int pageSize)
        {
            if (!_niconicoSession.IsLoggedIn)
            {
                throw new InvalidOperationException();
            }

            return await _niconicoSession.ToolkitContext.Follow.Channel.GetFollowChannelAsync(offset, pageSize);
        }

        public Task<bool> IsFollowingAsync(IChannel channel) => IsFollowingAsync(channel.Id);

        public async Task<ContentManageResult> AddFollowAsync(IChannel channel)
        {
            if (!_niconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            var result = await _niconicoSession.ToolkitContext.Follow.Channel.AddFollowChannelAsync(channel.Id);

            if (result.IsSucceed)
            {
                _messenger.Send<ChannelFollowAddedMessage>(new (channel));
            }

            return result.IsSucceed ? ContentManageResult.Success : ContentManageResult.Failed;
        }


        public async Task<ContentManageResult> RemoveFollowAsync(IChannel channel)
        {
            if (!_niconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            if (!await _messenger.Send<ChannelFollowRemoveConfirmingAsyncRequestMessage>(new(channel)))
            {
                return ContentManageResult.Exist; 
            }

            var result = await _niconicoSession.ToolkitContext.Follow.Channel.DeleteFollowChannelAsync(channel.Id);

            if (result.IsSucceed)
            {
                _messenger.Send<ChannelFollowRemovedMessage>(new(channel));
            }

            return result.IsSucceed ? ContentManageResult.Success : ContentManageResult.Failed;
        }


        public async Task<bool> IsFollowingAsync(string channelId)
        {
            var channelInfo = await _niconicoSession.ToolkitContext.Channel.GetChannelInfoAsync(channelId);
            var res = await _niconicoSession.ToolkitContext.Follow.Channel.GetChannelAuthorityAsync((uint)channelInfo.ChannelId);

            return res.Data?.Session?.IsFollowing ?? false;
        }

        public async Task<bool> IsFollowingAsync(uint numberId)
        {
            var res = await _niconicoSession.ToolkitContext.Follow.Channel.GetChannelAuthorityAsync(numberId);

            return res.Data?.Session?.IsFollowing ?? false;
        }

        public async Task<ChannelAuthorityResponse> GetChannelAuthorityAsync(uint numberId)
        {
            return await _niconicoSession.ToolkitContext.Follow.Channel.GetChannelAuthorityAsync(numberId);
        }

        public async Task<ChannelAuthorityResponse> GetChannelAuthorityAsync(string channelScreenName)
        {
            var channelInfo = await _niconicoSession.ToolkitContext.Channel.GetChannelInfoAsync(channelScreenName);
            return await _niconicoSession.ToolkitContext.Follow.Channel.GetChannelAuthorityAsync((uint)channelInfo.ChannelId);

        }
    }

}
