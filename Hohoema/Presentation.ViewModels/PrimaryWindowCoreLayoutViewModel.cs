using I18NPortable;
using Hohoema.Models.Domain;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Presentation.Services;
using Hohoema.Presentation.Services.Page;
using Hohoema.Presentation.Services.Player;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.NicoVideos;
using Hohoema.Presentation.ViewModels.PrimaryWindowCoreLayout;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Hohoema.Presentation.ViewModels.Pages.UserFeaturePages;
using Hohoema.Models.Domain.Application;
using System.Diagnostics;
using System.Reactive.Linq;
using Uno.Extensions;
using Hohoema.Presentation.ViewModels.Navigation.Commands;
using Hohoema.Models.UseCase.NicoVideos.Player;
using Prism.Navigation;
using Hohoema.Models.Domain.Niconico.UserFeature.Mylist;
using System.Windows.Input;
using Hohoema.Models.Domain.Playlist;
using Windows.UI.Xaml;
using NiconicoLiveToolkit.Live.Notify;
using Hohoema.Presentation.ViewModels.LivePages.Commands;
using NiconicoLiveToolkit.Live;
using Windows.UI.Popups;
using Hohoema.Presentation.Views.Dialogs;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.AppCenter.Crashes;
using Windows.System;

namespace Hohoema.Presentation.ViewModels
{  
    public sealed class PrimaryWindowCoreLayoutViewModel : BindableBase
    {

        public IEventAggregator EventAggregator { get; }
        public NiconicoSession NiconicoSession { get; }
        public PageManager PageManager { get; }
        public PinSettings PinSettings { get; }
        public AppearanceSettings AppearanceSettings { get; }
        public SearchCommand SearchCommand { get; }
        public PrimaryViewPlayerManager PrimaryViewPlayerManager { get; }
        public ObservableMediaPlayer ObservableMediaPlayer { get; }
        public NiconicoLoginService NiconicoLoginService { get; }
        public LogoutFromNiconicoCommand LogoutFromNiconicoCommand { get; }
        public WindowService WindowService { get; }
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public RestoreNavigationManager RestoreNavigationManager { get; }
        public VideoItemsSelectionContext VideoItemsSelectionContext { get; }
        private readonly UserMylistManager _userMylistManager;
        private readonly LocalMylistManager _localMylistManager;
        public OpenLiveContentCommand OpenLiveContentCommand { get; }

        private readonly ErrorTrackingManager _errorTrackingManager;
        private readonly DialogService _dialogService;


        public ObservableCollection<SearchAutoSuggestItemViewModel> SearchAutoSuggestItems { get; set; }

        public ObservableCollection<HohoemaListingPageItemBase> MenuItems_Offline { get; private set; }
        public ObservableCollection<HohoemaListingPageItemBase> MenuItems_LoggedIn { get; private set; }

        internal readonly QueueMenuItemViewModel _queueMenuItemViewModel;
        internal readonly PinsMenuSubItemViewModel _pinsMenuSubItemViewModel;

        public LocalMylistSubMenuItemViewModel _localMylistMenuSubItemViewModel { get; }

