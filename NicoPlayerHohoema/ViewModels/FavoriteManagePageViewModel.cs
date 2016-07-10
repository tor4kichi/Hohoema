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
using System.Collections.ObjectModel;

namespace NicoPlayerHohoema.ViewModels
{
	public class FavoriteManagePageViewModel : ViewModelBase
	{
		public FavoriteManagePageViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
		{
			_HohoemaApp = hohoemaApp;
			_PageManager = pageManager;

			Lists = new ObservableCollection<FavoriteListViewModel>();
		}

		public override async void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			while (_HohoemaApp.FavFeedManager == null)
			{
				await Task.Delay(100);
			}

			Lists.Clear();


			Lists.Add(new FavoriteListViewModel()
			{
				Name = "ユーザー",
				Items = _HohoemaApp.FavFeedManager.GetFavUserFeedListAll()
					.Select(x => new FavoriteItemViewModel(x, _PageManager))
					.ToList()
			});

			Lists.Add(new FavoriteListViewModel()
			{
				Name = "マイリスト",
				Items = _HohoemaApp.FavFeedManager.GetFavMylistFeedListAll()
					.Select(x => new FavoriteItemViewModel(x, _PageManager))
					.ToList()
			});

			Lists.Add(new FavoriteListViewModel()
			{
				Name = "タグ",
				Items = _HohoemaApp.FavFeedManager.GetFavTagFeedListAll()
					.Select(x => new FavoriteItemViewModel(x, _PageManager))
					.ToList()
			});

		}

		public ObservableCollection<FavoriteListViewModel> Lists { get; private set; }

		HohoemaApp _HohoemaApp;
		PageManager _PageManager;
	}

	public class FavoriteListViewModel : BindableBase
	{
		public string Name { get; set; }

		public List<FavoriteItemViewModel> Items { get; set; }
	}

	public class FavoriteItemViewModel : BindableBase
	{
		
		public FavoriteItemViewModel(FavFeedList feedList, PageManager pageManager)
		{
			Title = feedList.Name;
			ItemType = feedList.FavoriteItemType;
			SourceId = feedList.Id;

			_PageManager = pageManager;
		}


		private DelegateCommand _SelectedCommand;
		public DelegateCommand SelectedCommand
		{
			get
			{
				return _SelectedCommand
					?? (_SelectedCommand = new DelegateCommand(() => 
					{

						switch (ItemType)
						{
							case FavoriteItemType.Tag:
								var param = new SearchOption()
								{
									Keyword = this.SourceId,
									SearchTarget = SearchTarget.Tag,
									SortMethod = Mntone.Nico2.SortMethod.FirstRetrieve,
									SortDirection = Mntone.Nico2.SortDirection.Descending,
								}.ToParameterString();

								_PageManager.OpenPage(HohoemaPageType.Search, param);
								break;
							case FavoriteItemType.Mylist:
								_PageManager.OpenPage(HohoemaPageType.Mylist, this.SourceId);
								break;
							case FavoriteItemType.User:
								// TODO: UserVideo
								_PageManager.OpenPage(HohoemaPageType.UserVideo, this.SourceId);
								break;
							default:
								break;
						}
					}));
			}
		}

		public string Title { get; set; }
		public FavoriteItemType ItemType { get; set; }
		public string SourceId { get; set; }

		PageManager _PageManager;

	}

	
}
