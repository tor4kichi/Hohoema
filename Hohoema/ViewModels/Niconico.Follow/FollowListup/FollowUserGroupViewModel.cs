﻿using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Follow;
using Hohoema.Models.Niconico.Follow.LoginUser;
using System;

namespace Hohoema.ViewModels.Niconico.Follow;

public sealed class FollowUserGroupViewModel : FollowGroupViewModel<IUser>,
    IRecipient<UserFollowAddedMessage>,
    IRecipient<UserFollowRemovedMessage>,
    IDisposable
{
    private readonly IMessenger _messenger;

    public FollowUserGroupViewModel(UserFollowProvider followProvider, PageManager pageManager, IMessenger messenger) 
        : base(FollowItemType.User, followProvider, new FollowUserIncrementalSource(followProvider), pageManager)
    {
        _messenger = messenger;
        _messenger.RegisterAll(this);
    }

    public override void Dispose()
    {
        base.Dispose();
        _messenger.UnregisterAll(this);
    }

    void IRecipient<UserFollowAddedMessage>.Receive(UserFollowAddedMessage message)
    {
        Items.Insert(0, message.Value);
    }

    void IRecipient<UserFollowRemovedMessage>.Receive(UserFollowRemovedMessage message)
    {
        Items.Remove(message.Value);
    }
}
