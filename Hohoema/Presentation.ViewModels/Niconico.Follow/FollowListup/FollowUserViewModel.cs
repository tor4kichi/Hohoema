using System;
using Hohoema.Models.Domain.Niconico;
using NiconicoToolkit.Follow;
using NiconicoToolkit.User;

namespace Hohoema.Presentation.ViewModels.Niconico.Follow
{
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


}
