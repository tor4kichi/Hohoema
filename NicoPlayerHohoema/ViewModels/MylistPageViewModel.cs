using NicoPlayerHohoema.Models;
using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using Prism.Mvvm;
using Prism.Commands;
using Mntone.Nico2.Mylist;
using System.Collections.ObjectModel;
using Mntone.Nico2;
using Reactive.Bindings;
using System.Reactive.Linq;
using System.Diagnostics;
using NicoPlayerHohoema.Util;
using Windows.UI.Xaml;
using Reactive.Bindings.Extensions;
using System.Threading;

namespace NicoPlayerHohoema.ViewModels
{
	public class MylistPageViewModel : HohoemaVideoListingPageViewModelBase<VideoInfoControlViewModel>
	{
		public MylistPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
			: base(hohoemaApp, pageManager, isRequireSignIn: true)
		{
			IsFavoriteMylist = new ReactiveProperty<bool>(mode:ReactivePropertyMode.DistinctUntilChanged)
				.AddTo(_CompositeDisposable);
			CanChangeFavoriteMylistState = new ReactiveProperty<bool>()
				.AddTo(_CompositeDisposable);


			IsFavoriteMylist
				.Where(x => MylistGroupId != null)
				.Subscribe(async x => 
				{
					if (_NowProcessFavorite) { return; }

					_NowProcessFavorite = true;

					CanChangeFavoriteMylistState.Value = false;
					if (x)
					{
						if (await FavoriteMylist())
						{
							Debug.WriteLine(_MylistTitle + "のマイリストをお気に入り登録しました.");
						}
						else
						{
							// お気に入り登録に失敗した場合は状態を差し戻し
							Debug.WriteLine(_MylistTitle + "のマイリストをお気に入り登録に失敗");
							IsFavoriteMylist.Value = false;
						}
					}
					else
					{
						if (await UnfavoriteMylist())
						{
							Debug.WriteLine(_MylistTitle + "のマイリストをお気に入り解除しました.");
						}
						else
						{
							// お気に入り解除に失敗した場合は状態を差し戻し
							Debug.WriteLine(_MylistTitle + "のマイリストをお気に入り解除に失敗");
							IsFavoriteMylist.Value = true;
						}
					}

					CanChangeFavoriteMylistState.Value = 
						IsFavoriteMylist.Value == true 
						|| HohoemaApp.FavFeedManager.CanMoreAddFavorite(FavoriteItemType.Mylist);


					_NowProcessFavorite = false;
				})
				.AddTo(_CompositeDisposable);
		}



		private async Task<bool> FavoriteMylist()
		{
			if (MylistGroupId == null) { return false; }

			var favManager = HohoemaApp.FavFeedManager;
			var result = await favManager.AddFav(FavoriteItemType.Mylist, MylistGroupId, MylistTitle);

			return result == ContentManageResult.Success || result == ContentManageResult.Exist;
		}

		private async Task<bool> UnfavoriteMylist()
		{
			if (MylistGroupId == null) { return false; }

			var favManager = HohoemaApp.FavFeedManager;
			var result = await favManager.RemoveFav(FavoriteItemType.Mylist, MylistGroupId);

			return result == ContentManageResult.Success;

		}

		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (e.Parameter is string)
			{
				MylistGroupId = e.Parameter as string;
			}

			base.OnNavigatedTo(e, viewModelState);
		}

		protected override async Task ListPageNavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (MylistGroupId == null)
			{
				return;
			}



			// お気に入り状態の取得
			_NowProcessFavorite = true;

			var favManager = HohoemaApp.FavFeedManager;
			IsFavoriteMylist.Value = favManager.IsFavoriteItem(FavoriteItemType.Mylist, MylistGroupId);

			CanChangeFavoriteMylistState.Value =
				IsFavoriteMylist.Value == true
				|| favManager.CanMoreAddFavorite(FavoriteItemType.Mylist);

			_NowProcessFavorite = false;







			if (MylistGroupId == "0")
			{
				MylistTitle = "とりあえずマイリスト";
				OwnerUserId = HohoemaApp.LoginUserId.ToString();

				var loginUserInfo = await HohoemaApp.NiconicoContext.User.GetInfoAsync();
				UserName = loginUserInfo.Name;
			}
			else
			{
				try
				{
					var response = await HohoemaApp.ContentFinder.GetMylist(MylistGroupId);
					MylistTitle = StringExtention.DecodeUTF8(response.Name);
					MylistDescription = StringExtention.DecodeUTF8(response.Description);

					OwnerUserId = response.User_id;

					await Task.Delay(500);

					var userDetail = await HohoemaApp.ContentFinder.GetUserDetail(OwnerUserId);

					UserName = userDetail.Nickname;
				}
				catch
				{

				}
			}

