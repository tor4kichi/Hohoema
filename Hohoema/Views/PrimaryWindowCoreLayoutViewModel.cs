using Hohoema.Models.Application;
using Hohoema.Models.LocalMylist;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Mylist.LoginUser;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Pins;
using Hohoema.Models.Playlist;
using Hohoema.Services;
using Hohoema.Services.LocalMylist;
using Hohoema.Services.Niconico.Account;
using Hohoema.Services.Player;
using Hohoema.Services.Navigations;
using Hohoema.Services.Playlist;
using Hohoema.Services;
using Hohoema.ViewModels.Navigation.Commands;
using Hohoema.ViewModels.Niconico.Account;
using Hohoema.ViewModels.Niconico.Live;
using Hohoema.ViewModels.Niconico.Video;
using Hohoema.ViewModels.PrimaryWindowCoreLayout;
using I18NPortable;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Messaging;
using NiconicoToolkit;
using NiconicoToolkit.Live;
using NiconicoToolkit.Live.Notify;
using NiconicoToolkit.Mylist;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using Windows.System;
using ZLogger;
using Hohoema.Services.Navigations;
using System.Collections.Generic;

namespace Hohoema.ViewModels
{
    public partial class PrimaryWindowCoreLayoutViewModel : ObservableObject, IRecipient<SettingsRestoredMessage>
    {
        void IRecipient<SettingsRestoredMessage>.Receive(SettingsRestoredMessage message)
        {
            _pinsMenuSubItemViewModel.Reset();
        }



        public NiconicoSession NiconicoSession { get; }
        public PageManager PageManager { get; }
        public PinSettings PinSettings { get; }
        public AppearanceSettings AppearanceSettings { get; }
        public SearchCommand SearchCommand { get; }
        public PrimaryViewPlayerManager PrimaryViewPlayerManager { get; }
        public ObservableMediaPlayer ObservableMediaPlayer { get; }
        public NiconicoLoginService NiconicoLoginService { get; }
        public LogoutFromNiconicoCommand LogoutFromNiconicoCommand { get; }
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public RestoreNavigationManager RestoreNavigationManager { get; }
        public VideoItemsSelectionContext VideoItemsSelectionContext { get; }
        private readonly LoginUserOwnedMylistManager _userMylistManager;
        private readonly LocalMylistManager _localMylistManager;
        public OpenLiveContentCommand OpenLiveContentCommand { get; }

        private readonly ILogger _logger;
        private readonly DialogService _dialogService;
        private readonly NotificationService _notificationService;

        public ObservableCollection<SearchAutoSuggestItemViewModel> SearchAutoSuggestItems { get; }

        public ObservableCollection<HohoemaListingPageItemBase> MenuItems_Offline { get; private set; }
        public ObservableCollection<HohoemaListingPageItemBase> MenuItems_LoggedIn { get; private set; }

        internal readonly QueueMenuItemViewModel _queueMenuItemViewModel;
        internal readonly PinsMenuSubItemViewModel _pinsMenuSubItemViewModel;

        public LocalMylistSubMenuItemViewModel _localMylistMenuSubItemViewModel { get; }

