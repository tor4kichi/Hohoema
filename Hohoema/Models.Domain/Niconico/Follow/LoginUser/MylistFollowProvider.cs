using Hohoema.Models.Infrastructure;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using Hohoema.Models.Domain.Niconico.Mylist;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using Microsoft.Toolkit.Mvvm.Messaging;
using NiconicoToolkit.Follow;
using NiconicoToolkit.Account;

namespace Hohoema.Models.Domain.Niconico.Follow.LoginUser
{
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

        public async Task<FollowMylistResponse> GetFollowMylistsAsync(uint sampleItemsCount = 3)
        {
            if (!_niconicoSession.IsLoggedIn)
            {
                throw new InvalidOperationException();
            }


            return await _niconicoSession.ToolkitContext.Follow.Mylist.GetFollowMylistsAsync(sampleItemsCount);
        }

        public async Task<ContentManageResult> AddFollowAsync(IMylist mylist)
        {
            if (!_niconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            var result = await _niconicoSession.ToolkitContext.Follow.Mylist.AddFollowMylistAsync(mylist.Id);

            if (result is ContentManageResult.Success or ContentManageResult.Exist)
            {
                _messenger.Send<MylistFollowAddedMessage>(new (mylist));
            }

            return result;
        }

        public async Task<ContentManageResult> RemoveFollowAsync(IMylist mylist)
        {
            if (!_niconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            if (!await _messenger.Send<MylistFollowRemoveConfirmingAsyncRequestMessage>(new (mylist))) 
            {
                return ContentManageResult.Exist; 
            }

            var result = await _niconicoSession.ToolkitContext.Follow.Mylist.RemoveFollowMylistAsync(mylist.Id);

            if (result is ContentManageResult.Success)
            {
                _messenger.Send<MylistFollowRemovedMessage>(new (mylist));
            }

            return result;
        }

        public Task<bool> IsFollowingAsync(string id)
        {
            return ContextActionAsync(async context =>
            {
                var numberId = long.Parse(id);
                var res = await context.User.GetFollowMylistsAsync(0);
                return res.Data.Mylists.Any(x => x.Id == numberId);
            });
        }

        //Task<ContentManageResult> IFollowProvider<IMylist>.AddFollowAsync(IMylist followable) => AddFollowAsync(followable.Id);

        //Task<ContentManageResult> IFollowProvider<IMylist>.RemoveFollowAsync(IMylist followable) => RemoveFollowAsync(followable.Id);

        Task<bool> IFollowProvider<IMylist>.IsFollowingAsync(IMylist followable) => IsFollowingAsync(followable.Id);
    }

}