        public PrimaryWindowCoreLayoutViewModel(
            ErrorTrackingManager errorTrackingManager,
            IEventAggregator eventAggregator,
            NiconicoSession niconicoSession,
            PageManager pageManager,
            PinSettings pinSettings,
            AppearanceSettings appearanceSettings,
            SearchCommand searchCommand,
            DialogService dialogService,
            PrimaryViewPlayerManager primaryViewPlayerManager,
            ObservableMediaPlayer observableMediaPlayer,
            NiconicoLoginService niconicoLoginService,
            LogoutFromNiconicoCommand logoutFromNiconicoCommand,
            WindowService windowService,
            ApplicationLayoutManager applicationLayoutManager,
            RestoreNavigationManager restoreNavigationManager,
            VideoItemsSelectionContext videoItemsSelectionContext,
            QueueMenuItemViewModel queueMenuItemViewModel,
            UserMylistManager userMylistManager,
            LocalMylistManager localMylistManager,
            OpenLiveContentCommand openLiveContentCommand
            )
        {
            _errorTrackingManager = errorTrackingManager;
            EventAggregator = eventAggregator;
            NiconicoSession = niconicoSession;
            PageManager = pageManager;
            PinSettings = pinSettings;
            AppearanceSettings = appearanceSettings;
            SearchCommand = searchCommand;
            _dialogService = dialogService;
            PrimaryViewPlayerManager = primaryViewPlayerManager;
            ObservableMediaPlayer = observableMediaPlayer;
            NiconicoLoginService = niconicoLoginService;
            LogoutFromNiconicoCommand = logoutFromNiconicoCommand;
            WindowService = windowService;
            ApplicationLayoutManager = applicationLayoutManager;
            RestoreNavigationManager = restoreNavigationManager;
            VideoItemsSelectionContext = videoItemsSelectionContext;
            
            SettingsRestoredTempraryFlags.Instance.ObserveProperty(x => x.IsPinSettingsRestored)
                .Where(x => x)
                .Subscribe(_ => 
                {
                    _pinsMenuSubItemViewModel.Reset();

                    Task.Run(() => { SettingsRestoredTempraryFlags.Instance.WhenPinsRestored(); });                    
                });

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
            _pinsMenuSubItemViewModel = new PinsMenuSubItemViewModel("Pin".Translate(), PinSettings, _dialogService);
            _localMylistMenuSubItemViewModel = new LocalMylistSubMenuItemViewModel(_localMylistManager, PageManager.OpenPageCommand);

            // メニュー項目の初期化
            MenuItems_LoggedIn = new ObservableCollection<HohoemaListingPageItemBase>()
            {
                _pinsMenuSubItemViewModel,
                _queueMenuItemViewModel,
                new LogginUserLiveSummaryItemViewModel(NiconicoSession, OpenLiveContentCommand),
                new SeparatorMenuItemViewModel(),
                new MenuItemViewModel(HohoemaPageType.RankingCategoryList.Translate(), HohoemaPageType.RankingCategoryList),
                new MenuItemViewModel(HohoemaPageType.NicoRepo.Translate(), HohoemaPageType.NicoRepo),
                new MenuItemViewModel(HohoemaPageType.WatchHistory.Translate(), HohoemaPageType.WatchHistory),
                new MenuItemViewModel("WatchAfterMylist".Translate(), HohoemaPageType.Mylist, new NavigationParameters("id=0")),
                new MylistSubMenuMenu(_userMylistManager, PageManager.OpenPageCommand),
                _localMylistMenuSubItemViewModel,
                new MenuItemViewModel(HohoemaPageType.FollowManage.Translate(), HohoemaPageType.FollowManage),
                new MenuItemViewModel(HohoemaPageType.Timeshift.Translate(), HohoemaPageType.Timeshift),
                new MenuItemViewModel(HohoemaPageType.SubscriptionManagement.Translate(), HohoemaPageType.SubscriptionManagement),
                new MenuItemViewModel(HohoemaPageType.CacheManagement.Translate(), HohoemaPageType.CacheManagement),
            };

            MenuItems_Offline = new ObservableCollection<HohoemaListingPageItemBase>()
            {
                _pinsMenuSubItemViewModel,
                _queueMenuItemViewModel,
                new SeparatorMenuItemViewModel(),
                new MenuItemViewModel(HohoemaPageType.RankingCategoryList.Translate(), HohoemaPageType.RankingCategoryList),
                _localMylistMenuSubItemViewModel,
                new MenuItemViewModel(HohoemaPageType.SubscriptionManagement.Translate(), HohoemaPageType.SubscriptionManagement),
                new MenuItemViewModel(HohoemaPageType.CacheManagement.Translate(), HohoemaPageType.CacheManagement),
            };


        }


