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
        private readonly DialogService _dialogService;


        public ObservableCollection<SearchAutoSuggestItemViewModel> SearchAutoSuggestItems { get; set; }

        public ObservableCollection<HohoemaListingPageItemBase> MenuItems_Offline { get; private set; }
        public ObservableCollection<HohoemaListingPageItemBase> MenuItems_LoggedIn { get; private set; }

        private readonly WatchAfterMenuItemViewModel _watchAfterMenuItemViewModel;
        private readonly PinsMenuSubItemViewModel _pinsMenuSubItemViewModel;

        public PrimaryWindowCoreLayoutViewModel(
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
            WatchAfterMenuItemViewModel watchAfterMenuItemViewModel,
            UserMylistManager userMylistManager
            )
        {
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

            _watchAfterMenuItemViewModel = watchAfterMenuItemViewModel;
            _userMylistManager = userMylistManager;
            _pinsMenuSubItemViewModel = new PinsMenuSubItemViewModel("Pin".Translate(), PinSettings, _dialogService);

            // メニュー項目の初期化
            MenuItems_LoggedIn = new ObservableCollection<HohoemaListingPageItemBase>()
            {
                _pinsMenuSubItemViewModel,
                _watchAfterMenuItemViewModel,
                new SeparatorMenuItemViewModel(),
                new MenuItemViewModel(HohoemaPageType.RankingCategoryList.Translate(), HohoemaPageType.RankingCategoryList),
                new MenuItemViewModel(HohoemaPageType.NicoRepo.Translate(), HohoemaPageType.NicoRepo),
                new MenuItemViewModel(HohoemaPageType.WatchHistory.Translate(), HohoemaPageType.WatchHistory),
                new MylistSubMenuMenu(_userMylistManager),
            };

            MenuItems_Offline = new ObservableCollection<HohoemaListingPageItemBase>()
            {
                _pinsMenuSubItemViewModel,
                _watchAfterMenuItemViewModel,
                new SeparatorMenuItemViewModel(),
                new MenuItemViewModel(HohoemaPageType.RankingCategoryList.Translate(), HohoemaPageType.RankingCategoryList),
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
            _pinSettings.DeleteItem(item.Pin);
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

        public MylistSubMenuMenu(UserMylistManager userMylistManager)
        {
            Label = "Mylist".Translate();

            _userMylistManager = userMylistManager;

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

        private MenuItemViewModel ToMenuItem(LoginUserMylistPlaylist mylist)
        {
            return new MylistMenuItemViewModel(mylist);
        }
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

}
