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

        public string Id => _userDetail.OwnerId.ToString();

        public string Label => _userDetail.ScreenName;

        public string IconUrl => _userDetail.IconUrl;
    }

    
}
