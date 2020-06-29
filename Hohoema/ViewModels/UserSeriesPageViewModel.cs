using I18NPortable;
using Mntone.Nico2;
using Mntone.Nico2.Live.Watch.Crescendo;
using Mntone.Nico2.Users.Series;
using Mntone.Nico2.Users.User;
using Mntone.Nico2.Videos.Series;
using Hohoema.Interfaces;
using Hohoema.Models.Provider;
using Hohoema.Repository.NicoVideo;
using Hohoema.Services;
using Hohoema.ViewModels.Pages;
using Hohoema.UseCase.Page.Commands;
using Hohoema.UseCase.Playlist;
using Hohoema.ViewModels.Subscriptions;
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

namespace Hohoema.ViewModels
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

                var userInfo = await _userProvider.GetUserDetail(id);
                User = new UserViewModel(userInfo);
            }
        }

    }

    public class UserViewModel : IUser
    {
        private readonly UserDetail _userDetail;

        public UserViewModel(UserDetail userDetail)
        {
            _userDetail = userDetail;
        }

        public string Id => _userDetail.UserId;

        public string Label => _userDetail.Nickname;

        public string IconUrl => _userDetail.ThumbnailUri;
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

        public string ThumbnailUrl => _userSeries.ThumbnailUrl;

        public int ItemsCount => _userSeries.ItemsCount;

        public SeriesProviderType ProviderType => _userSeries.Owner.ProviderType;

        public string ProviderId => _userSeries.Owner.Id;        
    }

    
}
