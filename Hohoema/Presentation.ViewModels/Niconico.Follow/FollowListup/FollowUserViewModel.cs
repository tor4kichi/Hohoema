using System;
using Hohoema.Models.Domain.Niconico;
using Mntone.Nico2.Users.Follow;

namespace Hohoema.Presentation.ViewModels.Niconico.Follow
{
    public class FollowUserViewModel : IUser
    {
        private readonly UserFollowItem _userFollowItem;

        public FollowUserViewModel(UserFollowItem userFollowItem)
        {
            _userFollowItem = userFollowItem;
        }

        private string _Id;
        public string Id => _Id ??= _userFollowItem.Id.ToString();


        public string Label => _userFollowItem.Nickname;

        public Uri IconUrl_Small => _userFollowItem.Icons.Small;
        public Uri IconUrl_Large => _userFollowItem.Icons.Large;

        public string IconUrl => _userFollowItem.Icons.Small.OriginalString;

        public string Description => _userFollowItem.StrippedDescription;
    }


}
