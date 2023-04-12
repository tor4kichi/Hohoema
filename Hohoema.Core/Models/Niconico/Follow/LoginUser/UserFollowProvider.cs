#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Hohoema.Infra;
using NiconicoToolkit.Account;
using NiconicoToolkit.Follow;
using NiconicoToolkit.User;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Follow.LoginUser;

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

        List<UserFollowItem> followers = new();
        FollowUsersResponse res = await _niconicoSession.ToolkitContext.Follow.User.GetFollowUsersAsync(PageSize);

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
        return !_niconicoSession.IsLoggedIn
            ? throw new InvalidOperationException()
            : _niconicoSession.ToolkitContext.Follow.User.GetFollowUsersAsync(pageSize, lastRes);
    }

    Task<bool> IFollowProvider<IUser>.IsFollowingAsync(IUser followable)
    {
        return followable is null ? Task.FromResult(false) : IsFollowingAsync(followable.UserId);
    }

    public async Task<ContentManageResult> AddFollowAsync(IUser user)
    {
        if (!_niconicoSession.IsLoggedIn)
        {
            return ContentManageResult.Failed;
        }

        ContentManageResult result = await _niconicoSession.ToolkitContext.Follow.User.AddFollowUserAsync(user.UserId);

        if (result is ContentManageResult.Success or ContentManageResult.Exist)
        {
            _ = _messenger.Send<UserFollowAddedMessage>(new(user));
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

        ContentManageResult result = await _niconicoSession.ToolkitContext.Follow.User.RemoveFollowUserAsync(user.UserId);

        if (result is ContentManageResult.Success)
        {
            _ = _messenger.Send<UserFollowRemovedMessage>(new(user));
        }

        return result;
    }


    public Task<bool> IsFollowingAsync(UserId id)
    {
        return _niconicoSession.ToolkitContext.Follow.User.IsFollowingUserAsync(id);
    }

}
