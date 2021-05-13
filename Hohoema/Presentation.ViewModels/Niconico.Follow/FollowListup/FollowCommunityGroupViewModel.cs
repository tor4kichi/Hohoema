using Hohoema.Models.Domain.Niconico.Community;
using Hohoema.Models.Domain.Niconico.Follow;
using Hohoema.Models.Domain.Niconico.Follow.LoginUser;
using Hohoema.Models.UseCase.PageNavigation;
using Microsoft.Toolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Niconico.Follow
{
    public sealed class FollowCommunityGroupViewModel : FollowGroupViewModel<ICommunity>,
        IRecipient<CommunityFollowAddedMessage>,
        IRecipient<CommunityFollowRemovedMessage>,
        IDisposable
    {
        private readonly IMessenger _messenger;

        public FollowCommunityGroupViewModel(CommunityFollowProvider followProvider, uint loginUserId, PageManager pageManager, IMessenger messenger) 
            : base(FollowItemType.Community, followProvider, new FollowCommunityIncrementalSource(followProvider, loginUserId), pageManager)
        {
            _messenger = messenger;
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
    }
}
