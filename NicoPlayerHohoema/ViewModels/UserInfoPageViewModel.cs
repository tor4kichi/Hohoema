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

namespace NicoPlayerHohoema.ViewModels
{
	public class UserInfoPageViewModel : HohoemaViewModelBase
	{
		public UserInfoPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager) 
			: base(hohoemaApp, pageManager)
		{
			HasOwnerVideo = true;

			MylistGroups = new ObservableCollection<MylistGroupListItem>();
			VideoInfoItems = new ObservableCollection<VideoInfoControlViewModel>();

			IsFavorite = new ReactiveProperty<bool>()
				.AddTo(_CompositeDisposable);
			CanAddFavorite = new ReactiveProperty<bool>()
				.AddTo(_CompositeDisposable);


			AddFavoriteCommand = CanAddFavorite
				.ToReactiveCommand()
				.AddTo(_CompositeDisposable);

			RemoveFavoriteCommand = IsFavorite
				.ToReactiveCommand()
				.AddTo(_CompositeDisposable);

			AddFavoriteCommand.Where(_ => UserId != null)
				.Subscribe(async x => 
				{
					var result = await HohoemaApp.FavFeedManager.AddFav(FavoriteItemType.User, UserId);
					if (result == ContentManageResult.Success)
					{
						IsFavorite.Value = true;
						CanAddFavorite.Value = false;
					}
				})
				.AddTo(_CompositeDisposable);

			RemoveFavoriteCommand.Where(_ => UserId != null)
				.Subscribe(async x =>
				{
					var result = await HohoemaApp.FavFeedManager.RemoveFav(FavoriteItemType.User, UserId);
					if (result == ContentManageResult.Success)
					{
						IsFavorite.Value = false;
						CanAddFavorite.Value = HohoemaApp.FavFeedManager.CanMoreAddFavorite(FavoriteItemType.User);
					}
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


		}


		public override async void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
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

			IsLoadFailed = false;

			MylistGroups.Clear();
			VideoInfoItems.Clear();

			try
			{
				var userInfo = await HohoemaApp.ContentFinder.GetUserDetail(UserId);

				var user = userInfo;
				UserName = user.Nickname;
				UserIconUri = new Uri(user.ThumbnailUri);
				Description = user.Description;

				BirthDay = user.BirthDay;
				FavCount = user.FavCount;
				StampCount = user.StampCount;
				if (user.Gender == null)
				{
					Gender = "未公開";
				}
				else
				{
					switch (user.Gender.Value)
					{
						case Sex.Male:
							Gender = "男性";
							break;
						case Sex.Female:
							Gender = "女性";
							break;
						default:
							break;
					}
				}

				Region = user.Region;
				VideoCount = user.TotalVideoCount;
				IsVideoPrivate = user.IsOwnerVideoPrivate;
			}
			catch
			{
				IsLoadFailed = true;
				NowLoading = false;
			}


			if (UserId == null) { return; }

			UpdateTitle($"{UserName} さんのユーザー情報");

			try
			{
				var favManager = HohoemaApp.FavFeedManager;
				IsFavorite.Value = favManager.IsFavoriteItem(FavoriteItemType.User, UserId);
				if (!IsFavorite.Value)
				{
					CanAddFavorite.Value = favManager.CanMoreAddFavorite(FavoriteItemType.User);
				}
				else
				{
					CanAddFavorite.Value = false;
				}
			}
			catch (Exception ex)
			{
				CanAddFavorite.Value = false;
				IsLoadFailed = true;
				NowLoading = false;
				Debug.WriteLine(ex.Message);
			}

			try
			{
				await Task.Delay(500);

				var userVideos = await HohoemaApp.ContentFinder.GetUserVideos(uint.Parse(UserId), 1);
				foreach (var item in userVideos.Items.Take(5))
				{
					var nicoVideo = await HohoemaApp.MediaManager.GetNicoVideo(item.VideoId);
					VideoInfoItems.Add(new VideoInfoControlViewModel(nicoVideo, PageManager));
				}
			}
			catch (Exception ex)
			{
				IsLoadFailed = true;
				NowLoading = false;
				Debug.WriteLine(ex.Message);
			}

			HasOwnerVideo = VideoInfoItems.Count != 0;

			try
			{
				await Task.Delay(500);

				var mylistGroups = await HohoemaApp.ContentFinder.GetUserMylistGroups(UserId);
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

			


			NowLoading = false;

			base.OnNavigatedTo(e, viewModelState);
		}


		public string UserId { get; private set; }
		public bool IsLoadFailed { get; private set; }


		private string _UserName;
		public string UserName
		{
			get { return _UserName; }
			set { SetProperty(ref _UserName, value); }
		}


		private Uri _UserIconUri;
		public Uri UserIconUri
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
		public uint FavCount
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
		public ReactiveProperty<bool> CanAddFavorite { get; private set; }

		public ReactiveCommand AddFavoriteCommand { get; private set; }
		public ReactiveCommand RemoveFavoriteCommand { get; private set; }

		

		public ObservableCollection<MylistGroupListItem> MylistGroups { get; private set; }
		public ObservableCollection<VideoInfoControlViewModel> VideoInfoItems { get; private set; }

		public ReactiveCommand OpenUserVideoPageCommand { get; private set; }
	}
}
