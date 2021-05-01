using static Mntone.Nico2.Users.User.UserDetailResponse;
using Hohoema.Models.Domain.Niconico;

namespace Hohoema.Presentation.ViewModels.Niconico.Series
{
    public class SeriesOwnerViewModel : IUser
    {
        private readonly UserDetails _userDetail;

        public SeriesOwnerViewModel(UserDetails userDetail)
        {
            _userDetail = userDetail;
        }

        public string Id => _userDetail.User.Id.ToString();

        public string Label => _userDetail.User.Nickname;

        public string IconUrl => _userDetail.User.Icons.Small.OriginalString;
    }

    
}
