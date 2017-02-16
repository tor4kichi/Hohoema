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
using System.Windows.Input;

namespace NicoPlayerHohoema.ViewModels
{
	public class FollowManagePageViewModel : HohoemaViewModelBase
	{
		public FollowManagePageViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
			: base(hohoemaApp, pageManager)
		{
			Lists = new ObservableCollection<FavoriteListViewModel>();
		}

		protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{

			while (HohoemaApp.FollowManager == null)
			{
				await Task.Delay(100);
			}

			Lists.Clear();

			if (!HohoemaApp.FollowManagerUpdater.IsOneOrMoreUpdateCompleted)
			{
				await HohoemaApp.FollowManagerUpdater.WaitUpdate();
			}

			Lists.Add(new FavoriteListViewModel()
			{
				Name = "ユーザー",
				FavType = FollowItemType.User,
				MaxItemCount = HohoemaApp.FollowManager.User.MaxFollowItemCount,
				Items = HohoemaApp.FollowManager.User.FollowInfoItems
					.Select(x => new FavoriteItemViewModel(x, PageManager))
					.ToList()
			});

			Lists.Add(new FavoriteListViewModel()
			{
				Name = "マイリスト",
				FavType = FollowItemType.Mylist,
				MaxItemCount = HohoemaApp.FollowManager.Mylist.MaxFollowItemCount,
				Items = HohoemaApp.FollowManager.Mylist.FollowInfoItems
					.Select(x => new FavoriteItemViewModel(x, PageManager))
					.ToList()
			});

			Lists.Add(new FavoriteListViewModel()
			{
				Name = "タグ",
				FavType = FollowItemType.Tag,
				MaxItemCount = HohoemaApp.FollowManager.Tag.MaxFollowItemCount,
				Items = HohoemaApp.FollowManager.Tag.FollowInfoItems
					.Select(x => new FavoriteItemViewModel(x, PageManager))
					.ToList()
			});

			Lists.Add(new FavoriteListViewModel()
			{
				Name = "コミュニティ",
				FavType = FollowItemType.Community,
				MaxItemCount = HohoemaApp.FollowManager.Community.MaxFollowItemCount,
				Items = HohoemaApp.FollowManager.Community.FollowInfoItems
					.Select(x => new FavoriteItemViewModel(x, PageManager))
					.ToList()
			});

		}

		public ObservableCollection<FavoriteListViewModel> Lists { get; private set; }
	}

	public class FavoriteListViewModel : BindableBase
	{
		public FollowItemType FavType { get; set; }
		public string Name { get; set; }
		public uint MaxItemCount { get; set; }
		public int ItemCount => Items.Count;

		public List<FavoriteItemViewModel> Items { get; set; }
	}

	public class FavoriteItemViewModel : HohoemaListingPageItemBase
	{
		
		public FavoriteItemViewModel(FollowItemInfo feedList, PageManager pageManager)
		{
			Title = feedList.Name;
			ItemType = feedList.FollowItemType;
			SourceId = feedList.Id;

			_PageManager = pageManager;
		}


		private DelegateCommand _SelectedCommand;
		public override ICommand PrimaryCommand
		{
			get
			{
				return _SelectedCommand
					?? (_SelectedCommand = new DelegateCommand(() => 
					{

						switch (ItemType)
						{
							case FollowItemType.Tag:
                                var param =
                                    new TagSearchPagePayloadContent()
                                    {
                                        Keyword = this.SourceId,
                                        Sort = Mntone.Nico2.Sort.FirstRetrieve,
                                        Order = Mntone.Nico2.Order.Descending,
                                    };

                                _PageManager.Search(param);
								break;
							case FollowItemType.Mylist:
								_PageManager.OpenPage(HohoemaPageType.Mylist, this.SourceId);
								break;
							case FollowItemType.User:								
								_PageManager.OpenPage(HohoemaPageType.UserInfo, this.SourceId);
								break;
							case FollowItemType.Community:
								_PageManager.OpenPage(HohoemaPageType.Community, this.SourceId);
								break;
							default:
								break;
						}
					}));
			}
		}

		public FollowItemType ItemType { get; set; }
		public string SourceId { get; set; }

		PageManager _PageManager;

	}

	
}
