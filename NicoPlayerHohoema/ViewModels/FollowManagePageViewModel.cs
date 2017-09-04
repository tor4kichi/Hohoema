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
using System.Diagnostics;

namespace NicoPlayerHohoema.ViewModels
{
	public class FollowManagePageViewModel : HohoemaViewModelBase
	{

        public ReactiveProperty<bool> NowUpdatingFavList { get; }
		public FollowManagePageViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
			: base(hohoemaApp, pageManager)
		{
			Lists = new ObservableCollection<FavoriteListViewModel>();

            NowUpdatingFavList = new ReactiveProperty<bool>();

            UpdateFavListCommand = new DelegateCommand<FavoriteListViewModel>((favListVM) =>
            {
                NowUpdatingFavList.Value = true;
                try
                {
                    favListVM.FollowGroup.SyncFollowItems().ConfigureAwait(false);
                }
                catch
                {
                    Debug.WriteLine($"{favListVM.FollowGroup.FollowItemType} のFollow List更新に失敗");
                }
                finally
                {
                    NowUpdatingFavList.Value = false;
                }
            });

            ChangeRequireServiceLevel(HohoemaAppServiceLevel.LoggedIn);
        }


        protected override async Task OnSignIn(ICollection<IDisposable> userSessionDisposer, CancellationToken cancelToken)
        {
            if (Lists.Count == 0)
            {
                Lists.Add(new FavoriteListViewModel("ユーザー", HohoemaApp.FollowManager.User, HohoemaApp.FollowManager, PageManager));

                Lists.Add(new FavoriteListViewModel("マイリスト", HohoemaApp.FollowManager.Mylist, HohoemaApp.FollowManager, PageManager));

                Lists.Add(new FavoriteListViewModel("タグ", HohoemaApp.FollowManager.Tag, HohoemaApp.FollowManager, PageManager));

                Lists.Add(new FavoriteListViewModel("コミュニティ", HohoemaApp.FollowManager.Community, HohoemaApp.FollowManager, PageManager));
            }

            await base.OnSignIn(userSessionDisposer, cancelToken);
        }

        protected override Task OnSignOut()
        {
            Lists.Clear();

            return base.OnSignOut();
        }

        public ObservableCollection<FavoriteListViewModel> Lists { get; private set; }

        public DelegateCommand<FavoriteListViewModel> UpdateFavListCommand { get; }


    }

    public class FavoriteListViewModel : BindableBase
	{
        public IFollowInfoGroup FollowGroup { get; }
        public FollowItemType FavType => FollowGroup.FollowItemType;
        public string Name { get;  }
        public uint MaxItemCount => FollowGroup.MaxFollowItemCount;
        public ReadOnlyReactiveProperty<int> ItemCount { get; }
        public FollowManager FollowManager { get; }
        public PageManager PageManager { get; }

        public ReadOnlyReactiveCollection<FavoriteItemViewModel> Items { get; set; }

        public FavoriteListViewModel(string label, IFollowInfoGroup followGroup, FollowManager followMan, PageManager pageManager)
        {
            Name = label;
            FollowGroup = followGroup;
            FollowManager = followMan;
            PageManager = pageManager;

            Items = followGroup.FollowInfoItems
                .ToReadOnlyReactiveCollection(x => new FavoriteItemViewModel(x, FollowManager, PageManager));
            ItemCount = Items.ObserveProperty(x => x.Count).ToReadOnlyReactiveProperty();
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
								_PageManager.OpenPage(HohoemaPageType.Mylist,
                                    new MylistPagePayload(this.SourceId) { Origin = PlaylistOrigin.OtherUser }.ToParameterString()
                                    );
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
