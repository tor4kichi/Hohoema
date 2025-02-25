#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Niconico.Follow;
using Hohoema.Models.Niconico.Follow.LoginUser;
using Hohoema.Models.Niconico.Mylist;
using System;

namespace Hohoema.ViewModels.Niconico.Follow;

public sealed class FolloMylistGroupViewModel : FollowGroupViewModel<IMylist>,
    IRecipient<MylistFollowAddedMessage>,
    IRecipient<MylistFollowRemovedMessage>,
    IDisposable
{
    public FolloMylistGroupViewModel(MylistFollowProvider followProvider, IMessenger messenger) 
        : base(FollowItemType.Mylist, followProvider, new FollowMylistIncrementalSource(followProvider), messenger)
    {        
        _messenger.RegisterAll(this);
    }

    public override void Dispose()
    {
        base.Dispose();
        _messenger.UnregisterAll(this);
    }

    void IRecipient<MylistFollowAddedMessage>.Receive(MylistFollowAddedMessage message)
    {
        Items.Insert(0, message.Value);
    }

    void IRecipient<MylistFollowRemovedMessage>.Receive(MylistFollowRemovedMessage message)
    {
        Items.Remove(message.Value);
    }
}
