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
using Reactive.Bindings.Extensions;
using Reactive.Bindings;

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

			Lists.Add(new FavoriteListViewModel(HohoemaApp.FollowManager)
			{
				Name = "ユーザー",
				FavType = FollowItemType.User,
				MaxItemCount = HohoemaApp.FollowManager.User.MaxFollowItemCount,
				Items = HohoemaApp.FollowManager.User.FollowInfoItems
                    .ToReadOnlyReactiveCollection(x => new FavoriteItemViewModel(x, HohoemaApp.FollowManager, PageManager))

            });

			Lists.Add(new FavoriteListViewModel(HohoemaApp.FollowManager)
			{
				Name = "マイリスト",
				FavType = FollowItemType.Mylist,
				MaxItemCount = HohoemaApp.FollowManager.Mylist.MaxFollowItemCount,
				Items = HohoemaApp.FollowManager.Mylist.FollowInfoItems
                .ToReadOnlyReactiveCollection(x => new FavoriteItemViewModel(x, HohoemaApp.FollowManager, PageManager))
            });

			Lists.Add(new FavoriteListViewModel(HohoemaApp.FollowManager)
			{
				Name = "タグ",
				FavType = FollowItemType.Tag,
				MaxItemCount = HohoemaApp.FollowManager.Tag.MaxFollowItemCount,
				Items = HohoemaApp.FollowManager.Tag.FollowInfoItems
                .ToReadOnlyReactiveCollection(x => new FavoriteItemViewModel(x, HohoemaApp.FollowManager, PageManager))
            });

			Lists.Add(new FavoriteListViewModel(HohoemaApp.FollowManager)
			{
				Name = "コミュニティ",
				FavType = FollowItemType.Community,
				MaxItemCount = HohoemaApp.FollowManager.Community.MaxFollowItemCount,
				Items = HohoemaApp.FollowManager.Community.FollowInfoItems
                .ToReadOnlyReactiveCollection(x => new FavoriteItemViewModel(x, HohoemaApp.FollowManager, PageManager))
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
        public FollowManager FollowManager { get; }

		public ReadOnlyReactiveCollection<FavoriteItemViewModel> Items { get; set; }

        public FavoriteListViewModel(FollowManager followMan)
        {
            FollowManager = followMan;
        }

        private DelegateCommand<FavoriteItemViewModel> _SelectedCommand;
        public DelegateCommand<FavoriteItemViewModel> SelectedCommand
        {
            get
            {
                return _SelectedCommand
                    ?? (_SelectedCommand = new DelegateCommand<FavoriteItemViewModel>((itemVM) =>
                    {
                        itemVM.PrimaryCommand.Execute(null);
                    }));
            }
        }

        

    }

    public class FavoriteItemViewModel : HohoemaListingPageItemBase
	{
		
		public FavoriteItemViewModel(FollowItemInfo feedList, FollowManager followMan, PageManager pageManager)
		{
			Title = feedList.Name;
			ItemType = feedList.FollowItemType;
			SourceId = feedList.Id;

			_PageManager = pageManager;
            _FollowManager = followMan;

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

        private DelegateCommand _RemoveFavoriteCommand;
        public DelegateCommand RemoveFavoriteCommand
        {
            get
            {
                return _RemoveFavoriteCommand
                    ?? (_RemoveFavoriteCommand = new DelegateCommand(async () =>
                    {
                        switch (ItemType)
                        {
                            case FollowItemType.Tag:
                                await _FollowManager.Tag.RemoveFollow(SourceId);
                                break;
                            case FollowItemType.Mylist:
                                await _FollowManager.Mylist.RemoveFollow(SourceId);
                                break;
                            case FollowItemType.User:
                                await _FollowManager.User.RemoveFollow(SourceId);
                                break;
                            case FollowItemType.Community:
                                await _FollowManager.Community.RemoveFollow(SourceId);
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
        FollowManager _FollowManager;

    }

	
}
