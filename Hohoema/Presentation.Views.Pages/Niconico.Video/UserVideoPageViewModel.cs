using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Helpers;
using System.Threading;
using Prism.Commands;
using Hohoema.Models.Domain.Player.Video.Cache;
using Prism.Navigation;
using Hohoema.Presentation.Services.Page;
using Hohoema.Models.UseCase.NicoVideos;
using System;
using Reactive.Bindings.Extensions;
using Hohoema.Models.UseCase;
using Reactive.Bindings;
using Mntone.Nico2.Videos.Users;
using static Mntone.Nico2.Users.User.UserDetailResponse;
using System.Runtime.CompilerServices;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Niconico.User;
using Hohoema.Models.Domain.Subscriptions;
using Hohoema.Presentation.ViewModels.Subscriptions;
using Hohoema.Presentation.ViewModels.VideoListPage;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;
using Hohoema.Presentation.ViewModels.Niconico.User;

namespace Hohoema.Presentation.ViewModels.Pages.Niconico.Video
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
            AddSubscriptionCommand addSubscriptionCommand,
            SelectionModeToggleCommand selectionModeToggleCommand
            )
        {
            SubscriptionManager = subscriptionManager;
            ApplicationLayoutManager = applicationLayoutManager;
            UserProvider = userProvider;
            HohoemaPlaylist = hohoemaPlaylist;
            PageManager = pageManager;
            AddSubscriptionCommand = addSubscriptionCommand;
            SelectionModeToggleCommand = selectionModeToggleCommand;
            UserInfo = new ReactiveProperty<UserInfoViewModel>();
        }


        public SubscriptionManager SubscriptionManager { get; }
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public UserProvider UserProvider { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public PageManager PageManager { get; }
        public AddSubscriptionCommand AddSubscriptionCommand { get; }
        public SelectionModeToggleCommand SelectionModeToggleCommand { get; }
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
                    UserName = User.User.Nickname;
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
		public VideoCacheManagerLegacy MediaManager { get; }

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
                vm.SetDescription((int)item.Count.View, (int)item.Count.Comment, (int)item.Count.Mylist);

                await vm.InitializeAsync(ct).ConfigureAwait(false);
                yield return vm;

                ct.ThrowIfCancellationRequested();
            }
        }

        protected override async ValueTask<int> ResetSourceImpl()
        {
            _firstRes = await UserProvider.GetUserVideos(UserId, (uint)0);
            return (int)_firstRes.Data.TotalCount;
        }
    }
}
