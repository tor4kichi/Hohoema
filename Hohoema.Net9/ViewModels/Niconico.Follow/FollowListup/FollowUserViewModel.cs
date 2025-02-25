#nullable enable
using Hohoema.Models.Niconico;
using NiconicoToolkit.Follow;
using NiconicoToolkit.User;
using System;

namespace Hohoema.ViewModels.Niconico.Follow;

public class FollowUserViewModel : IUser
{
    private readonly UserFollowItem _userFollowItem;

    public FollowUserViewModel(UserFollowItem userFollowItem)
    {
        _userFollowItem = userFollowItem;
    }

    public UserId UserId => _userFollowItem.Id;


    public string Nickname => _userFollowItem.Nickname;

    public Uri IconUrl_Small => _userFollowItem.Icons.Small;
    public Uri IconUrl_Large => _userFollowItem.Icons.Large;

    public string IconUrl => _userFollowItem.Icons.Small.OriginalString;

    public string Description => _userFollowItem.StrippedDescription;
}