			UpdateTitle(MylistTitle);



		}

		

		protected override IIncrementalSource<VideoInfoControlViewModel> GenerateIncrementalSource()
		{
			if (MylistGroupId == "0")
			{
				return new DeflistMylistIncrementalSource(HohoemaApp, PageManager);
			}
			else
			{
				return new MylistIncrementalSource(MylistGroupId, HohoemaApp, PageManager);
			}
		}




		private DelegateCommand _OpenUserPageCommand;
		public DelegateCommand OpenUserPageCommand
		{
			get
			{
				return _OpenUserPageCommand
					?? (_OpenUserPageCommand = new DelegateCommand(() =>
					{
						PageManager.OpenPage(HohoemaPageType.UserInfo, OwnerUserId);
					}));
			}
		}



		private bool _NowProcessFavorite;



		private string _MylistTitle;
		public string MylistTitle
		{
			get { return _MylistTitle; }
			set { SetProperty(ref _MylistTitle, value); }
		}

		private string _MylistDescription;
		public string MylistDescription
		{
			get { return _MylistDescription; }
			set { SetProperty(ref _MylistDescription, value); }
		}

		public string MylistGroupId { get; private set; }

		public string OwnerUserId { get; private set; }


		private string _UserName;
		public string UserName
		{
			get { return _UserName; }
			set { SetProperty(ref _UserName, value); }
		}

		public ReactiveProperty<bool> IsFavoriteMylist { get; private set; }
		public ReactiveProperty<bool> CanChangeFavoriteMylistState { get; private set; }

		protected override uint IncrementalLoadCount
		{
			get
			{
				return 20;
			}
		}
	}

	public class DeflistMylistIncrementalSource : IIncrementalSource<VideoInfoControlViewModel>
	{
		public DeflistMylistIncrementalSource(HohoemaApp hohoemaApp, PageManager pageManager)
		{
			_HohoemaApp = hohoemaApp;
			_PageManager = pageManager;
		}


		public async Task<IEnumerable<VideoInfoControlViewModel>> GetPagedItems(uint head, uint pageSize)
		{
			List<VideoInfoControlViewModel> list = new List<VideoInfoControlViewModel>();

			// TODO とりまのマイリストアイテム一覧取得
			if (_MylistData == null || head == 1)
			{
				_MylistData = await _HohoemaApp.NiconicoContext.Mylist.GetMylistItemListAsync("0");
			}

			foreach (var item in _MylistData.Skip((int)head).Take((int)pageSize))
			{
				var nicoVideo = await _HohoemaApp.MediaManager.GetNicoVideo(item.ItemId);
				list.Add(new VideoInfoControlViewModel(item, nicoVideo, _PageManager));
			}

			return list;
		}

		List<MylistData> _MylistData;

		HohoemaApp _HohoemaApp;
		PageManager _PageManager;
	}

	public class MylistIncrementalSource : IIncrementalSource<VideoInfoControlViewModel>
	{
		public MylistIncrementalSource(string mylistGroupId, HohoemaApp hohoemaApp, PageManager pageManager)
		{
			MylistGroupId = mylistGroupId;

			_HohoemaApp = hohoemaApp;
			_PageManager = pageManager;
		}



		public async Task<IEnumerable<VideoInfoControlViewModel>> GetPagedItems(uint pageIndex, uint pageSize)
		{
			List<VideoInfoControlViewModel> list = new List<VideoInfoControlViewModel>();

			if (MylistGroupId == null || MylistGroupId == "0")
			{
				throw new Exception();
			}

			var res = await _HohoemaApp.ContentFinder.GetMylistItems(MylistGroupId, pageIndex, pageSize);


			foreach (var item in res.Video_info)
			{
				var nicoVideo = await _HohoemaApp.MediaManager.GetNicoVideo(item.Video.Id);
				list.Add(new VideoInfoControlViewModel(item, nicoVideo, _PageManager));
			}
			

			return list;
		}

		public string MylistGroupId { get; private set; }

		HohoemaApp _HohoemaApp;
		PageManager _PageManager;

	}




}
