﻿#nullable enable
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Infra;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Follow.LoginUser;
using Hohoema.Models.Niconico.Mylist;
using Hohoema.Models.Niconico.User;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Pins;
using Hohoema.Models.Subscriptions;
using Hohoema.Services;
using Hohoema.Services.Niconico;
using Hohoema.Services.Playlist;
using Hohoema.ViewModels.Navigation.Commands;
using Hohoema.ViewModels.Niconico.Follow;
using Hohoema.ViewModels.Niconico.Share;
using Hohoema.ViewModels.Niconico.Video.Commands;
using Hohoema.ViewModels.Subscriptions;
using Hohoema.ViewModels.VideoListPage;
using NiconicoToolkit.User;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Navigation;
using NiconicoSession = Hohoema.Models.Niconico.NiconicoSession;

namespace Hohoema.ViewModels.Pages.Niconico.User;

using UserFollowContext = FollowContext<IUser>;

public class UserViewModel : IUser
{
    public UserId UserId { get; set; }

    public string Nickname { get; set; }

    public string IconUrl { get; set; }
}

public class UserInfoPageViewModel : HohoemaPageViewModelBase, IPinablePage, ITitleUpdatablePage
	{
    HohoemaPin IPinablePage.GetPin()
    {
        return new HohoemaPin()
        {
            Label = Nickname,
            PageType = HohoemaPageType.UserInfo,
            Parameter = $"id={UserId}"
        };
    }

    IObservable<string> ITitleUpdatablePage.GetTitleObservable()
    {
        return this.ObserveProperty(x => x.Nickname);
    }

    public UserInfoPageViewModel(
        IMessenger messenger,
        ApplicationLayoutManager applicationLayoutManager,
        UserProvider userProvider,
        UserFollowProvider userFollowProvider,
        VideoFilteringSettings ngSettings,
        NiconicoSession niconicoSession,
        SubscriptionManager subscriptionManager,
        LoginUserOwnedMylistManager userMylistManager,
        MylistResolver mylistRepository,
        VideoPlayWithQueueCommand videoPlayWithQueueCommand,
        AddSubscriptionCommand addSubscriptionCommand,
        OpenVideoListPageCommand openVideoListPageCommand,
        OpenPageCommand openPageCommand,
        OpenLinkCommand openLinkCommand
        )
    {
        NiconicoSession = niconicoSession;
        SubscriptionManager = subscriptionManager;
        UserMylistManager = userMylistManager;
        OpenVideoListPageCommand = openVideoListPageCommand;
        OpenPageCommand = openPageCommand;
        _mylistRepository = mylistRepository;
        VideoPlayWithQueueCommand = videoPlayWithQueueCommand;
        AddSubscriptionCommand = addSubscriptionCommand;
        OpenLinkCommand = openLinkCommand;
        _messenger = messenger;
        ApplicationLayoutManager = applicationLayoutManager;
        UserProvider = userProvider;
        _userFollowProvider = userFollowProvider;
        NgSettings = ngSettings;

        HasOwnerVideo = true;

        VideoInfoItems = new ObservableCollection<VideoListItemControlViewModel>();

        IsNGVideoOwner = new ReactiveProperty<bool>(false, ReactivePropertyMode.DistinctUntilChanged);

        IsNGVideoOwner.Subscribe(isNgVideoOwner =>
        {
            if (isNgVideoOwner)
            {
                NgSettings.AddHiddenVideoOwnerId(UserId, Nickname);
                IsNGVideoOwner.Value = true;
                Debug.WriteLine(Nickname + "をNG動画投稿者として登録しました。");
            }
            else
            {
                NgSettings.RemoveHiddenVideoOwnerId(UserId);
                IsNGVideoOwner.Value = false;
                Debug.WriteLine(Nickname + "をNG動画投稿者の指定を解除しました。");

            }
        });
    }

    public NiconicoSession NiconicoSession { get; }
    public SubscriptionManager SubscriptionManager { get; }
    public LoginUserOwnedMylistManager UserMylistManager { get; }
    public OpenVideoListPageCommand OpenVideoListPageCommand { get; }
    public OpenPageCommand OpenPageCommand { get; }    
    public VideoPlayWithQueueCommand VideoPlayWithQueueCommand { get; }
    public AddSubscriptionCommand AddSubscriptionCommand { get; }
    public OpenLinkCommand OpenLinkCommand { get; }
    public ApplicationLayoutManager ApplicationLayoutManager { get; }
    public UserProvider UserProvider { get; }
    public VideoFilteringSettings NgSettings { get; }
   
    private RelayCommand _OpenUserMylistPageCommand;
    public RelayCommand OpenUserMylistPageCommand
    {
        get
        {
            return _OpenUserMylistPageCommand
                ?? (_OpenUserMylistPageCommand = new RelayCommand(() =>
                {
                    _ = _messenger.OpenPageWithIdAsync(HohoemaPageType.UserMylist, UserId!);
                }));
        }
    }

    private RelayCommand _OpenUserSeriesPageCommand;
    public RelayCommand OpenUserSeriesPageCommand
    {
        get
        {
            return _OpenUserSeriesPageCommand
                ?? (_OpenUserSeriesPageCommand = new RelayCommand(() =>
                {
                    _ = _messenger.OpenPageWithIdAsync(HohoemaPageType.UserSeries, UserId!);
                }));
        }
    }

    private UserViewModel _User;
    public UserViewModel User
    {
        get { return _User; }
        set { SetProperty(ref _User, value); }
    }

    public UserId? UserId { get; private set; }
		public bool IsLoadFailed { get; private set; }


		private bool _IsLoginUser;
		public bool IsLoginUser
		{
			get { return _IsLoginUser; }
			set { SetProperty(ref _IsLoginUser, value); }
		}


		private string _Nickname;
		public string Nickname
		{
			get { return _Nickname; }
			set { SetProperty(ref _Nickname, value); }
		}


		private string _IconUrl;
		public string IconUrl
		{
			get { return _IconUrl; }
			set { SetProperty(ref _IconUrl, value); }
		}

		private bool _IsPremium;
		public bool IsPremium
		{
			get { return _IsPremium; }
			set { SetProperty(ref _IsPremium, value); }
		}

		private string _Gender;
		public string Gender
		{
			get { return _Gender; }
			set { SetProperty(ref _Gender, value); }
		}

		private string _BirthDay;
		public string BirthDay
		{
			get { return _BirthDay; }
			set { SetProperty(ref _BirthDay, value); }
		}

		private string _Region;
		public string Region
		{
			get { return _Region; }
			set { SetProperty(ref _Region, value); }
		}


		private uint _FavCount;
		public uint FollowerCount
		{
			get { return _FavCount; }
			set { SetProperty(ref _FavCount, value); }
		}

		private uint _StampCount;
		public uint StampCount
		{
			get { return _StampCount; }
			set { SetProperty(ref _StampCount, value); }
		}

		private string _Description;
		public string Description
		{
			get { return _Description; }
			set { SetProperty(ref _Description, value); }
		}

		private uint _VideoCount;
		public uint VideoCount
		{
			get { return _VideoCount; }
			set { SetProperty(ref _VideoCount, value); }
		}


		private bool _IsVideoPrivate;
		public bool IsVideoPrivate
		{
			get { return _IsVideoPrivate; }
			set { SetProperty(ref _IsVideoPrivate, value); }
		}

		private bool _HasOwnerVideo;
		public bool HasOwnerVideo
		{
			get { return _HasOwnerVideo; }
			set { SetProperty(ref _HasOwnerVideo, value); }
		}


    // Follow
    private UserFollowContext _FollowContext = UserFollowContext.Default;
    public UserFollowContext FollowContext
    {
        get => _FollowContext;
        set => SetProperty(ref _FollowContext, value);
    }


    public ReactiveProperty<bool> IsNGVideoOwner { get; private set; }

    private readonly IMessenger _messenger;
    private readonly UserFollowProvider _userFollowProvider;
    private readonly MylistResolver _mylistRepository;

    private IReadOnlyCollection<MylistPlaylist> _mylists;
    public IReadOnlyCollection<MylistPlaylist> MylistGroups
    {
        get { return _mylists; }
        set { SetProperty(ref _mylists, value); }
    }

    public ObservableCollection<VideoListItemControlViewModel> VideoInfoItems { get; private set; }

    public override void OnNavigatedFrom(INavigationParameters parameters)
    {
        if (VideoInfoItems != null)
        {
            VideoInfoItems.Clear();
        }
        base.OnNavigatedFrom(parameters);
    }

    public override async Task OnNavigatedToAsync(INavigationParameters parameters)
    {
        await base.OnNavigatedToAsync(parameters);

        var prevUserId = UserId;
        if (parameters.GetNavigationMode() is not NavigationMode.Refresh)
        {
            UserId? userId = null;
            if (parameters.TryGetValue("id", out string id))
            {
                userId = id;
            }
            if (parameters.TryGetValue("id", out UserId justUserId))
            {
                userId = justUserId;
            }
            
            UserId = userId;
            OnPropertyChanged(nameof(UserId));
        }

        if (UserId is null)
        {
            IsLoadFailed = true;
            UserId = null;
            User = null;
            FollowContext = UserFollowContext.Default;
            IsNGVideoOwner.Value = false;
            IsLoginUser = false;
            VideoInfoItems.Clear();
            HasOwnerVideo = false;
            MylistGroups = null;
            return;
        }

        // ログインユーザーと同じ場合、お気に入り表示をOFFに
        IsLoginUser = NiconicoSession.UserId == UserId.Value;

        IsLoadFailed = false;

        VideoInfoItems.Clear();

        try
        {
            var userInfo = await UserProvider.GetUserDetailAsync(UserId.Value);
            if (!userInfo.Response.IsSuccess)
            {
                throw new HohoemaException();
            }

            var user = userInfo.Response.Data;
            Nickname = user.User.Nickname;
            IconUrl = user.User.Icons.Small.OriginalString;

            FollowerCount = (uint)user.User.FollowerCount;


            User = new UserViewModel()
            {
                UserId = UserId.Value,
                Nickname = Nickname,
                IconUrl = IconUrl
            };
        }
        catch
        {
            IsLoadFailed = true;
        }


        try
        {
            if (NiconicoSession.IsLoggedIn)
            {
                FollowContext = await UserFollowContext.CreateAsync(_userFollowProvider, User);
            }
            else
            {
                FollowContext = UserFollowContext.Default;
            }
        }
        catch
        {
            FollowContext = UserFollowContext.Default;
        }

        // NGユーザーの設定

        if (!IsLoginUser)
        {
            IsNGVideoOwner.Value = NgSettings.IsHiddenVideoOwnerId(UserId);
        }
        else
        {
            IsNGVideoOwner.Value = false;
        }

        try
        {
            var userVideos = await UserProvider.GetUserVideosAsync(uint.Parse(UserId), 0, 5);
            foreach (var item in userVideos.Data.Items.Take(5))
            {
                var vm = new VideoListItemControlViewModel(item.Essential);
                VideoInfoItems.Add(vm);
            }
            OnPropertyChanged(nameof(VideoInfoItems));

            HasOwnerVideo = VideoInfoItems.Count != 0;
        }
        catch (Exception ex)
        {
            IsLoadFailed = true;
            Debug.WriteLine(ex.Message);
        }



        if (NiconicoSession.IsLoginUserId(UserId.Value))
        {
            MylistGroups = UserMylistManager.Mylists;
            OnPropertyChanged(nameof(MylistGroups));
        }
        else
        {
            try
            {
                MylistGroups = await _mylistRepository.GetUserMylistsAsync(UserId.Value);
                OnPropertyChanged(nameof(MylistGroups));
            }
            catch (Exception ex)
            {
                IsLoadFailed = true;
                Debug.WriteLine(ex.Message);
            }
        }
    }
    
}
