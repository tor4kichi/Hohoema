using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hohoema.Models;
using Hohoema.Models.Helpers;
using Mntone.Nico2.Users.Video;
using Mntone.Nico2.Users.User;
using System.Threading;
using Prism.Commands;
using Windows.UI.Xaml.Navigation;
using Hohoema.Models.Cache;
using Hohoema.Models.Provider;
using Unity;
using Hohoema.Services;
using Prism.Navigation;
using Hohoema.ViewModels.Pages;
using Hohoema.UseCase.Playlist;
using Hohoema.Interfaces;
using System;
using Reactive.Bindings.Extensions;
using Hohoema.Models.Subscription;
using Hohoema.UseCase;
using Hohoema.ViewModels.Subscriptions;
using Reactive.Bindings;
using System.Runtime.CompilerServices;

namespace Hohoema.ViewModels
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
                    IsOwnerVideoPrivate = User.IsOwnerVideoPrivate;
                    UserName = User.Nickname;

                    UserInfo.Value = new UserInfoViewModel(User.Nickname, User.UserId, User.ThumbnailUri);
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

        protected override async IAsyncEnumerable<VideoInfoControlViewModel> GetPagedItemsImpl(int start, int count, [EnumeratorCancellation]CancellationToken cancellationToken)
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
                    yield break;
                }
                _ResList.Add(res);
            }

            var head = start - rawPage * 30;

            var items = res.Items.Skip(head).Take(count);
            foreach (var item in items)
            {
                var vm = new VideoInfoControlViewModel(item.VideoId);
                await vm.InitializeAsync(cancellationToken);
                yield return vm;
            }
        }

        protected override Task<int> ResetSourceImpl()
        {
            return Task.FromResult((int)User.TotalVideoCount);
        }
    }
}
