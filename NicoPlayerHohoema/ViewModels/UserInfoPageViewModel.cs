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
using NicoPlayerHohoema.Views.Service;
using Windows.System;

namespace NicoPlayerHohoema.ViewModels
{
	public class UserInfoPageViewModel : HohoemaViewModelBase
	{
        public UserInfoPageViewModel(
            HohoemaApp hohoemaApp
            , PageManager pageManager
            ) 
			: base(hohoemaApp, pageManager)
		{
            HasOwnerVideo = true;

           
            MylistGroups = new ObservableCollection<MylistGroupListItem>();
			VideoInfoItems = new ObservableCollection<VideoInfoControlViewModel>();

			IsFavorite = new ReactiveProperty<bool>()
				.AddTo(_CompositeDisposable);
			CanChangeFavoriteState = new ReactiveProperty<bool>()
				.AddTo(_CompositeDisposable);


			IsFavorite
				.Where(x => UserId != null)
				.Subscribe(async x =>
				{
					if (_NowProcessFavorite) { return; }

					_NowProcessFavorite = true;

					CanChangeFavoriteState.Value = false;
					if (x)
					{
						if (await FavoriteUser())
						{
							Debug.WriteLine(UserName + "をお気に入り登録しました.");
						}
						else
						{
							// お気に入り登録に失敗した場合は状態を差し戻し
							Debug.WriteLine(UserName + "をお気に入り登録に失敗");
							IsFavorite.Value = false;
						}
					}
					else
					{
						if (await UnfavoriteUser())
						{
							Debug.WriteLine(UserName + "をお気に入り解除しました.");
						}
						else
						{
							// お気に入り解除に失敗した場合は状態を差し戻し
							Debug.WriteLine(UserName + "お気に入り解除に失敗");
							IsFavorite.Value = true;
						}
					}

					CanChangeFavoriteState.Value =
						IsFavorite.Value == true
						|| HohoemaApp.FollowManager.CanMoreAddFollow(FollowItemType.User);


					_NowProcessFavorite = false;
				})
				.AddTo(_CompositeDisposable);


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
                    HohoemaApp.UserSettings.NGSettings.AddNGVideoOwnerId(UserId, UserName);
                    IsNGVideoOwner.Value = true;
                    Debug.WriteLine(UserName + "をNG動画投稿者として登録しました。");
                }
                else
                {
                    HohoemaApp.UserSettings.NGSettings.RemoveNGVideoOwnerId(UserId);
                    IsNGVideoOwner.Value = false;
                    Debug.WriteLine(UserName + "をNG動画投稿者の指定を解除しました。");

                }
            });
		}


		protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
            NowLoading = true;

            string userId = null;
			if(e.Parameter is string)
			{
				userId = e.Parameter as string;
			}
			else if (e.Parameter is uint)
			{
				userId = ((uint)e.Parameter).ToString();
			}

			if (userId == UserId) { return; }

			UserId = userId;

            
			// ログインユーザーと同じ場合、お気に入り表示をOFFに
			IsLoginUser = HohoemaApp.LoginUserId.ToString() == userId;

			IsLoadFailed = false;

			MylistGroups.Clear();
			VideoInfoItems.Clear();

            try
			{
				var userInfo = await HohoemaApp.ContentProvider.GetUserDetail(UserId);

				var user = userInfo;
				UserName = user.Nickname;
				UserIconUri = user.ThumbnailUri;
				Description = user.Description;

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

			UpdateTitle($"{UserName} さん");

			// NGユーザーの設定

			if (!IsLoginUser)
			{
				var ngResult = HohoemaApp.UserSettings.NGSettings.IsNgVideoOwnerId(UserId);
				IsNGVideoOwner.Value = ngResult != null;
			}
			else
			{
				IsNGVideoOwner.Value = false;
			}


			// お気に入り状態の取得
			_NowProcessFavorite = true;

			var favManager = HohoemaApp.FollowManager;
			IsFavorite.Value = favManager.IsFollowItem(FollowItemType.User, UserId);

			CanChangeFavoriteState.Value =
				IsFavorite.Value == true
				|| favManager.CanMoreAddFollow(FollowItemType.User);

			_NowProcessFavorite = false;

			try
			{
				await Task.Delay(500);

				var userVideos = await HohoemaApp.ContentProvider.GetUserVideos(uint.Parse(UserId), 1);
				foreach (var item in userVideos.Items.Take(5))
				{
                    var vm = new VideoInfoControlViewModel(item.VideoId, isNgEnabled:false);
                    vm.SetTitle(item.Title);
                    vm.SetThumbnailImage(item.ThumbnailUrl.OriginalString);
                    VideoInfoItems.Add(vm);
				}
			}
			catch (Exception ex)
			{
				IsLoadFailed = true;
				NowLoading = false;
				Debug.WriteLine(ex.Message);
			}

			HasOwnerVideo = VideoInfoItems.Count != 0;


			if (HohoemaApp.LoginUserId.ToString() == UserId)
			{
				foreach (var item in HohoemaApp.UserMylistManager.UserMylists)
				{
					MylistGroups.Add(new MylistGroupListItem(item, PageManager));
				}
			}
			else
			{
				try
				{
					await Task.Delay(500);

					var mylistGroups = await HohoemaApp.ContentProvider.GetUserMylistGroups(UserId);
					foreach (var item in mylistGroups)
					{
						MylistGroups.Add(new MylistGroupListItem(item, PageManager));
					}
				}
				catch (Exception ex)
				{
					IsLoadFailed = true;
					Debug.WriteLine(ex.Message);
				}

			}

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






		private async Task<bool> FavoriteUser()
		{
			if (UserId == null) { return false; }

			var favManager = HohoemaApp.FollowManager;
			var result = await favManager.AddFollow(FollowItemType.User, UserId, UserName);

			return result == ContentManageResult.Success || result == ContentManageResult.Exist;
		}

		private async Task<bool> UnfavoriteUser()
		{
			if (UserId == null) { return false; }

			var favManager = HohoemaApp.FollowManager;
			var result = await favManager.RemoveFollow(FollowItemType.User, UserId);

			return result == ContentManageResult.Success;

		}


        public Database.Bookmark UserBookmark { get; private set; }

        private DelegateCommand _LogoutCommand;
        public DelegateCommand LogoutCommand
        {
            get
            {
                return _LogoutCommand
                    ?? (_LogoutCommand = new DelegateCommand(async () =>
                    {
                        await HohoemaApp.SignOut();

                        PageManager.OpenPage(HohoemaPageType.Login);
                    }));
            }
        }


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

		public ReactiveProperty<bool> IsFavorite { get; private set; }
		public ReactiveProperty<bool> CanChangeFavoriteState { get; private set; }
		private bool _NowProcessFavorite;


		public ReactiveCommand AddFavoriteCommand { get; private set; }
		public ReactiveCommand RemoveFavoriteCommand { get; private set; }

		public ReactiveProperty<bool> IsNGVideoOwner { get; private set; }


		public ObservableCollection<MylistGroupListItem> MylistGroups { get; private set; }
		public ObservableCollection<VideoInfoControlViewModel> VideoInfoItems { get; private set; }

		public ReactiveCommand OpenUserVideoPageCommand { get; private set; }
	}
}
