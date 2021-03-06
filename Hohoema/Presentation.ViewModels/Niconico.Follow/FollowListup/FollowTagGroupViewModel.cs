﻿using Hohoema.Models.Domain.Niconico.Follow;
using Hohoema.Models.Domain.Niconico.Follow.LoginUser;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.UseCase.PageNavigation;
using Microsoft.Toolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Niconico.Follow
{
    public sealed class FollowTagGroupViewModel : FollowGroupViewModel<ITag>,
        IRecipient<TagFollowAddedMessage>,
        IRecipient<TagFollowRemovedMessage>,
        IDisposable
    {
        private readonly IMessenger _messenger;

        public FollowTagGroupViewModel(TagFollowProvider followProvider, PageManager pageManager, IMessenger messenger) 
            : base(FollowItemType.Tag, followProvider, new FollowTagIncrementalSource(followProvider), pageManager)
        {
            _messenger = messenger;
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
}