        private DelegateCommand _OpenAccountInfoCommand;
        public DelegateCommand OpenAccountInfoCommand
        {
            get
            {
                return _OpenAccountInfoCommand
                    ?? (_OpenAccountInfoCommand = new DelegateCommand(async () =>
                    {
                        await NiconicoSession.CheckSignedInStatus();

                        if (NiconicoSession.IsLoggedIn)
                        {
                            PageManager.OpenPageWithId(HohoemaPageType.UserInfo, NiconicoSession.UserIdString);
                        }
                    }));
            }
        }

        public DelegateCommand OpenDebugPageCommand
        {
            get
            {
                return new DelegateCommand(() =>
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

        #endregion

        #region Search

        private DelegateCommand<SearchAutoSuggestItemViewModel> _SuggestSelectedCommand;
        public DelegateCommand<SearchAutoSuggestItemViewModel> SuggestSelectedCommand =>
            _SuggestSelectedCommand ?? (_SuggestSelectedCommand = new DelegateCommand<SearchAutoSuggestItemViewModel>(ExecuteSuggestSelectedCommand));

        void ExecuteSuggestSelectedCommand(SearchAutoSuggestItemViewModel parameter)
        {

        }

        #endregion

        #region

        

        private DelegateCommand _RequestApplicationRestartCommand;
        public DelegateCommand RequestApplicationRestartCommand
        {
            get
            {
                return _RequestApplicationRestartCommand
                    ?? (_RequestApplicationRestartCommand = new DelegateCommand(async () =>
                    {
                        var result = await Windows.ApplicationModel.Core.CoreApplication.RequestRestartAsync(string.Empty);
                    }));
            }
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

    public class PinsMenuSubItemViewModel : MenuSubItemViewModelBase
    {
        private readonly PinSettings _pinSettings;
        private readonly DialogService _dialogService;

        public PinsMenuSubItemViewModel(string label, PinSettings pinSettings, DialogService dialogService)
        {
            Label = label;

            IsSelected = false;
            _pinSettings = pinSettings;
            _dialogService = dialogService;
            Items = new ObservableCollection<MenuItemViewModel>();
            Reset();
        }

        public void Reset()
        {
            foreach (var item in _pinSettings.ReadAllItems().OrderBy(x => x.SortIndex).Select(x => new PinMenuItemViewModel(x, this)))
            {
                Items.DisposableAdd(item);
            }
        }


        internal void AddPin(HohoemaPin pin)
        {
            Items.Add(new PinMenuItemViewModel(pin, this));
            SavePinsSortIndex();
        }



        private DelegateCommand<PinMenuItemViewModel> _DeletePinCommand;
        public DelegateCommand<PinMenuItemViewModel> DeletePinCommand =>
            _DeletePinCommand ?? (_DeletePinCommand = new DelegateCommand<PinMenuItemViewModel>(ExecuteDeletePinCommand));

        void ExecuteDeletePinCommand(PinMenuItemViewModel item)
        {
            var currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{currentMethod.DeclaringType.Name}#{currentMethod.Name}");

            Items.Remove(item);
            _pinSettings.DeleteItem(item.Pin.Id);
        }




        private DelegateCommand<PinMenuItemViewModel> _OverridePinCommand;
        public DelegateCommand<PinMenuItemViewModel> OverridePinCommand =>
            _OverridePinCommand ?? (_OverridePinCommand = new DelegateCommand<PinMenuItemViewModel>(ExecuteOverridePinCommand));

        async void ExecuteOverridePinCommand(PinMenuItemViewModel item)
        {
            var currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{currentMethod.DeclaringType.Name}#{currentMethod.Name}");

            var pin = item.Pin;

            var name = pin.OverrideLabel ?? $"{pin.Label} ({pin.PageType.Translate()})";
            var result = await _dialogService.GetTextAsync(
                $"RenameX".Translate(name),
                "PinRenameDialogPlacefolder_EmptyToDefault".Translate(),
                name,
                (s) => true
                );

            pin.OverrideLabel = result;
            _pinSettings.UpdateItem(pin);
        }


        private DelegateCommand<PinMenuItemViewModel> _MovePinToTopCommand;
        public DelegateCommand<PinMenuItemViewModel> MovePinToTopCommand =>
            _MovePinToTopCommand ?? (_MovePinToTopCommand = new DelegateCommand<PinMenuItemViewModel>(ExecuteMovePinToTopCommand));

        void ExecuteMovePinToTopCommand(PinMenuItemViewModel item)
        {
            var index = Items.IndexOf(item);
            if (index >= 1)
            {
                Items.Remove(item);
                Items.Insert(index - 1, item);
                SavePinsSortIndex();
            }
        }

        private DelegateCommand<PinMenuItemViewModel> _MovePinToBottomCommand;
        public DelegateCommand<PinMenuItemViewModel> MovePinToBottomCommand =>
            _MovePinToBottomCommand ?? (_MovePinToBottomCommand = new DelegateCommand<PinMenuItemViewModel>(ExecuteMovePinToBottomCommand));

        void ExecuteMovePinToBottomCommand(PinMenuItemViewModel item)
        {
            var index = Items.IndexOf(item);
            if (index < Items.Count - 1)
            {
                Items.Remove(item);
                Items.Insert(index + 1, item);
                SavePinsSortIndex();
            }
        }

        private DelegateCommand<PinMenuItemViewModel> _MovePinToMostTopCommand;
        public DelegateCommand<PinMenuItemViewModel> MovePinToMostTopCommand =>
            _MovePinToMostTopCommand ?? (_MovePinToMostTopCommand = new DelegateCommand<PinMenuItemViewModel>(ExecuteMovePinToMostTopCommand));

        void ExecuteMovePinToMostTopCommand(PinMenuItemViewModel item)
        {
            var index = Items.IndexOf(item);
            if (index >= 1)
            {
                Items.Remove(item);
                Items.Insert(0, item);
                SavePinsSortIndex();
            }
        }

        private DelegateCommand<PinMenuItemViewModel> _MovePinToMostBottomCommand;
        public DelegateCommand<PinMenuItemViewModel> MovePinToMostBottomCommand =>
            _MovePinToMostBottomCommand ?? (_MovePinToMostBottomCommand = new DelegateCommand<PinMenuItemViewModel>(ExecuteMovePinToMostBottomCommand));

        void ExecuteMovePinToMostBottomCommand(PinMenuItemViewModel item)
        {
            var index = Items.IndexOf(item);
            if (index < Items.Count - 1)
            {
                Items.Remove(item);
                Items.Insert(Items.Count, item);
                SavePinsSortIndex();
            }
        }

        private void SavePinsSortIndex()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                var pin = Items[i] as PinMenuItemViewModel;
                pin.Pin.SortIndex = i;
                _pinSettings.UpdateItem(pin.Pin);
            }
        }


    }


    public class PinMenuItemViewModel : MenuItemViewModel
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


    }

    public class MylistSubMenuMenu : MenuSubItemViewModelBase
    {
        private readonly UserMylistManager _userMylistManager;

        public MylistSubMenuMenu(UserMylistManager userMylistManager, ICommand mylistPageOpenCommand)
        {
            Label = "Mylist".Translate();

            _userMylistManager = userMylistManager;
            MylistPageOpenCommand = mylistPageOpenCommand;
            _userMylistManager.Mylists.CollectionChangedAsObservable()
                .Subscribe(e => 
                {
                    if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                    {
                        var items = e.OldItems.Cast<LoginUserMylistPlaylist>();
                        foreach (var removedItem in items)
                        {
                            var removeMenuItem = Items.FirstOrDefault(x => (x as MylistMenuItemViewModel).Mylist.Id == removedItem.Id);
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
        private readonly LocalMylistManager _localMylistManager;

        public LocalMylistSubMenuItemViewModel(LocalMylistManager localMylistManager, ICommand openLocalPlaylistManageCommand)
        {
            Label = "LocalPlaylist".Translate();

            _localMylistManager = localMylistManager;
            OpenLocalPlaylistManageCommand = openLocalPlaylistManageCommand;
            _localMylistManager.LocalPlaylists.CollectionChangedAsObservable()
                .Subscribe(e => 
                {
                    if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                    {
                        var items = e.OldItems.Cast<LocalPlaylist>();
                        foreach (var removedItem in items)
                        {
                            var removeMenuItem = Items.FirstOrDefault(x => (x as LocalMylistItemViewModel).LocalPlaylist.Id == removedItem.Id);
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
                });

            Items = new ObservableCollection<MenuItemViewModel>(_localMylistManager.LocalPlaylists.Select(x => new LocalMylistItemViewModel(x)));
        }

        public ICommand OpenLocalPlaylistManageCommand { get; }
    }


    public class MylistMenuItemViewModel : MenuItemViewModel
    {
        public MylistMenuItemViewModel(LoginUserMylistPlaylist mylist) 
            : base(mylist.Label, HohoemaPageType.Mylist, new NavigationParameters(("id", mylist.Id)))
        {
            Mylist = mylist;
        }

        public LoginUserMylistPlaylist Mylist { get; }
    }


    public class LocalMylistItemViewModel : MenuItemViewModel
    {
        public LocalMylistItemViewModel(LocalPlaylist localPlaylist)
            : base(localPlaylist.Label, HohoemaPageType.LocalPlaylist, new NavigationParameters(("id", localPlaylist.Id)))
        {
            LocalPlaylist = localPlaylist;
        }

        public LocalPlaylist LocalPlaylist { get; }
    }


    public sealed class LogginUserLiveSummaryItemViewModel : HohoemaListingPageItemBase
    {
        private readonly NiconicoSession _niconicoSession;
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

        public LogginUserLiveSummaryItemViewModel(NiconicoSession niconicoSession, OpenLiveContentCommand openLiveContentCommand)
        {
            _niconicoSession = niconicoSession;
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
        }

        private void Timer_Tick(object sender, object e)
        {
            UpdateNotify();
        }

        private async void UpdateNotify()
        {
            var unread = await _niconicoSession.LiveContext.Live.LiveNotify.GetUnreadLiveNotifyAsync();
            IsUnread = unread.Data.IsUnread;
            NotifyCount = unread.Data.Count;
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
            if (_nextRefreshAvairableAt > DateTime.Now)
            {
                return;
            }

            _nextRefreshAvairableAt = DateTime.Now + TimeSpan.FromMinutes(1);

            var res = await _niconicoSession.LiveContext.Live.LiveNotify.GetLiveNotifyAsync();
            Items.Clear();
            foreach (var data in res.Data.NotifyboxContent)
            {
                Items.Add(new LiveContentMenuItemViewModel(data));
            }

            IsUnread = false;
        }
    }

    public sealed class LiveContentMenuItemViewModel : HohoemaListingPageItemBase, Models.Domain.Niconico.Live.ILiveContent
    {
        private readonly NotifyboxContent _content;

        public string Title => _content.Title;
        public string CommunityName => _content.CommunityName;
        public string ThumbnailUrl => _content.ThumbnailUrl.OriginalString;
        private string _liveId;
        public string LiveId => _liveId ??= "lv" + _content.Id;

        public string ProviderId => throw new NotImplementedException();
        public string ProviderName => CommunityName;
        public ProviderType ProviderType => _content.ProviderType;
        public string Id => LiveId;

        public TimeSpan? _elapsedTime;
        public TimeSpan ElapsedTime => _elapsedTime ??= TimeSpan.FromSeconds(_content.ElapsedTime);

        public bool IsChannelContent => ProviderType is ProviderType.Channel;
        public bool IsOfficialContent => ProviderType is ProviderType.Official;

        public LiveContentMenuItemViewModel(NotifyboxContent content)
        {
            _content = content;
        }
    }

}
