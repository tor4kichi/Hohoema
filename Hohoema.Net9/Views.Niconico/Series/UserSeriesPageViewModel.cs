﻿#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Niconico.User;
using Hohoema.Models.Niconico.Video.Series;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Pins;
using Hohoema.Models.Playlist;
using Hohoema.ViewModels.Niconico.Series;
using Hohoema.ViewModels.Niconico.Video.Commands;
using Hohoema.ViewModels.Subscriptions;
using I18NPortable;
using NiconicoToolkit.User;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Pages.Niconico.Series;

public partial class UserSeriesPageViewModel 
    : HohoemaPageViewModelBase
    , ITitleUpdatablePage
    , IPinablePage
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
        IMessenger messenger,
        SeriesProvider seriesRepository,
        UserProvider userProvider,        
        AddSubscriptionCommand addSubscriptionCommand,
        PlaylistPlayAllCommand playlistPlayAllCommand
        )
    {
        _messenger = messenger;
        _seriesProvider = seriesRepository;
        _userProvider = userProvider;
        AddSubscriptionCommand = addSubscriptionCommand;
        PlaylistPlayAllCommand = playlistPlayAllCommand;
    }

    private readonly IMessenger _messenger;
    private readonly SeriesProvider _seriesProvider;
    private readonly UserProvider _userProvider;


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

    public AddSubscriptionCommand AddSubscriptionCommand { get; }
    public PlaylistPlayAllCommand PlaylistPlayAllCommand { get; }

    [ObservableProperty]
    public partial IPlaylist Playlist { get; set; }

    private RelayCommand<UserSeriesItemViewModel> _OpenSeriesVideoPageCommand;
    public RelayCommand<UserSeriesItemViewModel> OpenSeriesVideoPageCommand =>
        _OpenSeriesVideoPageCommand ?? (_OpenSeriesVideoPageCommand = new RelayCommand<UserSeriesItemViewModel>(ExecuteOpenSeriesVideoPageCommand));

    void ExecuteOpenSeriesVideoPageCommand(UserSeriesItemViewModel parameter)
    {
        _ = _messenger.OpenPageWithIdAsync(HohoemaPageType.Series, parameter.Id);
    }

    public override async Task OnNavigatedToAsync(INavigationParameters parameters)
    {
        await base.OnNavigatedToAsync(parameters);

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
        catch
        {
            UserSeriesList = null;
            User = null;
        }
        finally
        {
            NowUpdating = false;
        }
    }

}


