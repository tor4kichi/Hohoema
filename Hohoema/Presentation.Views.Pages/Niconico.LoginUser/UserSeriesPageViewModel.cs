using I18NPortable;
using Hohoema.Presentation.Services.Page;
using Hohoema.Models.UseCase.NicoVideos;
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
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Niconico.User;
using Hohoema.Presentation.ViewModels.Subscriptions;
using Hohoema.Presentation.ViewModels.Niconico.Series;

namespace Hohoema.Presentation.ViewModels.Pages.Niconico.LoginUser
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
            private set { SetProperty(ref _userSeriesList, value); }
        }


        private SeriesOwnerViewModel _user;
        public SeriesOwnerViewModel User
        {
            get { return _user; }
            private set { SetProperty(ref _user, value); }
        }

        private bool _nowUpdating;
        public bool NowUpdating
        {
            get { return _nowUpdating; }
            set { SetProperty(ref _nowUpdating, value); }
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
            NowUpdating = true;
            try
            {
                if (parameters.TryGetValue("id", out string id))
                {
                    var serieses = await _seriesRepository.GetUserSeriesAsync(id);
                    UserSeriesList = serieses.Select(x => new UserSeriesItemViewModel(x)).ToList();


                    var userInfo = await _userProvider.GetUserDetail(id);
                    User = new SeriesOwnerViewModel(userInfo);
                }
            }
            finally
            {
                NowUpdating = false;
            }
        }

    }

    
}
