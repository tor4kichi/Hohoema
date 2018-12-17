using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;
using Prism.Windows.Navigation;
using System.Collections.ObjectModel;
using Mntone.Nico2;
using Reactive.Bindings;
using System.Reactive.Linq;
using Reactive.Bindings.Extensions;
using System.Diagnostics;
using System.Threading;
using Prism.Commands;
using NicoPlayerHohoema.Services;
using Windows.System;
using NicoPlayerHohoema.Models.Subscription;
using NicoPlayerHohoema.Models.Provider;

namespace NicoPlayerHohoema.ViewModels
{
	public class UserInfoPageViewModel : HohoemaViewModelBase
	{


        public UserInfoPageViewModel(
            PageManager pageManager,
            Models.NiconicoSession niconicoSession,
            SubscriptionManager subscriptionManager,
            UserMylistManager userMylistManager,
            UserProvider userProvider,
            NGSettings ngSettings
            )
            : base(pageManager)
        {
            HasOwnerVideo = true;


            MylistGroups = new ObservableCollection<MylistGroupListItem>();
            VideoInfoItems = new ObservableCollection<VideoInfoControlViewModel>();


            OpenUserVideoPageCommand = VideoInfoItems.ObserveProperty(x => x.Count)
                .Select(x => x > 0)
                .ToReactiveCommand()
                .AddTo(_CompositeDisposable);

            OpenUserVideoPageCommand.Subscribe(x =>
            {
                PageManager.OpenPage(HohoemaPageType.UserVideo, UserId);
            })
            .AddTo(_CompositeDisposable);

            IsNGVideoOwner = new ReactiveProperty<bool>(false);

            IsNGVideoOwner.Subscribe(isNgVideoOwner =>
            {
                if (isNgVideoOwner)
                {
                    NgSettings.AddNGVideoOwnerId(UserId, UserName);
                    IsNGVideoOwner.Value = true;
                    Debug.WriteLine(UserName + "をNG動画投稿者として登録しました。");
                }
                else
                {
                    NgSettings.RemoveNGVideoOwnerId(UserId);
                    IsNGVideoOwner.Value = false;
                    Debug.WriteLine(UserName + "をNG動画投稿者の指定を解除しました。");

                }
            });
            NiconicoSession = niconicoSession;
            SubscriptionManager = subscriptionManager;
            UserMylistManager = userMylistManager;
            UserProvider = userProvider;
            NgSettings = ngSettings;
        }

        public ReactiveCommand OpenUserVideoPageCommand { get; private set; }
        public Models.NiconicoSession NiconicoSession { get; }
        public SubscriptionManager SubscriptionManager { get; }
        public UserMylistManager UserMylistManager { get; }
        public UserProvider UserProvider { get; }
        public NGSettings NgSettings { get; }

        public Models.Subscription.SubscriptionSource? SubscriptionSource => new Models.Subscription.SubscriptionSource(UserName, Models.Subscription.SubscriptionSourceType.User, UserId);
       
        public Database.Bookmark UserBookmark { get; private set; }

        private DelegateCommand _OpenUserAccountPageInBrowserCommand;
        public DelegateCommand OpenUserAccountPageInBrowserCommand
        {
            get
            {
                return _OpenUserAccountPageInBrowserCommand
                    ?? (_OpenUserAccountPageInBrowserCommand = new DelegateCommand(async () =>
                    {
                        if (IsLoginUser)
                        {
                            Uri UserAccountPageUri = new Uri("http://www.nicovideo.jp/my/top");
                            await Launcher.LaunchUriAsync(UserAccountPageUri);
                        }
                        else
                        {
                            // www.nicovideo.jp/user/3914961
                            var userPageUri = new Uri(NiconicoUrls.UserPageUrlBase + UserId);
                            await Launcher.LaunchUriAsync(userPageUri);
                        }
                    }));
            }
        }

