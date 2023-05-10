#nullable enable
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Niconico.Community;
using Hohoema.Models.Niconico.Follow;
using Hohoema.Models.Niconico.Follow.LoginUser;
using System;

namespace Hohoema.ViewModels.Niconico.Follow;

public sealed class FollowCommunityGroupViewModel : FollowGroupViewModel<ICommunity>,
    IRecipient<CommunityFollowAddedMessage>,
    IRecipient<CommunityFollowRemovedMessage>,
    IDisposable
{
    public FollowCommunityGroupViewModel(CommunityFollowProvider followProvider, uint loginUserId, IMessenger messenger) 
        : base(FollowItemType.Community, followProvider, new FollowCommunityIncrementalSource(followProvider, loginUserId), messenger)
    {
        _messenger.RegisterAll(this);
    }

    public override void Dispose()
    {
        base.Dispose();
        _messenger.UnregisterAll(this);
    }

    void IRecipient<CommunityFollowAddedMessage>.Receive(CommunityFollowAddedMessage message)
    {
        Items.Insert(0, message.Value);
    }

    void IRecipient<CommunityFollowRemovedMessage>.Receive(CommunityFollowRemovedMessage message)
    {
        Items.Remove(message.Value);
    }

    private RelayCommand<ICommunity> _OpenPageCommand;
    public override RelayCommand<ICommunity> OpenPageCommand =>
        _OpenPageCommand ??= new RelayCommand<ICommunity>(item => 
        {
//                await _messenger.OpenPageWithIdAsync(Models.PageNavigation.HohoemaPageType.Community, item.Id);
        });
}
