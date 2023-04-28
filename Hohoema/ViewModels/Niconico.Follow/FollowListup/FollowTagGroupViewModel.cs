#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Niconico.Follow;
using Hohoema.Models.Niconico.Follow.LoginUser;
using Hohoema.Models.Niconico.Video;
using System;

namespace Hohoema.ViewModels.Niconico.Follow;

public sealed class FollowTagGroupViewModel : FollowGroupViewModel<ITag>,
    IRecipient<TagFollowAddedMessage>,
    IRecipient<TagFollowRemovedMessage>,
    IDisposable
{
    public FollowTagGroupViewModel(TagFollowProvider followProvider, IMessenger messenger) 
        : base(FollowItemType.Tag, followProvider, new FollowTagIncrementalSource(followProvider), messenger)
    {
        _messenger.RegisterAll(this);
    }

    public override void Dispose()
    {
        base.Dispose();
        _messenger.UnregisterAll(this);
    }

    void IRecipient<TagFollowAddedMessage>.Receive(TagFollowAddedMessage message)
    {
        Items.Insert(0, message.Value);
    }

    void IRecipient<TagFollowRemovedMessage>.Receive(TagFollowRemovedMessage message)
    {
        Items.Remove(message.Value);
    }
}
