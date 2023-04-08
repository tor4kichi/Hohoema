using Hohoema.Models.Niconico.Channel;
using Hohoema.Models.Niconico.Follow;
using Hohoema.Models.Niconico.Follow.LoginUser;
using Hohoema.Services.Navigations;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Niconico.Follow
{
    public sealed class FollowChannelGroupViewModel : FollowGroupViewModel<IChannel>,
        IRecipient<ChannelFollowAddedMessage>,
        IRecipient<ChannelFollowRemovedMessage>,
        IDisposable
    {
        private readonly IMessenger _messenger;

        public FollowChannelGroupViewModel(ChannelFollowProvider followProvider, PageManager pageManager, IMessenger messenger) 
            : base(FollowItemType.Channel, followProvider, new FollowChannelIncrementalSource(followProvider), pageManager)
        {
            _messenger = messenger;
            _messenger.RegisterAll(this);
        }

        public override void Dispose()
        {
            base.Dispose();
            _messenger.UnregisterAll(this);
        }

        void IRecipient<ChannelFollowAddedMessage>.Receive(ChannelFollowAddedMessage message)
        {
            Items.Insert(0, message.Value);
        }

        void IRecipient<ChannelFollowRemovedMessage>.Receive(ChannelFollowRemovedMessage message)
        {
            Items.Remove(message.Value);
        }

    }
}
