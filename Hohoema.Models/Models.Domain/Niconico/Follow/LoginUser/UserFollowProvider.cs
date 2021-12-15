using Hohoema.Models.Infrastructure;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using Microsoft.Toolkit.Mvvm.Messaging;
using NiconicoToolkit.Follow;
using NiconicoToolkit.Account;
using NiconicoToolkit.User;

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
            if (!_niconicoSession.IsLoggedIn)
            {
                return new List<UserFollowItem>();
            }

            const int PageSize = 100;

            List<UserFollowItem> followers = new List<UserFollowItem>();
            var res = await _niconicoSession.ToolkitContext.Follow.User.GetFollowUsersAsync(PageSize);

            followers.AddRange(res.Data.Items);
            while (res.Data.Summary.HasNext)
            {
                res = await _niconicoSession.ToolkitContext.Follow.User.GetFollowUsersAsync(PageSize, res);

                followers.AddRange(res.Data.Items);
            }

            return followers;
        }

        public Task<FollowUsersResponse> GetItemsAsync(int pageSize, FollowUsersResponse lastRes = null)
        {
            if (!_niconicoSession.IsLoggedIn)
            {
                throw new InvalidOperationException();
            }

            return _niconicoSession.ToolkitContext.Follow.User.GetFollowUsersAsync(pageSize, lastRes);
        }

        Task<bool> IFollowProvider<IUser>.IsFollowingAsync(IUser followable) => followable is null ? Task.FromResult(false) : IsFollowingAsync(followable.UserId);

        public async Task<ContentManageResult> AddFollowAsync(IUser user)
        {
            if (!_niconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            var result = await _niconicoSession.ToolkitContext.Follow.User.AddFollowUserAsync(user.UserId);

            if (result is ContentManageResult.Success or ContentManageResult.Exist)
            {
                _messenger.Send<UserFollowAddedMessage>(new(user));
            }

            return result;
        }

        public async Task<ContentManageResult> RemoveFollowAsync(IUser user)
        {
            if (!_niconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }
            
            if (!await _messenger.Send<UserFollowRemoveConfirmingAsyncRequestMessage>(new(user)))
            {
                return ContentManageResult.Exist;
            }

            var result = await _niconicoSession.ToolkitContext.Follow.User.RemoveFollowUserAsync(user.UserId);

            if (result is ContentManageResult.Success)
            {
                _messenger.Send<UserFollowRemovedMessage>(new(user));
            }

            return result;
        }


        public Task<bool> IsFollowingAsync(UserId id)
        {
            return _niconicoSession.ToolkitContext.Follow.User.IsFollowingUserAsync(id);
        }

    }

}
