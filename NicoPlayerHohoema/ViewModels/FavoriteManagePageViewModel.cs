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
using System.Threading;

namespace NicoPlayerHohoema.ViewModels
{
	public class FavoriteManagePageViewModel : HohoemaViewModelBase
	{
		public FavoriteManagePageViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
			: base(hohoemaApp, pageManager)
		{
			Lists = new ObservableCollection<FavoriteListViewModel>();
		}

		protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{

			while (HohoemaApp.FavManager == null)
			{
				await Task.Delay(100);
			}

			Lists.Clear();


			Lists.Add(new FavoriteListViewModel()
			{
				Name = "ユーザー",
				FavType = FavoriteItemType.User,
				MaxItemCount = HohoemaApp.FavManager.User.
				Items = HohoemaApp.FavManager.User.FavInfoItems
					.Select(x => new FavoriteItemViewModel(x, PageManager))
					.ToList()
			});

			Lists.Add(new FavoriteListViewModel()
			{
				Name = "マイリスト",
				FavType = FavoriteItemType.Mylist,
				Items = HohoemaApp.FavManager.Mylist.FavInfoItems
					.Select(x => new FavoriteItemViewModel(x, PageManager))
					.ToList()
			});

			Lists.Add(new FavoriteListViewModel()
			{
				Name = "タグ",
				FavType = FavoriteItemType.Tag,
				Items = HohoemaApp.FavManager.Tag.FavInfoItems
					.Select(x => new FavoriteItemViewModel(x, PageManager))
					.ToList()
			});

			Lists.Add(new FavoriteListViewModel()
			{
				Name = "コミュニティ",
				FavType = FavoriteItemType.Community,
				Items = HohoemaApp.FavManager.Community.FavInfoItems
					.Select(x => new FavoriteItemViewModel(x, PageManager))
					.ToList()
			});

		}

		public ObservableCollection<FavoriteListViewModel> Lists { get; private set; }
	}

	public class FavoriteListViewModel : BindableBase
	{
		public FavoriteItemType FavType { get; set; }
		public string Name { get; set; }
		public int MaxItemCount { get; set; }
		public int ItemCount => Items.Count;

		public List<FavoriteItemViewModel> Items { get; set; }
	}

	public class FavoriteItemViewModel : BindableBase
	{
		
		public FavoriteItemViewModel(FavInfo feedList, PageManager pageManager)
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
								var param = new SearchPagePayload(
									new TagSearchPagePayloadContent()
									{
										Keyword = this.SourceId,
										Sort = Mntone.Nico2.Sort.FirstRetrieve,
										Order = Mntone.Nico2.Order.Descending,
									}
									).ToParameterString();

								_PageManager.OpenPage(HohoemaPageType.Search, param);
								break;
							case FavoriteItemType.Mylist:
								_PageManager.OpenPage(HohoemaPageType.Mylist, this.SourceId);
								break;
							case FavoriteItemType.User:								
								_PageManager.OpenPage(HohoemaPageType.UserInfo, this.SourceId);
								break;
							case FavoriteItemType.Community:
								_PageManager.OpenPage(HohoemaPageType.Community, this.SourceId);
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
