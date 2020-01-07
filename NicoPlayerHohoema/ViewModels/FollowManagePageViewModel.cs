using NicoPlayerHohoema.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using Prism.Commands;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Input;
using Reactive.Bindings.Extensions;
using Reactive.Bindings;
using System.Diagnostics;
using NicoPlayerHohoema.Services;
using Unity;
using Unity.Resolution;
using Prism.Unity;
using NicoPlayerHohoema.Services.Page;

namespace NicoPlayerHohoema.ViewModels
{
	public class FollowManagePageViewModel : HohoemaViewModelBase
	{
     	public FollowManagePageViewModel(
            PageManager pageManager,
            NiconicoSession niconicoSession,
            FollowManager followManager
            )
		{
            PageManager = pageManager;
            NiconicoSession = niconicoSession;
            FollowManager = followManager;

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


            if (NiconicoSession.IsLoggedIn)
            {
//                Lists.Add(new FavoriteListViewModel("ユーザー", FollowManager.User, FollowManager, PageManager));
//                Lists.Add(new FavoriteListViewModel("マイリスト", FollowManager.Mylist, FollowManager, PageManager));
//                Lists.Add(new FavoriteListViewModel("タグ", FollowManager.Tag, FollowManager, PageManager));
//                Lists.Add(new FavoriteListViewModel("コミュニティ", FollowManager.Community, FollowManager, PageManager));
//                Lists.Add(new FavoriteListViewModel("チャンネル", FollowManager.Channel, FollowManager, PageManager));
            }
        }

        public ReactiveProperty<bool> NowUpdatingFavList { get; }

        public ObservableCollection<FavoriteListViewModel> Lists { get; private set; }

        public DelegateCommand<FavoriteListViewModel> UpdateFavListCommand { get; }
        public PageManager PageManager { get; }
        public NiconicoSession NiconicoSession { get; }
        public FollowManager FollowManager { get; }

        protected override bool TryGetHohoemaPin(out HohoemaPin pin)
        {
            pin = null;
            return false;
        }
    }

    public class FavoriteListViewModel : BindableBase
	{
        public IFollowInfoGroup FollowGroup { get; }
        public FollowItemType FavType => FollowGroup.FollowItemType;
        public string Label { get;  }
        public uint MaxItemCount => FollowGroup.MaxFollowItemCount;
        public bool IsCountInfinity => MaxItemCount == uint.MaxValue;

        public IReadOnlyReactiveProperty<int> ItemCount { get; }
        public FollowManager FollowManager { get; }
        public PageManager PageManager { get; }

        public bool IsSyncFailed { get; }

        //public ReadOnlyObservableCollection<FavoriteItemViewModel> Items { get; set; }

        //Func<FollowItemInfo, FavoriteItemViewModel> _ItemVMFactory;
        //Func<FollowItemInfo, FavoriteItemViewModel> ItemVMFactory
        //{
        //    get
        //    {
        //        return _ItemVMFactory ?? (_ItemVMFactory = MakeFavItemVMFactory(FavType));
                    
                
        //    }
        //}

        //static Func<FollowItemInfo, FavoriteItemViewModel> MakeFavItemVMFactory(FollowItemType type)
        //{
        //    Func<FollowItemInfo, FavoriteItemViewModel> fac = null;
        //    var unityContainer = App.Current.Container.GetContainer();
        //    switch (type)
        //    {
        //        case FollowItemType.Tag:
        //            fac = item => unityContainer.Resolve<TagFavItemVM>(new ParameterOverride("follow", item));
        //            break;
        //        case FollowItemType.Mylist:
        //            fac = item => unityContainer.Resolve<MylistFavItemVM>(new ParameterOverride("follow", item));
        //            break;
        //        case FollowItemType.User:
        //            fac = item => unityContainer.Resolve<UserFavItemVM>(new ParameterOverride("follow", item));
        //            break;
        //        case FollowItemType.Community:
        //            fac = item => unityContainer.Resolve<CommunityFavItemVM>(new ParameterOverride("follow", item));
        //            break;
        //        case FollowItemType.Channel:
        //            fac = item => unityContainer.Resolve<ChannelFavItemVM>(new ParameterOverride("follow", item));
        //            break;
        //        default:
        //            throw new NotSupportedException();
        //    }

        //    return fac;
        //}


        //public FavoriteListViewModel(string label, IFollowInfoGroup followGroup, FollowManager followMan, PageManager pageManager)
        //{
        //    Label = label;
        //    FollowGroup = followGroup;
        //    FollowManager = followMan;
        //    PageManager = pageManager;
        //    IsSyncFailed = FollowGroup.IsFailedUpdate;

        //    Items = followGroup.FollowInfoItems?
        //        .ToReadOnlyReactiveCollection(x => ItemVMFactory(x)) 
        //        ?? new ReadOnlyObservableCollection<FavoriteItemViewModel>(new ObservableCollection<FavoriteItemViewModel>());
        //    ItemCount = Items?.ObserveProperty(x => x.Count).ToReadOnlyReactiveProperty() 
        //        ?? new ReactiveProperty<int>(0).ToReadOnlyReactiveProperty();
        //}
        
    }

 //   public class TagFavItemVM : FavoriteItemViewModel, Interfaces.ISearchWithtag
 //   {
 //       public TagFavItemVM(
 //           FollowItemInfo follow,
 //           Services.NiconicoFollowToggleButtonService followToggleButtonService,
 //           Models.Subscription.SubscriptionManager subscriptionManager,
 //           Commands.Subscriptions.CreateSubscriptionGroupCommand createSubscriptionGroupCommand
 //           )
 //           : base(follow, followToggleButtonService, subscriptionManager, createSubscriptionGroupCommand)
 //       {
 //           FollowToggleButtonService.SetFollowTarget(this);
 //       }

