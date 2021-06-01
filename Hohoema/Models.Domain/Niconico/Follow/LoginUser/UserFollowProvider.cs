using Hohoema.Models.Infrastructure;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using Microsoft.Toolkit.Mvvm.Messaging;
using NiconicoToolkit.Follow;
using NiconicoToolkit.Account;

namespace Hohoema.Models.Domain.Niconico.Follow.LoginUser
{
    public class UserFollowAddedMessage : ValueChangedMessage<IUser>
    {
        public UserFollowAddedMessage(IUser value) : base(value)
        {
        }
    }

    public class UserFollowRemoveConfirmingAsyncRequestMessage : AsyncRequestMessage<bool>
    {
        public UserFollowRemoveConfirmingAsyncRequestMessage(IUser user)
        {
            Target = user;
        }

        public IUser Target { get; }
    }


    public class UserFollowRemovedMessage : ValueChangedMessage<IUser>
    {
        public UserFollowRemovedMessage(IUser value) : base(value)
        {
        }
    }


    public sealed class UserFollowProvider : ProviderBase, IFollowProvider<IUser>
    {
        private readonly IMessenger _messenger;

        public UserFollowProvider(NiconicoSession niconicoSession, IMessenger messenger)
            : base(niconicoSession)
        {
            _messenger = messenger;
        }


        public async Task<List<UserFollowItem>> GetAllAsync()
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return new List<UserFollowItem>();
            }

            const int PageSize = 100;

            List<UserFollowItem> followers = new List<UserFollowItem>();
            var res = await NiconicoSession.ToolkitContext.Follow.User.GetFollowUsersAsync(PageSize);

            followers.AddRange(res.Data.Items);
            while (res.Data.Summary.HasNext)
            {
                res = await NiconicoSession.ToolkitContext.Follow.User.GetFollowUsersAsync(PageSize, res);

                followers.AddRange(res.Data.Items);
            }

            return followers;
        }

        public Task<FollowUsersResponse> GetItemsAsync(int pageSize, FollowUsersResponse lastRes = null)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                throw new InvalidOperationException();
            }

            return NiconicoSession.ToolkitContext.Follow.User.GetFollowUsersAsync((uint)pageSize, lastRes);
        }

        Task<bool> IFollowProvider<IUser>.IsFollowingAsync(IUser followable) => IsFollowingAsync(followable.Id);

        public async Task<ContentManageResult> AddFollowAsync(IUser user)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            var result = await NiconicoSession.ToolkitContext.Follow.User.AddFollowUserAsync(user.Id);

            if (result is ContentManageResult.Success or ContentManageResult.Exist)
            {
                _messenger.Send<UserFollowAddedMessage>(new(user));
            }

            return result;
        }

        public async Task<ContentManageResult> RemoveFollowAsync(IUser user)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }
            
            if (!await _messenger.Send<UserFollowRemoveConfirmingAsyncRequestMessage>(new(user)))
            {
                return ContentManageResult.Exist;
            }

            var result = await NiconicoSession.ToolkitContext.Follow.User.RemoveFollowUserAsync(user.Id);

            if (result is ContentManageResult.Success)
            {
                _messenger.Send<UserFollowRemovedMessage>(new(user));
            }

            return result;
        }

        public Task<bool> IsFollowingAsync(string id)
        {
            return IsFollowingAsync(uint.Parse(id));
        }


        public Task<bool> IsFollowingAsync(uint id)
        {
            return NiconicoSession.ToolkitContext.Follow.User.IsFollowingUserAsync(id);
        }

    }

}