        public PrimaryWindowCoreLayoutViewModel(
            ILoggerFactory loggerFactory,
            NiconicoSession niconicoSession,
            PageManager pageManager,
            PinSettings pinSettings,
            AppearanceSettings appearanceSettings,
            SearchCommand searchCommand,
            DialogService dialogService,
            NotificationService notificationService,
            PrimaryViewPlayerManager primaryViewPlayerManager,
            ObservableMediaPlayer observableMediaPlayer,
            NiconicoLoginService niconicoLoginService,
            LogoutFromNiconicoCommand logoutFromNiconicoCommand,
            ApplicationLayoutManager applicationLayoutManager,
            RestoreNavigationManager restoreNavigationManager,
            VideoItemsSelectionContext videoItemsSelectionContext,
            QueueMenuItemViewModel queueMenuItemViewModel,
            LoginUserOwnedMylistManager userMylistManager,
            LocalMylistManager localMylistManager,
            OpenLiveContentCommand openLiveContentCommand
            )
        {
            _logger = loggerFactory.CreateLogger<PrimaryWindowCoreLayoutViewModel>();
            NiconicoSession = niconicoSession;
            PageManager = pageManager;
            PinSettings = pinSettings;
            AppearanceSettings = appearanceSettings;
            SearchCommand = searchCommand;
            _dialogService = dialogService;
            _notificationService = notificationService;
            PrimaryViewPlayerManager = primaryViewPlayerManager;
            ObservableMediaPlayer = observableMediaPlayer;
            NiconicoLoginService = niconicoLoginService;
            LogoutFromNiconicoCommand = logoutFromNiconicoCommand;
            ApplicationLayoutManager = applicationLayoutManager;
            RestoreNavigationManager = restoreNavigationManager;
            VideoItemsSelectionContext = videoItemsSelectionContext;

            WeakReferenceMessenger.Default.Register<SettingsRestoredMessage>(this);

            SearchAutoSuggestItems = new ObservableCollection<SearchAutoSuggestItemViewModel>
            {
                new SearchAutoSuggestItemViewModel()
                {
                    Id = "VideoSearchSuggest",
                    SearchAction = (s) => PageManager.Search(SearchTarget.Keyword, s),
                },
                new SearchAutoSuggestItemViewModel()
                {
                    Id = "LiveSearchSuggest",
                    SearchAction = (s) => PageManager.Search(SearchTarget.Niconama, s),
                },
                new SearchAutoSuggestItemViewModel()
                {
                    Id = "DetailSearchSuggest",
                    SearchAction = (s) => PageManager.OpenPage(HohoemaPageType.Search, ""),
                },
            };

            _queueMenuItemViewModel = queueMenuItemViewModel;
            _userMylistManager = userMylistManager;
            _localMylistManager = localMylistManager;
            OpenLiveContentCommand = openLiveContentCommand;
            _pinsMenuSubItemViewModel = new PinsMenuSubItemViewModel("Pin".Translate(), PinSettings, _dialogService, _notificationService, loggerFactory.CreateLogger<PinsMenuSubItemViewModel>());
            _localMylistMenuSubItemViewModel = new LocalMylistSubMenuItemViewModel(_localMylistManager, PageManager.OpenPageCommand);

            // メニュー項目の初期化
            MenuItems_LoggedIn = new ObservableCollection<HohoemaListingPageItemBase>()
            {
                _pinsMenuSubItemViewModel,
                new SeparatorMenuItemViewModel(),
                _queueMenuItemViewModel,
                new NavigateAwareMenuItemViewModel(HohoemaPageType.RankingCategoryList.Translate(), HohoemaPageType.RankingCategoryList),
                new NavigateAwareMenuItemViewModel(HohoemaPageType.NicoRepo.Translate(), HohoemaPageType.NicoRepo, new NavigationParameters("type=Video")),
                new SubscriptionMenuItemViewModel(HohoemaPageType.SubscriptionManagement.Translate(), HohoemaPageType.SubscriptionManagement),
                //new NavigateAwareMenuItemViewModel("WatchAfterMylist".Translate(), HohoemaPageType.Mylist, new NavigationParameters(("id", MylistId.WatchAfterMylistId.ToString()))),
                new MylistSubMenuMenu(_userMylistManager, PageManager.OpenPageCommand),
                _localMylistMenuSubItemViewModel,
                new NavigateAwareMenuItemViewModel(HohoemaPageType.WatchHistory.Translate(), HohoemaPageType.WatchHistory),
                new NavigateAwareMenuItemViewModel(HohoemaPageType.CacheManagement.Translate(), HohoemaPageType.CacheManagement),
                
                new SeparatorMenuItemViewModel(),
                new LogginUserLiveSummaryItemViewModel(NiconicoSession, _logger, OpenLiveContentCommand),
                new NavigateAwareMenuItemViewModel(HohoemaPageType.NicoRepo.Translate(), HohoemaPageType.NicoRepo, new NavigationParameters("type=Program")),
                new NavigateAwareMenuItemViewModel(HohoemaPageType.Timeshift.Translate(), HohoemaPageType.Timeshift),
            };

            MenuItems_Offline = new ObservableCollection<HohoemaListingPageItemBase>()
            {
                _pinsMenuSubItemViewModel,
                new SeparatorMenuItemViewModel(),
                _queueMenuItemViewModel,
                new NavigateAwareMenuItemViewModel(HohoemaPageType.RankingCategoryList.Translate(), HohoemaPageType.RankingCategoryList),
                _localMylistMenuSubItemViewModel,
                new SubscriptionMenuItemViewModel(HohoemaPageType.SubscriptionManagement.Translate(), HohoemaPageType.SubscriptionManagement),
                new NavigateAwareMenuItemViewModel(HohoemaPageType.CacheManagement.Translate(), HohoemaPageType.CacheManagement),
            };


        }

