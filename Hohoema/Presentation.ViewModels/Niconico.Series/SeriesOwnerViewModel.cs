using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Video;
using NiconicoToolkit.User;

namespace Hohoema.Presentation.ViewModels.Niconico.Series
{
    public class SeriesOwnerViewModel : IUser
    {
        private readonly NicoVideoOwner _userDetail;

        public SeriesOwnerViewModel(NicoVideoOwner userDetail)
        {
            _userDetail = userDetail;
        }

        public UserId UserId => (_userDetail as IUser).UserId;

        public string Nickname => (_userDetail as IUser).Nickname;

        public string IconUrl => (_userDetail as IUser).IconUrl;
    }

    
}
