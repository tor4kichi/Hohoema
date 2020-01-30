using Mntone.Nico2;
using Mntone.Nico2.Users.Series;
using Mntone.Nico2.Videos.Series;
using NicoPlayerHohoema.Repository.NicoVideo;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels
{
    public sealed class UserSeriesPageViewModel : HohoemaViewModelBase, INavigatedAwareAsync
    {
        public UserSeriesPageViewModel(SeriesRepository seriesRepository)
        {
            _seriesRepository = seriesRepository;
        }

        private readonly SeriesRepository _seriesRepository;

        private List<Series> _userSeriesList;
        public List<Series> UserSeriesList
        {
            get { return _userSeriesList; }
            set { SetProperty(ref _userSeriesList, value); }
        }

        public async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            if (parameters.TryGetValue("id", out string id))
            {
                UserSeriesList = await _seriesRepository.GetUserSeriesAsync(id);
            }
        }        
    }
}
