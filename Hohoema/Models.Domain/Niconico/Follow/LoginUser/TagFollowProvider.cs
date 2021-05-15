using Mntone.Nico2;
using Mntone.Nico2.Users.Follow;
using Hohoema.Models.Infrastructure;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Hohoema.Models.Domain.Niconico.Video;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using Microsoft.Toolkit.Mvvm.Messaging;

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
            if (!NiconicoSession.IsLoggedIn)
            {
                return new List<FollowTagsResponse.Tag>();
            }

            var res = await ContextActionWithPageAccessWaitAsync(context =>
            {
                return context.User.GetFollowTagsAsync();
            });

            return res.Data.Tags;
        }

        Task<bool> IFollowProvider<ITag>.IsFollowingAsync(ITag followable) => IsFollowingAsync(followable.Tag);

        public async Task<ContentManageResult> AddFollowAsync(ITag tag)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            var result = await ContextActionAsync(context =>
            {
                return context.User.AddFollowTagAsync(tag.Tag);
            });

            if (result is ContentManageResult.Success or ContentManageResult.Exist)
            {
                _messenger.Send<TagFollowAddedMessage>(new(tag));
            }

            return result;
        }

        public async Task<ContentManageResult> RemoveFollowAsync(ITag tag)
        {
            if (!NiconicoSession.IsLoggedIn)
            {
                return ContentManageResult.Failed;
            }

            if (!await _messenger.Send<TagFollowRemoveConfirmingAsyncRequestMessage>(new(tag)))
            {
                return ContentManageResult.Exist;
            }

            var result = await ContextActionAsync(context =>
            {
                return context.User.RemoveFollowTagAsync(tag.Tag);
            });

            if (result is ContentManageResult.Success)
            {
                _messenger.Send<TagFollowRemovedMessage>(new(tag));
            }

            return result;
        }

        public Task<bool> IsFollowingAsync(string tag)
        {
            return ContextActionAsync(async context =>
            {
//                return context.User.IsFollowingTagAsync(tag);

                var res = await context.User.GetFollowTagsAsync();
                return res.Data.Tags.Any(t => t.Name == tag);
            });
        }

    }

}