 //       public string Tag => SourceId;

 //       public string Id => SourceId;
 //   }

 //   public class MylistFavItemVM : FavoriteItemViewModel, Interfaces.IMylistItem
 //   {
 //       public MylistFavItemVM(
 //           FollowItemInfo follow,
 //           Services.NiconicoFollowToggleButtonService followToggleButtonService,
 //           Models.Subscription.SubscriptionManager subscriptionManager,
 //           Commands.Subscriptions.CreateSubscriptionGroupCommand createSubscriptionGroupCommand
 //           )
 //           : base(follow, followToggleButtonService, subscriptionManager, createSubscriptionGroupCommand)
 //       {
 //           FollowToggleButtonService.SetFollowTarget(this);
 //       }

 //       public string Id => SourceId;
 //   }


 //   public class UserFavItemVM : FavoriteItemViewModel, Interfaces.IUser
 //   {
 //       public UserFavItemVM(
 //           FollowItemInfo follow,
 //           Services.NiconicoFollowToggleButtonService followToggleButtonService,
 //           Models.Subscription.SubscriptionManager subscriptionManager,
 //           Commands.Subscriptions.CreateSubscriptionGroupCommand createSubscriptionGroupCommand
 //           )
 //           : base(follow, followToggleButtonService, subscriptionManager, createSubscriptionGroupCommand)
 //       {
 //           FollowToggleButtonService.SetFollowTarget(this);
 //       }

 //       public string Id => SourceId;
 //   }

 //   public class CommunityFavItemVM : FavoriteItemViewModel, Interfaces.ICommunity
 //   {
 //       public CommunityFavItemVM(
 //           FollowItemInfo follow,
 //           Services.NiconicoFollowToggleButtonService followToggleButtonService,
 //           Models.Subscription.SubscriptionManager subscriptionManager,
 //           Commands.Subscriptions.CreateSubscriptionGroupCommand createSubscriptionGroupCommand
 //           )
 //           : base(follow, followToggleButtonService, subscriptionManager, createSubscriptionGroupCommand)
 //       {
 //           FollowToggleButtonService.SetFollowTarget(this);
 //       }

 //       public string Id => SourceId;
 //   }

 //   public class ChannelFavItemVM : FavoriteItemViewModel, Interfaces.IChannel
 //   {
 //       public ChannelFavItemVM(
 //           FollowItemInfo follow,
 //           Services.NiconicoFollowToggleButtonService followToggleButtonService,
 //           Models.Subscription.SubscriptionManager subscriptionManager,
 //           Commands.Subscriptions.CreateSubscriptionGroupCommand createSubscriptionGroupCommand
 //           ) 
 //           : base(follow, followToggleButtonService, subscriptionManager, createSubscriptionGroupCommand)
 //       {
 //           FollowToggleButtonService.SetFollowTarget(this);
 //       }

 //       public string Id => SourceId;
 //   }


 //   public class FavoriteItemViewModel : HohoemaListingPageItemBase
	//{
	//	public FavoriteItemViewModel(
 //           FollowItemInfo follow,
 //           Services.NiconicoFollowToggleButtonService followToggleButtonService,
 //           Models.Subscription.SubscriptionManager subscriptionManager,
 //           Commands.Subscriptions.CreateSubscriptionGroupCommand createSubscriptionGroupCommand
 //           )
	//	{
 //           FollowItemInfo = follow;
 //           FollowToggleButtonService = followToggleButtonService;
 //           SubscriptionManager = subscriptionManager;
 //           CreateSubscriptionGroupCommand = createSubscriptionGroupCommand;
 //           Label = follow.Name;
	//		ItemType = follow.FollowItemType;
	//		SourceId = follow.Id;
 //       }

        
 //       /*
 //       private DelegateCommand _RemoveFavoriteCommand;
 //       public DelegateCommand RemoveFavoriteCommand
 //       {
 //           get
 //           {
 //               return _RemoveFavoriteCommand
 //                   ?? (_RemoveFavoriteCommand = new DelegateCommand(async () =>
 //                   {
 //                       switch (ItemType)
 //                       {
 //                           case FollowItemType.Tag:
 //                               await _FollowManager.Tag.RemoveFollow(SourceId);
 //                               break;
 //                           case FollowItemType.Mylist:
 //                               await _FollowManager.Mylist.RemoveFollow(SourceId);
 //                               break;
 //                           case FollowItemType.User:
 //                               await _FollowManager.User.RemoveFollow(SourceId);
 //                               break;
 //                           case FollowItemType.Community:
 //                               await _FollowManager.Community.RemoveFollow(SourceId);
 //                               break;
 //                           default:
 //                               break;
 //                       }
 //                   }));
 //           }
 //       }
 //       */

 //       public FollowItemType ItemType { get; set; }
	//	public string SourceId { get; set; }

 //       public FollowItemInfo FollowItemInfo { get; }
 //       public NiconicoFollowToggleButtonService FollowToggleButtonService { get; }
 //       public FollowManager FollowManager { get; }
 //       public Models.Subscription.SubscriptionManager SubscriptionManager { get; }
 //       public Commands.Subscriptions.CreateSubscriptionGroupCommand CreateSubscriptionGroupCommand { get; }
 //   }

	
}
