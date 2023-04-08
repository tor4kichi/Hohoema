using Hohoema.Models.Niconico.Follow;
using Hohoema.Models.Niconico.Follow.LoginUser;
using Hohoema.Models.Niconico.Mylist;
using Hohoema.Services.Navigations;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Niconico.Follow
{
    public sealed class FolloMylistGroupViewModel : FollowGroupViewModel<IMylist>,
        IRecipient<MylistFollowAddedMessage>,
        IRecipient<MylistFollowRemovedMessage>,
        IDisposable
    {
        private readonly IMessenger _messenger;

        public FolloMylistGroupViewModel(MylistFollowProvider followProvider, PageManager pageManager, IMessenger messenger) 
            : base(FollowItemType.Mylist, followProvider, new FollowMylistIncrementalSource(followProvider), pageManager)
        {
            _messenger = messenger; 
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
}
