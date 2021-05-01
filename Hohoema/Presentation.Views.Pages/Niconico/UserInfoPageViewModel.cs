using I18NPortable;
using Mntone.Nico2;

using Hohoema.Models.Domain;

using Hohoema.Presentation.Services;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.NicoVideos;
using Prism.Commands;
using Prism.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.User;
using NiconicoSession = Hohoema.Models.Domain.Niconico.NiconicoSession;
using Hohoema.Models.Domain.Subscriptions;
using Hohoema.Models.Domain.Niconico.Mylist.LoginUser;
using Hohoema.Presentation.ViewModels.Subscriptions;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Presentation.ViewModels.VideoListPage;
using Hohoema.Models.Domain.Pins;
using Hohoema.Models.Domain.Niconico.Mylist;
using Hohoema.Presentation.ViewModels.Niconico.Follow;
using Hohoema.Presentation.ViewModels.Niconico.Share;

namespace Hohoema.Presentation.ViewModels.Pages.Niconico
{
    public class UserInfoPageViewModel : HohoemaViewModelBase, IUser, INavigatedAwareAsync, IPinablePage, ITitleUpdatablePage
	{
        HohoemaPin IPinablePage.GetPin()
        {
            return new HohoemaPin()
            {
                Label = UserName,
                PageType = HohoemaPageType.UserInfo,
                Parameter = $"id={UserId}"
            };
        }

        IObservable<string> ITitleUpdatablePage.GetTitleObservable()
        {
            return this.ObserveProperty(x => x.UserName);
        }

        public UserInfoPageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            UserProvider userProvider,
            VideoFilteringSettings ngSettings,
            NiconicoSession niconicoSession,
            SubscriptionManager subscriptionManager,
            LoginUserOwnedMylistManager userMylistManager,
            HohoemaPlaylist hohoemaPlaylist,
            PageManager pageManager,
            MylistRepository mylistRepository,
            NiconicoFollowToggleButtonViewModel followToggleButtonService,
            AddSubscriptionCommand addSubscriptionCommand,
            OpenLinkCommand openLinkCommand
            )
        {
            NiconicoSession = niconicoSession;
            SubscriptionManager = subscriptionManager;
            UserMylistManager = userMylistManager;
            HohoemaPlaylist = hohoemaPlaylist;
            PageManager = pageManager;
            _mylistRepository = mylistRepository;
            FollowToggleButtonService = followToggleButtonService;
            AddSubscriptionCommand = addSubscriptionCommand;
            OpenLinkCommand = openLinkCommand;
            ApplicationLayoutManager = applicationLayoutManager;
            UserProvider = userProvider;
            NgSettings = ngSettings;

            HasOwnerVideo = true;

            VideoInfoItems = new ObservableCollection<VideoInfoControlViewModel>();

            OpenUserVideoPageCommand = VideoInfoItems.ObserveProperty(x => x.Count)
                .Select(x => x > 0)
                .ToReactiveCommand()
                .AddTo(_CompositeDisposable);

            OpenUserVideoPageCommand.Subscribe(x =>
            {
                PageManager.OpenPageWithId(HohoemaPageType.UserVideo, UserId);
            })
            .AddTo(_CompositeDisposable);

            IsNGVideoOwner = new ReactiveProperty<bool>(false, ReactivePropertyMode.DistinctUntilChanged);

            IsNGVideoOwner.Subscribe(isNgVideoOwner =>
            {
                if (isNgVideoOwner)
                {
                    NgSettings.AddHiddenVideoOwnerId(UserId, UserName);
                    IsNGVideoOwner.Value = true;
                    Debug.WriteLine(UserName + "をNG動画投稿者として登録しました。");
                }
                else
                {
                    NgSettings.RemoveHiddenVideoOwnerId(UserId);
                    IsNGVideoOwner.Value = false;
                    Debug.WriteLine(UserName + "をNG動画投稿者の指定を解除しました。");

                }
            });
        }

        public ReactiveCommand OpenUserVideoPageCommand { get; private set; }
        public NiconicoSession NiconicoSession { get; }
        public SubscriptionManager SubscriptionManager { get; }
        public LoginUserOwnedMylistManager UserMylistManager { get; }
        public PageManager PageManager { get; }
        public NiconicoFollowToggleButtonViewModel FollowToggleButtonService { get; }
        public AddSubscriptionCommand AddSubscriptionCommand { get; }
        public OpenLinkCommand OpenLinkCommand { get; }
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public UserProvider UserProvider { get; }
        public VideoFilteringSettings NgSettings { get; }
       
