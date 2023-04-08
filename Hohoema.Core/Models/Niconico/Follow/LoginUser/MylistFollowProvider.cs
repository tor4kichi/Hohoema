#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Hohoema.Infra;
using Hohoema.Models.Niconico.Mylist;
using NiconicoToolkit.Account;
using NiconicoToolkit.Follow;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Follow.LoginUser;

public class MylistFollowAddedMessage : ValueChangedMessage<IMylist>
{
    public MylistFollowAddedMessage(IMylist value) : base(value)
    {
    }
}

public class MylistFollowRemoveConfirmingAsyncRequestMessage : AsyncRequestMessage<bool>
{
    public MylistFollowRemoveConfirmingAsyncRequestMessage(IMylist mylist)
    {
        Target = mylist;
    }

    public IMylist Target { get; }
}


public class MylistFollowRemovedMessage : ValueChangedMessage<IMylist>
{
    public MylistFollowRemovedMessage(IMylist value) : base(value)
    {
    }
}


public sealed class MylistFollowProvider : ProviderBase, IFollowProvider<IMylist>
{
    private readonly IMessenger _messenger;

    public MylistFollowProvider(NiconicoSession niconicoSession, IMessenger messenger)
        : base(niconicoSession)
    {
        _messenger = messenger;
    }

    public async Task<FollowMylistResponse> GetFollowMylistsAsync(int sampleItemsCount = 3)
    {
        return !_niconicoSession.IsLoggedIn
            ? throw new InvalidOperationException()
            : await _niconicoSession.ToolkitContext.Follow.Mylist.GetFollowMylistsAsync(sampleItemsCount);
    }

    public async Task<ContentManageResult> AddFollowAsync(IMylist mylist)
    {
        if (!_niconicoSession.IsLoggedIn)
        {
            return ContentManageResult.Failed;
        }

        ContentManageResult result = await _niconicoSession.ToolkitContext.Follow.Mylist.AddFollowMylistAsync(mylist.PlaylistId.Id);

        if (result is ContentManageResult.Success or ContentManageResult.Exist)
        {
            _ = _messenger.Send<MylistFollowAddedMessage>(new(mylist));
        }

        return result;
    }

    public async Task<ContentManageResult> RemoveFollowAsync(IMylist mylist)
    {
        if (!_niconicoSession.IsLoggedIn)
        {
            return ContentManageResult.Failed;
        }

        if (!await _messenger.Send<MylistFollowRemoveConfirmingAsyncRequestMessage>(new(mylist)))
        {
            return ContentManageResult.Exist;
        }

        ContentManageResult result = await _niconicoSession.ToolkitContext.Follow.Mylist.RemoveFollowMylistAsync(mylist.PlaylistId.Id);

        if (result is ContentManageResult.Success)
        {
            _ = _messenger.Send<MylistFollowRemovedMessage>(new(mylist));
        }

        return result;
    }

    public async Task<bool> IsFollowingAsync(string id)
    {
        long numberId = long.Parse(id);
        FollowMylistResponse res = await _niconicoSession.ToolkitContext.Follow.Mylist.GetFollowMylistsAsync(0);
        return res.Data.Mylists.Any(x => x.Id == numberId);
    }

    //Task<ContentManageResult> IFollowProvider<IMylist>.AddFollowAsync(IMylist followable) => AddFollowAsync(followable.Id);

    //Task<ContentManageResult> IFollowProvider<IMylist>.RemoveFollowAsync(IMylist followable) => RemoveFollowAsync(followable.Id);

    Task<bool> IFollowProvider<IMylist>.IsFollowingAsync(IMylist followable)
    {
        return IsFollowingAsync(followable.PlaylistId.Id);
    }
}