        [RelayCommand]
        void OpenFollowPage()
        {
            //new NavigateAwareMenuItemViewModel(HohoemaPageType.FollowManage.Translate(), HohoemaPageType.FollowManage),
            PageManager.OpenPage(HohoemaPageType.FollowManage);
        }


        private RelayCommand _OpenAccountInfoCommand;
        public RelayCommand OpenAccountInfoCommand
        {
            get
            {
                return _OpenAccountInfoCommand
                    ?? (_OpenAccountInfoCommand = new RelayCommand(async () =>
                    {
                        await NiconicoSession.CheckSignedInStatus();

                        if (NiconicoSession.IsLoggedIn)
                        {
                            PageManager.OpenPageWithId(HohoemaPageType.UserInfo, NiconicoSession.UserId);
                        }
                    }));
            }
        }

        public RelayCommand OpenDebugPageCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    PageManager.OpenDebugPage();
                });
            }
        }

        #region Pins

        public void AddPin(HohoemaPin pin)
        {
            _pinsMenuSubItemViewModel.AddPin(pin);
        }    
        
        public void AddPinToFolder(HohoemaPin pin, PinFolderMenuItemViewModel folderVM)
        {
            _pinsMenuSubItemViewModel.AddPin(pin, folderVM);            
        }

        #endregion

        #region Search

        private RelayCommand<SearchAutoSuggestItemViewModel> _SuggestSelectedCommand;
        public RelayCommand<SearchAutoSuggestItemViewModel> SuggestSelectedCommand =>
            _SuggestSelectedCommand ?? (_SuggestSelectedCommand = new RelayCommand<SearchAutoSuggestItemViewModel>(ExecuteSuggestSelectedCommand));

        void ExecuteSuggestSelectedCommand(SearchAutoSuggestItemViewModel parameter)
        {

        }


        #endregion

        #region

        private RelayCommand _OpenDebugLogFileCommand;
        public RelayCommand OpenDebugLogFileCommand
        {
            get
            {
                return _OpenDebugLogFileCommand
                    ?? (_OpenDebugLogFileCommand = new RelayCommand(async () =>
                    {
                        var file = await ApplicationData.Current.TemporaryFolder.GetFileAsync("_log.txt");
                        await Launcher.LaunchFolderAsync(ApplicationData.Current.TemporaryFolder, new FolderLauncherOptions() { ItemsToSelect = { file } });
                    }));
            }
        }

        private RelayCommand _RequestApplicationRestartCommand;
        public RelayCommand RequestApplicationRestartCommand
        {
            get
            {
                return _RequestApplicationRestartCommand
                    ?? (_RequestApplicationRestartCommand = new RelayCommand(async () =>
                    {
                        var result = await Windows.ApplicationModel.Core.CoreApplication.RequestRestartAsync(string.Empty);
                    }));
            }
        }

        internal IEnumerable<PinFolderMenuItemViewModel> GetPinFolders()
        {
            return _pinsMenuSubItemViewModel.GetPinFolders();
        }

        internal PinFolderMenuItemViewModel GetParentPinFolder(PinMenuItemViewModel itemVM)
        {
            return _pinsMenuSubItemViewModel.GetParentPinFolder(itemVM);
        }

        #endregion
    }


    public sealed class LiteIssueDummyException : Exception
    {
        public LiteIssueDummyException()
        {
        }

        public LiteIssueDummyException(string message) : base(message)
        {
        }
    }

    public sealed class SearchAutoSuggestItemViewModel
    {
        public string Id { get; set; }
        public Action<string> SearchAction { get; set; }
    }

    public abstract class MenuSubItemViewModelBase : HohoemaListingPageItemBase
    {
        public ObservableCollection<MenuItemViewModel> Items { get; protected set; }
    }

    public partial class PinsMenuSubItemViewModel : MenuSubItemViewModelBase
    {
        private readonly PinSettings _pinSettings;
        private readonly DialogService _dialogService;
        private readonly NotificationService _notificationService;
        private readonly ILogger<PinsMenuSubItemViewModel> _logger;

        public PinsMenuSubItemViewModel(string label,  PinSettings pinSettings, DialogService dialogService, NotificationService notificationService, ILogger<PinsMenuSubItemViewModel> logger)
        {
            Label = label;

            IsSelected = false;
            _pinSettings = pinSettings;
            _dialogService = dialogService;
            _notificationService = notificationService;
            _logger = logger;
            Items = new ObservableCollection<MenuItemViewModel>();
            Reset();
        }

        internal IEnumerable<PinFolderMenuItemViewModel> GetPinFolders()
        {
            return Items.Where(x => x is PinFolderMenuItemViewModel).Select(x => x as PinFolderMenuItemViewModel);
        }

        internal PinFolderMenuItemViewModel GetParentPinFolder(PinMenuItemViewModel itemVM)
        {
            return Items.FirstOrDefault(x => (x as PinFolderMenuItemViewModel)?.Items.Contains(itemVM) ?? false) as PinFolderMenuItemViewModel;
        }

        public void Reset()
        {
            Items.Clear();
            foreach (var item in _pinSettings.ReadAllItems().OrderBy(x => x.SortIndex))
            {
                try
                {
                    MenuItemViewModel itemVM = item.PinType switch
                    {
                        BookmarkType.Item => new PinMenuItemViewModel(item, this),
                        BookmarkType.Folder => new PinFolderMenuItemViewModel(item, this),
                        _ => throw new NotSupportedException(),
                    };
                    Items.Add(itemVM);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Pin(Bookmark)の読み込みに失敗、問題回避のため削除 : {item.Label}");
                    _pinSettings.DeleteItem(item.Id);
                }
            }                       
        }


        internal void AddPin(HohoemaPin pin)
        {
            if (pin.PinType == BookmarkType.Item)
            {
                Items.Add(new PinMenuItemViewModel(pin, this));
            }
            else if (pin.PinType == BookmarkType.Folder)
            {
                Items.Add(new PinFolderMenuItemViewModel(pin, this));
            }
            else
            {
                throw new NotSupportedException();
            }
            
            SavePinsSortIndex();

            _notificationService.ShowLiteInAppNotification_Success("PinAddedWithTitle".Translate(pin.Label));
        }

        internal void AddPin(HohoemaPin pin, PinFolderMenuItemViewModel folderVM)
        {
            if (pin.PinType == BookmarkType.Item)
            {
                folderVM.AddItem(new PinMenuItemViewModel(pin, this));
            }
            else
            {
                throw new NotSupportedException();
            }

            SavePinsSortIndex();

            _notificationService.ShowLiteInAppNotification_Success("PinAddedWithTitle".Translate(pin.Label));
        }


        [RelayCommand]
        void DeletePin(MenuItemViewModel menuItem)
        {
            if (menuItem is PinMenuItemViewModel pinVM)
            {
                foreach (var item in Items)
                {
                    if (item is PinFolderMenuItemViewModel folderVM)
                    {
                        if (folderVM.RemoveItem(pinVM))
                        {
                            _pinSettings.UpdateItem(folderVM.Pin);
                        }
                    }
                }

                Items.Remove(pinVM);
                _pinSettings.DeleteItem(pinVM.Pin.Id);

                _notificationService.ShowLiteInAppNotification_Success("PinRemovedWithTitle".Translate(pinVM.Label));
            }
            else if (menuItem is PinFolderMenuItemViewModel folderVM)
            {
                Items.Remove(folderVM);
                _pinSettings.DeleteItem(folderVM.Pin.Id);

                _notificationService.ShowLiteInAppNotification_Success("PinRemovedWithTitle".Translate(folderVM.Label));
            }
        }


        [RelayCommand]
        async void OverridePin(IPinMenuItem item)
        {
            var pin = item.Pin;


            var name = pin.OverrideLabel ?? (item.Pin.PinType is BookmarkType.Item ? $"{pin.Label} ({pin.PageType.Translate()})" : $"{pin.Label}");
            var result = await _dialogService.GetTextAsync(
                $"RenameX".Translate(name),
                "PinRenameDialogPlacefolder_EmptyToDefault".Translate(),
                name,
                (s) => true
                );

            pin.OverrideLabel = result;
            _pinSettings.UpdateItem(pin);
        }

        ObservableCollection<MenuItemViewModel> GetParentItems(MenuItemViewModel itemVM)
        {
            var items = Items;
            if (itemVM is PinMenuItemViewModel pinVM)
            {
                var parentFolder = GetParentPinFolder(pinVM);
                if (parentFolder is not null)
                {
                    items = parentFolder.Items;
                }
            }
            return items;
        }


        [RelayCommand]
        void MovePinToTop(MenuItemViewModel item)
        {
            var items = GetParentItems(item);
            var index = items.IndexOf(item);
            if (index >= 1)
            {
                items.Remove(item);
                items.Insert(index - 1, item);
                SavePinsSortIndex();
            }
        }

        [RelayCommand]
        void MovePinToBottom(MenuItemViewModel item)
        {
            var items = GetParentItems(item);
            var index = items.IndexOf(item);
            if (index < items.Count - 1)
            {
                items.Remove(item);
                items.Insert(index + 1, item);
                SavePinsSortIndex();
            }
        }
        
        [RelayCommand]
        void MovePinToMostTop(MenuItemViewModel item)
        {
            var items = GetParentItems(item);
            var index = items.IndexOf(item);
            if (index >= 1)
            {
                items.Remove(item);
                items.Insert(0, item);
                SavePinsSortIndex();
            }
        }


        [RelayCommand]
        void MovePinToMostBottom(MenuItemViewModel item)
        {
            var items = GetParentItems(item);
            var index = items.IndexOf(item);
            if (index < items.Count - 1)
            {
                items.Remove(item);
                items.Insert(items.Count, item);   
                
                

                SavePinsSortIndex();
            }
        }

        private void SavePinsSortIndex()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                HohoemaPin pin = Items[i] switch
                {
                    PinMenuItemViewModel pinVM => pinVM.Pin,
                    PinFolderMenuItemViewModel folder => folder.Pin,
                    _ => throw new NotSupportedException(),
                };

                pin.SortIndex = i;
                if (Items[i] is PinFolderMenuItemViewModel folderVM)
                {                    
                    for (int m = 0; m < pin.SubItems.Count; m++)
                    {
                        (folderVM.Items[m] as IPinMenuItem).Pin.SortIndex = m;
                    }
                }

                _pinSettings.UpdateItem(pin);
            }
        }

        internal void PinMoveToFolder(PinMenuItemViewModel targetPinVM, PinFolderMenuItemViewModel destPinFolderVM)
        {
            // すでにあるフォルダから一回削除
            foreach (var item in Items)
            {
                if (item is PinFolderMenuItemViewModel folderVM)
                {
                    folderVM.RemoveItem(targetPinVM);
                    _pinSettings.UpdateItem(folderVM.Pin);                    
                }
            }

            bool isRootFolderItemRemoved = _pinSettings.DeleteItem(targetPinVM.Pin.Id);
            Items.Remove(targetPinVM);

            destPinFolderVM.AddItem(targetPinVM);            
            _pinSettings.UpdateItem(destPinFolderVM.Pin);

            SavePinsSortIndex();
        }

        internal void PinMoveToRootFolder(PinMenuItemViewModel targetPinVM)
        {
            foreach (var item in Items)
            {
                if (item is PinFolderMenuItemViewModel folderVM)
                {
                    folderVM.RemoveItem(targetPinVM);
                    _pinSettings.UpdateItem(folderVM.Pin);
                }
            }

            _pinSettings.UpdateItem(targetPinVM.Pin);
            Items.Add(targetPinVM);

            SavePinsSortIndex();
        }

        internal void SavePin(PinFolderMenuItemViewModel folderVM)
        {
            _pinSettings.UpdateItem(folderVM.Pin);
        }


        [RelayCommand]
        void PinCurrentPage()
        {

        }

    }

    public interface IPinMenuItem
    {
        HohoemaPin Pin { get; }
    }

    public partial class PinMenuItemViewModel : NavigateAwareMenuItemViewModel, IPinMenuItem
    {
        public HohoemaPin Pin { get; }
        private readonly PinsMenuSubItemViewModel _parentVM;

        public PinMenuItemViewModel(HohoemaPin pin, PinsMenuSubItemViewModel parentVM) 
            : base(pin.Label, pin.PageType, new NavigationParameters(pin.Parameter))
        {
            Pin = pin;
            _parentVM = parentVM;

            if (Pin.PageType == HohoemaPageType.Search)
            {
                var service = Parameter.GetValue<string>("service");
                if (Enum.TryParse<SearchTarget>(service, out var searchTarget))
                {
                    Description = $"{searchTarget.Translate()} {HohoemaPageType.Search.Translate()}";
                }
                else
                {
                    Description = HohoemaPageType.Search.Translate();
                }
            }
            else
            {
                Description = Pin.PageType.Translate();
            }
            
        }

        public ICommand DeletePinCommand => _parentVM.DeletePinCommand;
        public ICommand OverridePinCommand => _parentVM.OverridePinCommand;

        public ICommand MovePinToTopCommand => _parentVM.MovePinToTopCommand;
        public ICommand MovePinToBottomCommand => _parentVM.MovePinToBottomCommand;
        public ICommand MovePinToMostTopCommand => _parentVM.MovePinToMostTopCommand;
        public ICommand MovePinToMostBottomCommand => _parentVM.MovePinToMostBottomCommand;

        [RelayCommand]
        void MoveToFolder(PinFolderMenuItemViewModel pinFolderVM)
        {
            _parentVM.PinMoveToFolder(this, pinFolderVM);
        }

        [RelayCommand]
        void MoveToRootFolder()
        {
            _parentVM.PinMoveToRootFolder(this);
        }
    }

    public partial class PinFolderMenuItemViewModel : MenuItemViewModel, IPinMenuItem
    {
        public HohoemaPin Pin { get; }
        private readonly PinsMenuSubItemViewModel _parentVM;

        public ObservableCollection<MenuItemViewModel> Items { get; }

        public PinFolderMenuItemViewModel(HohoemaPin pin, PinsMenuSubItemViewModel parentVM)
            : base(pin.Label)
        {
            Pin = pin;
            _parentVM = parentVM;

            Items = new ObservableCollection<MenuItemViewModel>(pin.SubItems.OrderBy(x => x.SortIndex).Select(x => new PinMenuItemViewModel(x, parentVM)));
            _isOpen = Pin.IsOpenSubItems;
        }

        public ICommand DeletePinCommand => _parentVM.DeletePinCommand;
        public ICommand OverridePinCommand => _parentVM.OverridePinCommand;

        public ICommand MovePinToTopCommand => _parentVM.MovePinToTopCommand;
        public ICommand MovePinToBottomCommand => _parentVM.MovePinToBottomCommand;
        public ICommand MovePinToMostTopCommand => _parentVM.MovePinToMostTopCommand;
        public ICommand MovePinToMostBottomCommand => _parentVM.MovePinToMostBottomCommand;

        public ICommand PinCurrentPageCommand => _parentVM.PinCurrentPageCommand;

        public bool RemoveItem(PinMenuItemViewModel itemVM)
        {
            Items.Remove(itemVM);
            return Pin.SubItems.Remove(itemVM.Pin);
        }

        public void AddItem(PinMenuItemViewModel itemVM)
        {
            Items.Add(itemVM);
            Pin.SubItems.Add(itemVM.Pin);
        }

        [ObservableProperty]
        private bool _isOpen;


        partial void OnIsOpenChanged(bool value)
        {
            Pin.IsOpenSubItems = value;
            _parentVM.SavePin(this);
        }
    }

    public class MylistSubMenuMenu : MenuSubItemViewModelBase
    {
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly LoginUserOwnedMylistManager _userMylistManager;

        public MylistSubMenuMenu(LoginUserOwnedMylistManager userMylistManager, ICommand mylistPageOpenCommand)
        {
            Label = "Mylist".Translate();

            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _userMylistManager = userMylistManager;
            MylistPageOpenCommand = mylistPageOpenCommand;
            _userMylistManager.Mylists.CollectionChangedAsObservable()
                .Subscribe(e => 
                {
                    _dispatcherQueue.TryEnqueue(() => 
                    {
                        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                        {
                            var items = e.OldItems.Cast<LoginUserMylistPlaylist>();
                            foreach (var removedItem in items)
                            {
                                var removeMenuItem = Items.FirstOrDefault(x => (x as MylistMenuItemViewModel).Mylist.MylistId == removedItem.MylistId);
                                if (removeMenuItem != null)
                                {
                                    Items.Remove(removeMenuItem);
                                }
                            }
                        }
                        else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                        {
                            var items = e.NewItems.Cast<LoginUserMylistPlaylist>();
                            foreach (var item in items)
                            {
                                Items.Add(ToMenuItem(item));
                            }
                        }
                        else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
                        {
                            Items.Clear();
                        }
                    });
                });

            Items = new ObservableCollection<MenuItemViewModel>(_userMylistManager.Mylists.Select(ToMenuItem));
        }

        public ICommand MylistPageOpenCommand { get; }

        private MenuItemViewModel ToMenuItem(LoginUserMylistPlaylist mylist)
        {
            return new MylistMenuItemViewModel(mylist);
        }
    }

    public class LocalMylistSubMenuItemViewModel : MenuSubItemViewModelBase
    {
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly LocalMylistManager _localMylistManager;

        public LocalMylistSubMenuItemViewModel(LocalMylistManager localMylistManager, ICommand openLocalPlaylistManageCommand)
        {
            Label = "LocalPlaylist".Translate();

            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _localMylistManager = localMylistManager;
            OpenLocalPlaylistManageCommand = openLocalPlaylistManageCommand;
            _localMylistManager.LocalPlaylists.CollectionChangedAsObservable()
                .Subscribe(e => 
                {
                    _dispatcherQueue.TryEnqueue(() => 
                    {
                        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                        {
                            var items = e.OldItems.Cast<LocalPlaylist>();
                            foreach (var removedItem in items)
                            {
                                var removeMenuItem = Items.FirstOrDefault(x => (x as LocalMylistItemViewModel).LocalPlaylist.PlaylistId == removedItem.PlaylistId);
                                if (removeMenuItem != null)
                                {
                                    Items.Remove(removeMenuItem);
                                }
                            }
                        }
                        else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                        {
                            var items = e.NewItems.Cast<LocalPlaylist>();
                            foreach (var item in items)
                            {
                                Items.Add(new LocalMylistItemViewModel(item));
                            }
                        }
                        else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
                        {
                            Items.Clear();
                        }
                    });
                });

            Items = new ObservableCollection<MenuItemViewModel>(_localMylistManager.LocalPlaylists.Select(x => new LocalMylistItemViewModel(x)));
        }

        public ICommand OpenLocalPlaylistManageCommand { get; }
    }


    public class MylistMenuItemViewModel : NavigateAwareMenuItemViewModel
    {
        public MylistMenuItemViewModel(LoginUserMylistPlaylist mylist) 
            : base(mylist.Name, HohoemaPageType.Mylist, new NavigationParameters(("id", mylist.MylistId)))
        {
            Mylist = mylist;
        }

        public LoginUserMylistPlaylist Mylist { get; }
    }
    
    public class LocalMylistItemViewModel : NavigateAwareMenuItemViewModel
    {
        public LocalMylistItemViewModel(LocalPlaylist localPlaylist)
            : base(localPlaylist.Name, HohoemaPageType.LocalPlaylist, new NavigationParameters(("id", localPlaylist.PlaylistId.Id)))
        {
            LocalPlaylist = localPlaylist;
        }

        public LocalPlaylist LocalPlaylist { get; }
    }


    public sealed class LogginUserLiveSummaryItemViewModel : HohoemaListingPageItemBase
    {
        private readonly NiconicoSession _niconicoSession;
        private readonly ILogger _logger;
        private readonly DispatcherQueueTimer _timer;

        private long _NotifyCount;
        public long NotifyCount
        {
            get => _NotifyCount;
            set => SetProperty(ref _NotifyCount, value);
        }

        private bool _IsUnread;
        public bool IsUnread
        {
            get => _IsUnread;
            set => SetProperty(ref _IsUnread, value);
        }


        public ObservableCollection<LiveContentMenuItemViewModel> Items { get; }
        public OpenLiveContentCommand OpenLiveContentCommand { get; }

        public LogginUserLiveSummaryItemViewModel(NiconicoSession niconicoSession, ILogger logger, OpenLiveContentCommand openLiveContentCommand)
        {
            _niconicoSession = niconicoSession;
            _logger = logger;
            OpenLiveContentCommand = openLiveContentCommand;
            Items = new ObservableCollection<LiveContentMenuItemViewModel>();

            _timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            _timer.Interval = TimeSpan.FromMinutes(1);
            _timer.IsRepeating = true;

            _timer.Tick += Timer_Tick;

            if (_niconicoSession.IsLoggedIn)
            {
                _timer.Start();
            }
            else
            {
                _timer.Stop();
            }

            _niconicoSession.LogIn += _niconicoSession_LogIn;
            _niconicoSession.LogOut += _niconicoSession_LogOut;

            App.Current.Suspending += Current_Suspending;
            App.Current.Resuming += Current_Resuming;

            RefreshItems();
        }

        private void Current_Resuming(object sender, object e)
        {
            try
            {
                if (_niconicoSession.IsLoggedIn)
                {
                    _timer.Start();
                }
                else
                {
                    _timer.Stop();
                }
            }
            catch (Exception ex) { _logger.ZLogError(ex.ToString()); }            
        }

        private void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            try
            {
                _timer.Stop();
            }
            catch (Exception ex) { _logger.ZLogError(ex.ToString()); }
        }

        

        private void Timer_Tick(object sender, object e)
        {
            UpdateNotify();
        }

        private async void UpdateNotify()
        {
            try
            {
                var unread = await _niconicoSession.ToolkitContext.Live.LiveNotify.GetUnreadLiveNotifyAsync();
                IsUnread = unread.Data.IsUnread;
                NotifyCount = unread.Data.Count;
            }
            catch (Exception ex)
            {
                _logger.ZLogError(ex.ToString());
            }
        }

        private void _niconicoSession_LogOut(object sender, EventArgs e)
        {
            _timer.Stop();
        }

        private void _niconicoSession_LogIn(object sender, NiconicoSessionLoginEventArgs e)
        {
            _timer.Interval = e.IsPremium ? TimeSpan.FromMinutes(1) : TimeSpan.FromMinutes(3);
            _timer.Start();
            UpdateNotify();
        }

        DateTime _nextRefreshAvairableAt = DateTime.Now;

        public async void RefreshItems()
        {
            try
            {
                if (_nextRefreshAvairableAt > DateTime.Now)
                {
                    return;
                }

                _nextRefreshAvairableAt = DateTime.Now + TimeSpan.FromMinutes(1);

                var res = await _niconicoSession.ToolkitContext.Live.LiveNotify.GetLiveNotifyAsync();
                Items.Clear();
                foreach (var data in res.Data.NotifyboxContent)
                {
                    Items.Add(new LiveContentMenuItemViewModel(data));
                }

                IsUnread = false;
            }
            catch (Exception ex)
            {
                _logger.ZLogError(ex.ToString());
            }
        }
    }

    public sealed class LiveContentMenuItemViewModel : HohoemaListingPageItemBase, Models.Niconico.Live.ILiveContent
    {
        private readonly NotifyboxContent _content;

        public string Title => _content.Title;
        public string CommunityName => _content.CommunityName;
        public string ThumbnailUrl => _content.ThumbnailUrl.OriginalString;
        public LiveId LiveId => _content.Id;

        public string ProviderId => throw new NotImplementedException();
        public string ProviderName => CommunityName;
        public ProviderType ProviderType => _content.ProviderType;
        public NiconicoId Id => LiveId;

        public TimeSpan? _elapsedTime;
        public TimeSpan ElapsedTime => _elapsedTime ??= TimeSpan.FromSeconds(_content.ElapsedTime);

        public bool IsChannelContent => ProviderType is ProviderType.Channel;
        public bool IsOfficialContent => ProviderType is ProviderType.Official;

        public LiveContentMenuItemViewModel(NotifyboxContent content)
        {
            _content = content;
        }
    }


    public sealed class SubscriptionMenuItemViewModel : NavigateAwareMenuItemViewModel
    {
        public SubscriptionMenuItemViewModel(string label, HohoemaPageType pageType, INavigationParameters paramaeter = null) : base(label, pageType, paramaeter)
        {
        }
    }

}
