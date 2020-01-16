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
using System.Collections.Async;
using NicoPlayerHohoema.Models.Cache;
using NicoPlayerHohoema.Models.Provider;
using Unity;
using NicoPlayerHohoema.Services;
using Prism.Navigation;
using NicoPlayerHohoema.Services.Page;
using NicoPlayerHohoema.UseCase.Playlist;

namespace NicoPlayerHohoema.ViewModels
{
    public class UserVideoPageViewModel : HohoemaListingPageViewModelBase<VideoInfoControlViewModel>, INavigatedAwareAsync, IPinablePage
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

        public UserVideoPageViewModel(
            UserProvider userProvider,
            Models.Subscription.SubscriptionManager subscriptionManager,
            HohoemaPlaylist hohoemaPlaylist,
            Services.PageManager pageManager,
            Commands.Subscriptions.CreateSubscriptionGroupCommand createSubscriptionGroupCommand
            )
        {
            SubscriptionManager = subscriptionManager;
            UserProvider = userProvider;
            HohoemaPlaylist = hohoemaPlaylist;
            PageManager = pageManager;
            CreateSubscriptionGroupCommand = createSubscriptionGroupCommand;
        }


        public Models.Subscription.SubscriptionManager SubscriptionManager { get; }
        public UserProvider UserProvider { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public PageManager PageManager { get; }
        public Commands.Subscriptions.CreateSubscriptionGroupCommand CreateSubscriptionGroupCommand { get; }

        public Models.Subscription.SubscriptionSource? SubscriptionSource => new Models.Subscription.SubscriptionSource(UserName, Models.Subscription.SubscriptionSourceType.User, UserId);

        public override async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            if (parameters.TryGetValue<string>("id", out string userId))
            {
                UserId = userId;

                User = await UserProvider.GetUserDetail(UserId);

                if (User != null)
                {
                    IsOwnerVideoPrivate = User.IsOwnerVideoPrivate;
                    UserName = User.Nickname;
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

        public UserDetail User { get; private set; }

		
		public string UserId { get; private set; }
	}


	public class UserVideoIncrementalSource : HohoemaIncrementalSourceBase<VideoInfoControlViewModel>
	{
		public uint UserId { get; }
		public UserProvider UserProvider { get; }
		public VideoCacheManager MediaManager { get; }
        


		public UserDetail User { get; private set;}

		public List<UserVideoResponse> _ResList;
		
		public UserVideoIncrementalSource(string userId, UserDetail userDetail, UserProvider userProvider)
		{
			UserId = uint.Parse(userId);
			User = userDetail;
            UserProvider = userProvider;
			_ResList = new List<UserVideoResponse>();
		}

        protected override async Task<IAsyncEnumerable<VideoInfoControlViewModel>> GetPagedItemsImpl(int start, int count)
        {
            var rawPage = ((start) / 30);
            var page = rawPage + 1;

            var res = _ResList.ElementAtOrDefault(rawPage);
            if (res == null)
            {
                try
                {
                    res = await UserProvider.GetUserVideos(UserId, (uint)page);
                }
                catch
                {
                    return AsyncEnumerable.Empty<VideoInfoControlViewModel>();
                }
                _ResList.Add(res);
            }

            var head = start - rawPage * 30;

            var items = res.Items.Skip(head).Take(count);
            return items.Select(x =>
            {
                var vm = new VideoInfoControlViewModel(x.VideoId);
                vm.SetupDisplay(x);
                return vm;
            })
            .ToAsyncEnumerable();
        }

        protected override Task<int> ResetSourceImpl()
        {
            return Task.FromResult((int)User.TotalVideoCount);
        }
    }
}
