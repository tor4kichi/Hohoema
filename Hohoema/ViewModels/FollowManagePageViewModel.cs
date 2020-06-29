using Hohoema.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using Prism.Commands;
using System.Collections.ObjectModel;
using System.Threading;
using Reactive.Bindings;
using System.Diagnostics;
using Hohoema.UseCase;
using Hohoema.ViewModels.Pages;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Follow;

namespace Hohoema.ViewModels
{
	public class FollowManagePageViewModel : HohoemaViewModelBase
	{
     	public FollowManagePageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            PageManager pageManager,
            NiconicoSession niconicoSession,
            FollowManager followManager
            )
		{
            ApplicationLayoutManager = applicationLayoutManager;
            PageManager = pageManager;
            NiconicoSession = niconicoSession;
            FollowManager = followManager;

            NowUpdatingFavList = new ReactiveProperty<bool>();

            UpdateFavListCommand = new DelegateCommand<IFollowInfoGroup>(async (group) =>
            {
                NowUpdatingFavList.Value = true;
                try
                {
                    await FollowManager.SyncAll();
                }
                catch
                {
                    Debug.WriteLine($"{group.FollowItemType} のFollow List更新に失敗");
                }
                finally
                {
                    NowUpdatingFavList.Value = false;
                }
            });


            FollowGroups = new ObservableCollection<IFollowInfoGroup>()
            {
                FollowManager.User,
                FollowManager.Mylist,
                FollowManager.Tag,
                FollowManager.Community,
                FollowManager.Channel,
            };
        }

        public ReactiveProperty<bool> NowUpdatingFavList { get; }

        public ObservableCollection<IFollowInfoGroup> FollowGroups { get; private set; }

        public DelegateCommand<IFollowInfoGroup> UpdateFavListCommand { get; }
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public PageManager PageManager { get; }
        public NiconicoSession NiconicoSession { get; }
        public FollowManager FollowManager { get; }


        private DelegateCommand<FollowItemInfo> _RemoveFavoriteCommand;
        public DelegateCommand<FollowItemInfo> RemoveFavoriteCommand
        {
            get
            {
                return _RemoveFavoriteCommand
                    ?? (_RemoveFavoriteCommand = new DelegateCommand<FollowItemInfo>(async (followItem) =>
                    {
                        switch (followItem.FollowItemType)
                        {
                            case FollowItemType.Tag:
                                await FollowManager.Tag.RemoveFollow(followItem.Id);
                                break;
                            case FollowItemType.Mylist:
                                await FollowManager.Mylist.RemoveFollow(followItem.Id);
                                break;
                            case FollowItemType.User:
                                await FollowManager.User.RemoveFollow(followItem.Id);
                                break;
                            case FollowItemType.Community:
                                await FollowManager.Community.RemoveFollow(followItem.Id);
                                break;
                            case FollowItemType.Channel:
                                await FollowManager.Channel.RemoveFollow(followItem.Id);
                                break;
                            default:
                                break;
                        }
                    }));
            }
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
