#nullable enable
using Hohoema.Models.Niconico.Community;
using NiconicoToolkit.Community;
using NiconicoToolkit.Follow;

namespace Hohoema.ViewModels.Niconico.Follow;

public sealed class FollowCommunityViewModel : ICommunity
{
    public bool IsOwnedCommunity { get; }

    private readonly IFollowCommunity _followCommunity;

    public CommunityId CommunityId => _followCommunity.GlobalId;

    public string Name => _followCommunity.Name;

    public string Description => _followCommunity.Description;

    public string ThumbnailUrl => _followCommunity.ThumbnailUrl.Small.OriginalString;

    public FollowCommunityViewModel(IFollowCommunity followCommunity, bool isOwnedCommnity)
    {
        _followCommunity = followCommunity;
        IsOwnedCommunity = isOwnedCommnity;
    }
}
