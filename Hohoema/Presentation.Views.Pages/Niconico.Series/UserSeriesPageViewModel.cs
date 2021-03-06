﻿using Hohoema.Models.Domain.Niconico.User;
using Hohoema.Models.Domain.Niconico.Video.Series;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Pins;
using Hohoema.Models.UseCase.Playlist;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Presentation.ViewModels.Niconico.Series;
using Hohoema.Presentation.ViewModels.Subscriptions;
using I18NPortable;
using NiconicoToolkit.User;
using Prism.Commands;
using Prism.Navigation;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Pages.Niconico.Series
{
    public sealed class UserSeriesPageViewModel : HohoemaPageViewModelBase, INavigatedAwareAsync, ITitleUpdatablePage, IPinablePage
    {
        public HohoemaPin GetPin()
        {
            return new HohoemaPin()
            {
                Label = User.Nickname,
                PageType = HohoemaPageType.UserSeries,
                Parameter = $"id={User.UserId}"
            };
        }

        public IObservable<string> GetTitleObservable()
        {
            return this.ObserveProperty(x => x.User)
                .Select(x => x == null ? null : "UserSeriesListWithOwnerName".Translate(x.Nickname));
        }



        public UserSeriesPageViewModel(
            SeriesProvider seriesRepository,
            UserProvider userProvider,
            PageManager pageManager,
            AddSubscriptionCommand addSubscriptionCommand
            )
        {
            _seriesProvider = seriesRepository;
            _userProvider = userProvider;
            _pageManager = pageManager;
            AddSubscriptionCommand = addSubscriptionCommand;
        }

        private readonly SeriesProvider _seriesProvider;
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

        public AddSubscriptionCommand AddSubscriptionCommand { get; }

        void ExecuteOpenSeriesVideoPageCommand(UserSeriesItemViewModel parameter)
        {
            _pageManager.OpenPage(HohoemaPageType.Series, $"id={parameter.Id}");
        }

        public async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            UserId? userId = null;
            if (parameters.TryGetValue("id", out string id))
            {
                userId = id;
            }
            else if (parameters.TryGetValue("id", out string strUserid))
            {
                userId = strUserid;
            }
            else if (parameters.TryGetValue("id", out UserId justUserid))
            {
                userId = justUserid;
            }

            if (userId == null)
            {
                return;
            }

            NowUpdating = true;
            try
            {
                var serieses = await _seriesProvider.GetUserSeriesAsync(userId.Value);
                UserSeriesList = serieses.Select(x => new UserSeriesItemViewModel(x)).ToList();

                var userInfo = await _userProvider.GetUserInfoAsync(userId.Value);
                if (userInfo != null)
                {
                    User = new SeriesOwnerViewModel(userInfo);
                }
                else
                {

                }
            }
            finally
            {
                NowUpdating = false;
            }
        }

    }

    
}