        private DelegateCommand _OpenUserMylistPageCommand;
        public DelegateCommand OpenUserMylistPageCommand
        {
            get
            {
                return _OpenUserMylistPageCommand
                    ?? (_OpenUserMylistPageCommand = new DelegateCommand(() =>
                    {
                        PageManager.OpenPageWithId(HohoemaPageType.UserMylist, UserId);
                    }));
            }
        }

        private DelegateCommand _OpenUserSeriesPageCommand;
        public DelegateCommand OpenUserSeriesPageCommand
        {
            get
            {
                return _OpenUserSeriesPageCommand
                    ?? (_OpenUserSeriesPageCommand = new DelegateCommand(() =>
                    {
                        PageManager.OpenPageWithId(HohoemaPageType.UserSeries, UserId);
                    }));
            }
        }

        public string UserId { get; private set; }
		public bool IsLoadFailed { get; private set; }


		private bool _IsLoginUser;
		public bool IsLoginUser
		{
			get { return _IsLoginUser; }
			set { SetProperty(ref _IsLoginUser, value); }
		}


		private string _UserName;
		public string UserName
		{
			get { return _UserName; }
			set { SetProperty(ref _UserName, value); }
		}


		private string _UserIconUri;
		public string UserIconUri
		{
			get { return _UserIconUri; }
			set { SetProperty(ref _UserIconUri, value); }
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

		public ReactiveProperty<bool> IsNGVideoOwner { get; private set; }


        public  HohoemaPlaylist HohoemaPlaylist { get; }

        private readonly MylistRepository _mylistRepository;

        private IReadOnlyCollection<MylistPlaylist> _mylists;
        public IReadOnlyCollection<MylistPlaylist> MylistGroups
        {
            get { return _mylists; }
            set { SetProperty(ref _mylists, value); }
        }

        public ObservableCollection<VideoInfoControlViewModel> VideoInfoItems { get; private set; }


        string INiconicoObject.Id => UserId;

        string INiconicoObject.Label => UserName;

        public async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            string userId = null;

            if (parameters.TryGetValue<string>("id", out var id))
            {
                userId = id;
            }

            if (userId == UserId)
            {
                return;
            }

            UserId = userId;
            RaisePropertyChanged(nameof(IUser.Id));

            // ログインユーザーと同じ場合、お気に入り表示をOFFに
            IsLoginUser = NiconicoSession.UserId.ToString() == userId;

            IsLoadFailed = false;

            VideoInfoItems.Clear();

            try
            {
                var userInfo = await UserProvider.GetUserDetail(UserId);

                var user = userInfo;
                UserName = user.User.Nickname;
                UserIconUri = user.User.Icons.Small.OriginalString;

                FollowerCount = (uint)user.User.FollowerCount;
            }
            catch
            {
                IsLoadFailed = true;
            }


            if (UserId == null) { return; }


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
                await Task.Delay(500);

                var userVideos = await UserProvider.GetUserVideos(uint.Parse(UserId), 1);
                foreach (var item in userVideos.Data.Items.Take(5))
                {
                    var vm = new VideoInfoControlViewModel(item.Id);
                    vm.SetTitle(item.Title);
                    vm.SetThumbnailImage(item.Thumbnail.ListingUrl.OriginalString);
                    VideoInfoItems.Add(vm);
                }
                RaisePropertyChanged(nameof(VideoInfoItems));
            }
            catch (Exception ex)
            {
                IsLoadFailed = true;
                Debug.WriteLine(ex.Message);
            }

            HasOwnerVideo = VideoInfoItems.Count != 0;


            if (NiconicoSession.IsLoginUserId(UserId))
            {
                MylistGroups = UserMylistManager.Mylists;
            }
            else
            {
                try
                {
                    //					await Task.Delay(500);
                    MylistGroups = await _mylistRepository.GetUserMylistsAsync(UserId);
                }
                catch (Exception ex)
                {
                    IsLoadFailed = true;
                    Debug.WriteLine(ex.Message);
                }
            }
            RaisePropertyChanged(nameof(MylistGroups));

            FollowToggleButtonService.SetFollowTarget(this);
        }
        
    }
}
