using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Helpers;
using Mntone.Nico2.Users.Video;
using Mntone.Nico2.Users.User;
using System.Threading;
using Prism.Commands;
using Windows.UI.Xaml.Navigation;
using NicoPlayerHohoema.Models.Cache;
using NicoPlayerHohoema.Models.Provider;
using Unity;
using NicoPlayerHohoema.Services;
using Prism.Navigation;
using NicoPlayerHohoema.Services.Page;
using NicoPlayerHohoema.UseCase.Playlist;
using NicoPlayerHohoema.Interfaces;
using System;
using Reactive.Bindings.Extensions;
using NicoPlayerHohoema.Models.Subscription;
using NicoPlayerHohoema.UseCase;
using NicoPlayerHohoema.ViewModels.Subscriptions;
using Reactive.Bindings;
using Mntone.Nico2.Videos.Users;
using WinRTXamlToolkit.IO.Serialization;
using static Mntone.Nico2.Users.User.UserDetailResponse;
using System.Runtime.CompilerServices;

namespace NicoPlayerHohoema.ViewModels
{
    public class UserVideoPageViewModel : HohoemaListingPageViewModelBase<VideoInfoControlViewModel>, INavigatedAwareAsync, IPinablePage, ITitleUpdatablePage
    {
        HohoemaPin IPinablePage.GetPin()
        {
            return new HohoemaPin()
            {
                Label = UserName,
                PageType = HohoemaPageType.UserVideo,
                Parameter = $"id={UserId}"
            };
        }

        IObservable<string> ITitleUpdatablePage.GetTitleObservable()
        {
            return this.ObserveProperty(x => x.UserName);
        }

        public UserVideoPageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            UserProvider userProvider,
            SubscriptionManager subscriptionManager,
            HohoemaPlaylist hohoemaPlaylist,
            PageManager pageManager,
            ViewModels.Subscriptions.AddSubscriptionCommand addSubscriptionCommand
            )
        {
            SubscriptionManager = subscriptionManager;
            ApplicationLayoutManager = applicationLayoutManager;
            UserProvider = userProvider;
            HohoemaPlaylist = hohoemaPlaylist;
            PageManager = pageManager;
            AddSubscriptionCommand = addSubscriptionCommand;

            UserInfo = new ReactiveProperty<UserInfoViewModel>();
        }


        public Models.Subscription.SubscriptionManager SubscriptionManager { get; }
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public UserProvider UserProvider { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public PageManager PageManager { get; }
        public AddSubscriptionCommand AddSubscriptionCommand { get; }

        public Models.Subscription.SubscriptionSource? SubscriptionSource => new Models.Subscription.SubscriptionSource(UserName, Models.Subscription.SubscriptionSourceType.User, UserId);

        public ReactiveProperty<UserInfoViewModel> UserInfo { get; }

        public override async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            if (parameters.TryGetValue<string>("id", out string userId))
            {
                UserId = userId;

                User = await UserProvider.GetUserDetail(UserId);

                if (User != null)
                {
                    UserInfo.Value = new UserInfoViewModel(User.User.Nickname, User.User.Id.ToString(), User.User.Icons.Small.OriginalString);
                }
                else
                {
                    //				UpdateTitle("投稿動画一覧");
                }
            }

            await base.OnNavigatedToAsync(parameters);
        }



		protected override IIncrementalSource<VideoInfoControlViewModel> GenerateIncrementalSource()
		{
			return new UserVideoIncrementalSource(
				UserId,
				User, 
                UserProvider
                );
		}


        private DelegateCommand _OpenVideoOwnerUserPageCommand;
		public DelegateCommand OpenVideoOwnerUserPageCommand
		{
			get
			{
				return _OpenVideoOwnerUserPageCommand
					?? (_OpenVideoOwnerUserPageCommand = new DelegateCommand(() => 
					{
						PageManager.OpenPageWithId(HohoemaPageType.UserInfo, UserId);
					}));
			}
		}


		private string _UserName;
		public string UserName
		{
			get { return _UserName; }
			set { SetProperty(ref _UserName, value); }
		}

        private bool _IsOwnerVideoPrivate;
        public bool IsOwnerVideoPrivate
        {
            get { return _IsOwnerVideoPrivate; }
            set { SetProperty(ref _IsOwnerVideoPrivate, value); }
        }

        public UserDetails User { get; private set; }

		
		public string UserId { get; private set; }
	}


	public class UserVideoIncrementalSource : HohoemaIncrementalSourceBase<VideoInfoControlViewModel>
	{
		public uint UserId { get; }
		public UserProvider UserProvider { get; }
		public VideoCacheManager MediaManager { get; }

        public override uint OneTimeLoadCount => 25;

        public UserDetails User { get; private set;}

        UserVideosResponse _firstRes;
        public List<UserVideosResponse> _ResList;
		
		public UserVideoIncrementalSource(string userId, UserDetails userDetail, UserProvider userProvider)
		{
			UserId = uint.Parse(userId);
			User = userDetail;
            UserProvider = userProvider;
			_ResList = new List<UserVideosResponse>();
		}

        protected override async IAsyncEnumerable<VideoInfoControlViewModel> GetPagedItemsImpl(int start, int count, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var res = start == 0 ? _firstRes : await UserProvider.GetUserVideos(UserId, (uint)start / OneTimeLoadCount);

            ct.ThrowIfCancellationRequested();

            var items = res.Data.Items;
            foreach (var item in items)
            {
                var vm = new VideoInfoControlViewModel(item.Id);
                vm.SetTitle(item.Title);
                vm.SetThumbnailImage(item.Thumbnail.ListingUrl.OriginalString);
                vm.SetSubmitDate(item.RegisteredAt.DateTime);
                vm.SetVideoDuration(TimeSpan.FromSeconds(item.Duration));

                yield return vm;

                ct.ThrowIfCancellationRequested();
            }
        }

        protected override async Task<int> ResetSourceImpl()
        {
            _firstRes = await UserProvider.GetUserVideos(UserId, (uint)0);
            return (int)_firstRes.Data.TotalCount;
        }
    }
}
