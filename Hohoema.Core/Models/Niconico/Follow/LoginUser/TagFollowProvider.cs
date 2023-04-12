#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Hohoema.Infra;
using Hohoema.Models.Niconico.Video;
using NiconicoToolkit.Account;
using NiconicoToolkit.Follow;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Follow.LoginUser;

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

        FollowTagsResponse res = await _niconicoSession.ToolkitContext.Follow.Tag.GetFollowTagsAsync();

        return res.Data.Tags;
    }

    Task<bool> IFollowProvider<ITag>.IsFollowingAsync(ITag followable)
    {
        return IsFollowingAsync(followable.Tag);
    }

    public async Task<ContentManageResult> AddFollowAsync(ITag tag)
    {
        if (!_niconicoSession.IsLoggedIn)
        {
            return ContentManageResult.Failed;
        }

        ContentManageResult result = await _niconicoSession.ToolkitContext.Follow.Tag.AddFollowTagAsync(tag.Tag);

        if (result is ContentManageResult.Success or ContentManageResult.Exist)
        {
            _ = _messenger.Send<TagFollowAddedMessage>(new(tag));
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

        ContentManageResult result = await _niconicoSession.ToolkitContext.Follow.Tag.RemoveFollowTagAsync(tag.Tag);

        if (result is ContentManageResult.Success)
        {
            _ = _messenger.Send<TagFollowRemovedMessage>(new(tag));
        }

        return result;
    }

    public async Task<bool> IsFollowingAsync(string tag)
    {
        FollowTagsResponse res = await _niconicoSession.ToolkitContext.Follow.Tag.GetFollowTagsAsync();
        return res.Data.Tags.Any(t => t.Name == tag);
    }

}
