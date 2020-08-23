using I18NPortable;
using Mntone.Nico2;
using Mntone.Nico2.Live.Watch.Crescendo;
using Mntone.Nico2.Users.Series;
using Mntone.Nico2.Users.User;
using Mntone.Nico2.Videos.Series;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models.Provider;
using NicoPlayerHohoema.Repository.NicoVideo;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.Services.Page;
using NicoPlayerHohoema.UseCase.Page.Commands;
using NicoPlayerHohoema.UseCase.Playlist;
using NicoPlayerHohoema.ViewModels.Subscriptions;
using Prism.Commands;
using Prism.Navigation;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static Mntone.Nico2.Users.User.UserDetailResponse;

namespace NicoPlayerHohoema.ViewModels
{
    public sealed class UserSeriesPageViewModel : HohoemaViewModelBase, INavigatedAwareAsync, ITitleUpdatablePage, IPinablePage
    {
        public HohoemaPin GetPin()
        {
            return new HohoemaPin()
            {
                Label = User.Label,
                PageType = HohoemaPageType.UserSeries,
                Parameter = $"id={User.Id}"
            };
        }

        public IObservable<string> GetTitleObservable()
        {
            return this.ObserveProperty(x => x.User)
                .Select(x => x == null ? null : "UserSeriesListWithOwnerName".Translate(x.Label));
        }



        public UserSeriesPageViewModel(
            SeriesRepository seriesRepository,
            UserProvider userProvider,
            PageManager pageManager,
            HohoemaPlaylist hohoemaPlaylist,
            AddSubscriptionCommand addSubscriptionCommand
            )
        {
            _seriesRepository = seriesRepository;
            _userProvider = userProvider;
            _pageManager = pageManager;
            HohoemaPlaylist = hohoemaPlaylist;
            AddSubscriptionCommand = addSubscriptionCommand;
        }

        private readonly SeriesRepository _seriesRepository;
        private readonly UserProvider _userProvider;
        private readonly PageManager _pageManager;


        private List<UserSeriesItemViewModel> _userSeriesList;
        public List<UserSeriesItemViewModel> UserSeriesList
        {
            get { return _userSeriesList; }
            set { SetProperty(ref _userSeriesList, value); }
        }


        private UserViewModel _user;
        public UserViewModel User
        {
            get { return _user; }
            set { SetProperty(ref _user, value); }
        }


        private DelegateCommand<UserSeriesItemViewModel> _OpenSeriesVideoPageCommand;
        public DelegateCommand<UserSeriesItemViewModel> OpenSeriesVideoPageCommand =>
            _OpenSeriesVideoPageCommand ?? (_OpenSeriesVideoPageCommand = new DelegateCommand<UserSeriesItemViewModel>(ExecuteOpenSeriesVideoPageCommand));

        public HohoemaPlaylist HohoemaPlaylist { get; }
        public AddSubscriptionCommand AddSubscriptionCommand { get; }

        void ExecuteOpenSeriesVideoPageCommand(UserSeriesItemViewModel parameter)
        {
            _pageManager.OpenPage(HohoemaPageType.Series, $"id={parameter.Id}");
        }

        public async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            if (parameters.TryGetValue("id", out string id))
            {
                var serieses = await _seriesRepository.GetUserSeriesAsync(id);
                UserSeriesList = serieses.Select(x => new UserSeriesItemViewModel(x)).ToList();

                
//                var userInfo = await _userProvider.GetUserDetail(id);
//                User = new UserViewModel(userInfo);
            }
        }

    }

    public class UserViewModel : IUser
    {
        private readonly UserDetails _userDetail;

        public UserViewModel(UserDetails userDetail)
        {
            _userDetail = userDetail;
        }

        public string Id => _userDetail.User.Id.ToString();

        public string Label => _userDetail.User.Nickname;

        public string IconUrl => _userDetail.User.Icons.Small.OriginalString;
    }


    public class UserSeriesItemViewModel : ISeries
    {
        private readonly UserSeries _userSeries;

        public UserSeriesItemViewModel(UserSeries userSeries)
        {
            _userSeries = userSeries;
        }

        public string Id => _userSeries.Id.ToString();

        public string Title => _userSeries.Title;

        public bool IsListed => _userSeries.IsListed;

        public string Description => _userSeries.Description;

        public string ThumbnailUrl => _userSeries.ThumbnailUrl.OriginalString;

        public int ItemsCount => (int)_userSeries.ItemsCount;

        public string ProviderType => _userSeries.Owner.Type;

        public string ProviderId => _userSeries.Owner.Id;        
    }

    
}
