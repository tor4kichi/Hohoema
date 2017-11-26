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
        public string Label { get;  }
        public uint MaxItemCount => FollowGroup.MaxFollowItemCount;
        public IReadOnlyReactiveProperty<int> ItemCount { get; }
        public FollowManager FollowManager { get; }
        public PageManager PageManager { get; }

        public bool IsSyncFailed { get; }

        public ReadOnlyObservableCollection<FavoriteItemViewModel> Items { get; set; }

        public FavoriteListViewModel(string label, IFollowInfoGroup followGroup, FollowManager followMan, PageManager pageManager)
        {
            Label = label;
            FollowGroup = followGroup;
            FollowManager = followMan;
            PageManager = pageManager;
            IsSyncFailed = FollowGroup.IsFailedUpdate;

            Items = followGroup.FollowInfoItems?
                .ToReadOnlyReactiveCollection(x => CreateFavVM(x)) 
                ?? new ReadOnlyObservableCollection<FavoriteItemViewModel>(new ObservableCollection<FavoriteItemViewModel>());
            ItemCount = Items?.ObserveProperty(x => x.Count).ToReadOnlyReactiveProperty() 
                ?? new ReactiveProperty<int>(0).ToReadOnlyReactiveProperty();
        }


        private static FavoriteItemViewModel CreateFavVM(FollowItemInfo favItem)
        {
            switch (favItem.FollowItemType)
            {
                case FollowItemType.Tag:
                    return new TagFavItemVM(favItem);
                case FollowItemType.Mylist:
                    return new MylistFavItemVM(favItem);
                case FollowItemType.User:
                    return new UserFavItemVM(favItem);
                case FollowItemType.Community:
                    return new CommunityFavItemVM(favItem);
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class TagFavItemVM : FavoriteItemViewModel, Interfaces.ISearchWithtag
    {
        public TagFavItemVM(FollowItemInfo feedList) : base(feedList)
        {
        }

        public string Tag => SourceId;
    }

    public class MylistFavItemVM : FavoriteItemViewModel, Interfaces.IMylist
    {
        public MylistFavItemVM(FollowItemInfo feedList) : base(feedList)
        {
        }

        public string Id => SourceId;
    }


    public class UserFavItemVM : FavoriteItemViewModel, Interfaces.IUser
    {
        public UserFavItemVM(FollowItemInfo feedList) : base(feedList)
        {
        }

        public string Id => SourceId;
    }

    public class CommunityFavItemVM : FavoriteItemViewModel, Interfaces.ICommunity
    {
        public CommunityFavItemVM(FollowItemInfo feedList) : base(feedList)
        {
        }

        public string Id => SourceId;
    }


    public class FavoriteItemViewModel : HohoemaListingPageItemBase
	{
		public FavoriteItemViewModel(FollowItemInfo feedList)
		{
            FollowItemInfo = feedList;
            Label = feedList.Name;
			ItemType = feedList.FollowItemType;
			SourceId = feedList.Id;
        }

        
        /*
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
        */

        public FollowItemType ItemType { get; set; }
		public string SourceId { get; set; }

        public FollowItemInfo FollowItemInfo { get; }
    }

	
}