        private DelegateCommand _OpenUserMylistPageCommand;
        public DelegateCommand OpenUserMylistPageCommand
        {
            get
            {
                return _OpenUserMylistPageCommand
                    ?? (_OpenUserMylistPageCommand = new DelegateCommand(() =>
                    {
                        PageManager.OpenPage(HohoemaPageType.UserMylist, UserId);
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


		private bool _NowLoading;
		public bool NowLoading
		{
			get { return _NowLoading; }
			set { SetProperty(ref _NowLoading, value); }
		}

		public ReactiveProperty<bool> IsNGVideoOwner { get; private set; }


		public ObservableCollection<MylistGroupListItem> MylistGroups { get; private set; }
		public ObservableCollection<VideoInfoControlViewModel> VideoInfoItems { get; private set; }




        protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            NowLoading = true;

            string userId = null;
            if (e.Parameter is string)
            {
                userId = e.Parameter as string;
            }
            else if (e.Parameter is uint)
            {
                userId = ((uint)e.Parameter).ToString();
            }
            else
            {
                userId = NiconicoSession.UserId.ToString();
            }

            if (userId == UserId)
            {
                NowLoading = false;
                return;
            }

            UserId = userId;


            // ログインユーザーと同じ場合、お気に入り表示をOFFに
            IsLoginUser = NiconicoSession.UserId.ToString() == userId;

            IsLoadFailed = false;

            MylistGroups.Clear();
            VideoInfoItems.Clear();

            try
            {
                var userInfo = await UserProvider.GetUserDetail(UserId);

                var user = userInfo;
                UserName = user.Nickname;
                UserIconUri = user.ThumbnailUri;
                
                FollowerCount = user.FollowerCount;
                StampCount = user.StampCount;
                VideoCount = user.TotalVideoCount;
                IsVideoPrivate = user.IsOwnerVideoPrivate;
            }
            catch
            {
                IsLoadFailed = true;
                NowLoading = false;
            }


            if (UserId == null) { return; }


            // NGユーザーの設定

            if (!IsLoginUser)
            {
                var ngResult = NgSettings.IsNgVideoOwnerId(UserId);
                IsNGVideoOwner.Value = ngResult != null;
            }
            else
            {
                IsNGVideoOwner.Value = false;
            }

            try
            {
                await Task.Delay(500);

                var userVideos = await UserProvider.GetUserVideos(uint.Parse(UserId), 1);
                foreach (var item in userVideos.Items.Take(5))
                {
                    var vm = new VideoInfoControlViewModel(item.VideoId, NgSettings);
                    vm.SetTitle(item.Title);
                    vm.SetThumbnailImage(item.ThumbnailUrl.OriginalString);
                    VideoInfoItems.Add(vm);
                }
                RaisePropertyChanged(nameof(VideoInfoItems));
            }
            catch (Exception ex)
            {
                IsLoadFailed = true;
                NowLoading = false;
                Debug.WriteLine(ex.Message);
            }

            HasOwnerVideo = VideoInfoItems.Count != 0;


            if (NiconicoSession.IsLoginUserId(UserId))
            {
                foreach (var item in UserMylistManager.UserMylists)
                {
                    MylistGroups.Add(new MylistGroupListItem(item));
                }
            }
            else
            {
                try
                {
                    //					await Task.Delay(500);

                    var mylistGroups = await UserProvider.GetUserMylistGroups(UserId);
                    foreach (var item in mylistGroups)
                    {
                        MylistGroups.Add(new MylistGroupListItem(item));
                    }
                }
                catch (Exception ex)
                {
                    IsLoadFailed = true;
                    Debug.WriteLine(ex.Message);
                }
            }
            RaisePropertyChanged(nameof(MylistGroups));


            UserBookmark = Database.BookmarkDb.Get(Database.BookmarkType.User, UserId)
            ?? new Database.Bookmark()
            {
                Content = UserId,
                Label = UserName,
                BookmarkType = Database.BookmarkType.User
            };

            RaisePropertyChanged(nameof(UserBookmark));

            NowLoading = false;
        }



    }
}
