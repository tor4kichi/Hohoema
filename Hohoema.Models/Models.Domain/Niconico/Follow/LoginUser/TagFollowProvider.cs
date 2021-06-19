using Hohoema.Models.Infrastructure;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Hohoema.Models.Domain.Niconico.Video;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using Microsoft.Toolkit.Mvvm.Messaging;
using NiconicoToolkit.Follow;
using NiconicoToolkit.Account;

namespace Hohoema.Models.Domain.Niconico.Follow.LoginUser
{
    public class TagFollowAddedMessage : ValueChangedMessage<ITag>
    {
        public TagFollowAddedMessage(ITag value) : base(value)
        {
        }
    }

    public class TagFollowRemoveConfirmingAsyncRequestMessage : AsyncRequestMessage<bool>
    {
        public TagFollowRemoveConfirmingAsyncRequestMessage(ITag tag)
        {
            Target = tag;
        }

        public ITag Target { get; }
    }


    public class TagFollowRemovedMessage : ValueChangedMessage<ITag>
    {
        public TagFollowRemovedMessage(ITag value) : base(value)
        {
        }
    }


    public sealed class TagFollowProvider : ProviderBase, IFollowProvider<ITag>
    {
        private readonly IMessenger _messenger;

        public TagFollowProvider(NiconicoSession niconicoSession, IMessenger messenger)
            : base(niconicoSession)
        {
            _messenger = messenger;
        }


        public async Task<List<FollowTagsResponse.Tag>> GetAllAsync()
        {
            if (!_niconicoSession.IsLoggedIn)
            {
                return new List<FollowTagsResponse.Tag>();
            }

            var res = await _niconicoSession.ToolkitContext.Follow.Tag.GetFollowTagsAsync();

            return res.Data.Tags;
        }

        Task<bool> IFollowProvider<ITag>.IsFollowingAsync(ITag followable) => IsFollowingAsync(followable.Tag);

        public async Task<ContentManageResult> AddFollowAsync(ITag tag)
        {
            if (!_niconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            var result = await _niconicoSession.ToolkitContext.Follow.Tag.AddFollowTagAsync(tag.Tag);

            if (result is ContentManageResult.Success or ContentManageResult.Exist)
            {
                _messenger.Send<TagFollowAddedMessage>(new(tag));
            }

            return result;
        }

        public async Task<ContentManageResult> RemoveFollowAsync(ITag tag)
        {
            if (!_niconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            if (!await _messenger.Send<TagFollowRemoveConfirmingAsyncRequestMessage>(new(tag)))
            {
                return ContentManageResult.Exist;
            }

            var result = await _niconicoSession.ToolkitContext.Follow.Tag.RemoveFollowTagAsync(tag.Tag);

            if (result is ContentManageResult.Success)
            {
                _messenger.Send<TagFollowRemovedMessage>(new(tag));
            }

            return result;
        }

        public async Task<bool> IsFollowingAsync(string tag)
        {
            var res = await _niconicoSession.ToolkitContext.Follow.Tag.GetFollowTagsAsync();
            return res.Data.Tags.Any(t => t.Name == tag);
        }

    }

}
