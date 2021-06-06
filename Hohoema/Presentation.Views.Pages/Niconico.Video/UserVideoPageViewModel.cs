using Hohoema.Models.Domain.Niconico.User;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Pins;
using Hohoema.Models.Domain.Player.Video.Cache;
using Hohoema.Models.Domain.Subscriptions;
using Hohoema.Models.Helpers;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.NicoVideos;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Presentation.ViewModels.Niconico.User;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;
using Hohoema.Presentation.ViewModels.Subscriptions;
using Hohoema.Presentation.ViewModels.VideoListPage;
using Mntone.Nico2.Videos.Users;
using NiconicoToolkit.User;
using Prism.Commands;
using Prism.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static Mntone.Nico2.Users.User.UserDetailResponse;

namespace Hohoema.Presentation.ViewModels.Pages.Niconico.Video
{
    public class UserVideoPageViewModel : HohoemaListingPageViewModelBase<VideoListItemControlViewModel>, INavigatedAwareAsync, IPinablePage, ITitleUpdatablePage
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
                
                var res = await UserProvider.GetUserDetailAsync(UserId);
                if (res.IsSuccess)
                {
                    User = res.Data.User;

                    UserInfo.Value = new UserInfoViewModel(User.Nickname, User.Id.ToString(), User.Icons.Small.OriginalString);
                    UserName = User.Nickname;
                }

                if (User != null)
                {
                }
                else
                {
                    //				UpdateTitle("投稿動画一覧");
                }
            }

            await base.OnNavigatedToAsync(parameters);
        }



		protected override IIncrementalSource<VideoListItemControlViewModel> GenerateIncrementalSource()
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


	public class UserVideoIncrementalSource : HohoemaIncrementalSourceBase<VideoListItemControlViewModel>
	{

        public override uint OneTimeLoadCount => 25;

        public uint UserId { get; }
		public UserProvider UserProvider { get; }
        public UserDetail User { get; private set;}

		public UserVideoIncrementalSource(string userId, UserDetail userDetail, UserProvider userProvider)
		{
			UserId = uint.Parse(userId);
			User = userDetail;
            UserProvider = userProvider;
		}

        bool _isEnd = false;
        int _count = 0;
        protected override async IAsyncEnumerable<VideoListItemControlViewModel> GetPagedItemsImpl(int start, int count, [EnumeratorCancellation] CancellationToken ct = default)
        {
            if (_isEnd) { yield break; }

            var res = await UserProvider.GetUserVideosAsync(UserId, start / (int)OneTimeLoadCount);

            ct.ThrowIfCancellationRequested();

            var items = res.Data.Items;
            foreach (var item in items)
            {
                var vm = new VideoListItemControlViewModel(item.Essential);

                await vm.EnsureProviderIdAsync(ct).ConfigureAwait(false);
                yield return vm;

                ct.ThrowIfCancellationRequested();
            }

            _count += items.Length;

            _isEnd = _count >= res.Data.TotalCount;
        }

        protected override ValueTask<int> ResetSourceImpl()
        {
            return new ValueTask<int>(1);
        }
    }
}
