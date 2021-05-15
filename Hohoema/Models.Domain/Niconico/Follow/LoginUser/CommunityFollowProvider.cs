using Mntone.Nico2;
using Hohoema.Models.Infrastructure;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mntone.Nico2.Users.Follow;
using System;
using Hohoema.Models.Domain.Niconico.Community;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using Microsoft.Toolkit.Mvvm.Messaging;

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

        public async Task<UserOwnedCommunityResponse> GetUserOwnedCommunitiesAsync(uint userId)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                throw new InvalidOperationException();
            }

            return await ContextActionWithPageAccessWaitAsync(context =>
            {
                return context.User.GetUserOwnedCommunitiesAsync(userId);
            });
        }


        public async Task<FollowCommunityResponse> GetCommunityItemsAsync(uint pageSize, uint page )
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                throw new InvalidOperationException();
            }

            return await ContextActionWithPageAccessWaitAsync(context =>
            {
                return  context.User.GetFollowCommunityAsync(pageSize, page);
            });
        }

        public Task<bool> IsFollowingAsync(ICommunity community) => IsFollowingAsync(community.Id);

        public async Task<ContentManageResult> AddFollowAsync(ICommunity community)
        {
            var result = await ContextActionAsync(async context =>
            {
                return await context.User.AddFollowCommunityAsync(community.Id);
            });

            if (result is ContentManageResult.Success or ContentManageResult.Exist)
            {
                _messenger.Send<CommunityFollowAddedMessage>(new(community));
            }

            return result;
        }


        public async Task<ContentManageResult> RemoveFollowAsync(ICommunity community)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            if (!await _messenger.Send<CommunityFollowRemoveConfirmingAsyncRequestMessage>(new (community)))
            {
                return ContentManageResult.Exist;
            }

            var result = await ContextActionAsync(async context =>
            {
                return await context.User.RemoveFollowCommunityAsync(community.Id);
            });

            if (result is ContentManageResult.Success)
            {
                _messenger.Send<CommunityFollowRemovedMessage>(new(community));
            }

            return result;
        }


        public Task<bool> IsFollowingAsync(string id)
        {
            return ContextActionAsync(async context =>
            {
                try
                {
                    var res = await context.User.GetCommunityAuthorityAsync(id);
                    return res.Data?.IsMember ?? false;
                }
                catch
                {
                    return false;
                }
            });
        }

        public Task<CommunityAuthorityResponse> GetCommunityAuthorityAsync(string id)
        {
            return ContextActionAsync(async context =>
            {
                return await context.User.GetCommunityAuthorityAsync(id);
            });
        }
    }

}
