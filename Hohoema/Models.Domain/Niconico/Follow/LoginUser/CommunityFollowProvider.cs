using Hohoema.Models.Infrastructure;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Hohoema.Models.Domain.Niconico.Community;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using Microsoft.Toolkit.Mvvm.Messaging;
using NiconicoToolkit.Follow;
using NiconicoToolkit.Account;
using NiconicoToolkit.Community;
using NiconicoToolkit;
using NiconicoToolkit.User;

namespace Hohoema.Models.Domain.Niconico.Follow.LoginUser
{
    public sealed class CommunityFollowAddedMessage : ValueChangedMessage<ICommunity>
    {
        public CommunityFollowAddedMessage(ICommunity value) : base(value)
        {
        }
    }


    public sealed class CommunityFollowRemovedMessage : ValueChangedMessage<ICommunity>
    {
        public CommunityFollowRemovedMessage(ICommunity value) : base(value)
        {
        }
    }


    public sealed class CommunityFollowRemoveConfirmingAsyncRequestMessage : AsyncRequestMessage<bool>
    {
        public CommunityFollowRemoveConfirmingAsyncRequestMessage(ICommunity community)
        {
            Target = community;
        }

        public ICommunity Target { get; }
    }


    public sealed class CommunityFollowProvider : ProviderBase, IFollowProvider<ICommunity>
    {
        private readonly IMessenger _messenger;

        public CommunityFollowProvider(NiconicoSession niconicoSession, IMessenger messenger)
            : base(niconicoSession)
        {
            _messenger = messenger;
        }

        public static CommunituFollowAdditionalInfo CommunituFollowAdditionalInfo { get; set; }

        public async Task<UserOwnedCommunityResponse> GetUserOwnedCommunitiesAsync(UserId userId)
        {
            if (!_niconicoSession.IsLoggedIn)
            {
                throw new InvalidOperationException();
            }

            return await _niconicoSession.ToolkitContext.Follow.Community.GetUserOwnedCommunitiesAsync(userId);
        }


        public async Task<FollowCommunityResponse> GetCommunityItemsAsync(uint pageSize, uint page )
        {
            if (!_niconicoSession.IsLoggedIn)
            {
                throw new InvalidOperationException();
            }

            return await _niconicoSession.ToolkitContext.Follow.Community.GetFollowCommunityAsync((int)page, (int)pageSize);
        }

        public Task<bool> IsFollowingAsync(ICommunity community) => IsFollowingAsync(community.CommunityId);

        public async Task<ContentManageResult> AddFollowAsync(ICommunity community)
        {
            var result = await _niconicoSession.ToolkitContext.Follow.Community.AddFollowCommunityAsync(community.CommunityId);

            if (result is ContentManageResult.Success or ContentManageResult.Exist)
            {
                _messenger.Send<CommunityFollowAddedMessage>(new(community));
            }

            return result;
        }


        public async Task<ContentManageResult> RemoveFollowAsync(ICommunity community)
        {
            if (!_niconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            if (!await _messenger.Send<CommunityFollowRemoveConfirmingAsyncRequestMessage>(new (community)))
            {
                return ContentManageResult.Exist;
            }

            var result = await _niconicoSession.ToolkitContext.Follow.Community.RemoveFollowCommunityAsync(community.CommunityId);

            if (result is ContentManageResult.Success)
            {
                _messenger.Send<CommunityFollowRemovedMessage>(new(community));
            }

            return result;
        }


        public async Task<bool> IsFollowingAsync(string id)
        {
            var res = await _niconicoSession.ToolkitContext.Community.GetCommunityAuthorityForLoginUserAsync(id);
            return res.Data?.IsMember ?? false;
        }

        public Task<CommunityAuthorityResponse> GetCommunityAuthorityAsync(string id)
        {
            return _niconicoSession.ToolkitContext.Community.GetCommunityAuthorityForLoginUserAsync(id);
        }
    }

}
