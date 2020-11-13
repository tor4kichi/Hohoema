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

        private readonly DialogService _dialogService;


        public ObservableCollection<HohoemaPin> Pins { get; }

        public ObservableCollection<SearchAutoSuggestItemViewModel> SearchAutoSuggestItems { get; set; }

        public ObservableCollection<HohoemaListingPageItemBase> MenuItems_Offline { get; private set; }
        public ObservableCollection<HohoemaListingPageItemBase> MenuItems_LoggedIn { get; private set; }

        WatchAfterMenuItemViewModel _watchAfterMenuItemViewModel;

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
            WatchAfterMenuItemViewModel watchAfterMenuItemViewModel
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
            Pins = new ObservableCollection<HohoemaPin>(PinSettings.ReadAllItems());
            Pins.CollectionChangedAsObservable()
                .Throttle(TimeSpan.FromSeconds(0.1))
                .Subscribe(_ => 
                {
                    for (int i = 0; i < Pins.Count; i++)
                    {
                        Pins[i].SortIndex = i;
                        PinSettings.UpdateItem(Pins[i]);
                    }

                    Debug.WriteLine("PinSettings Saved");
                });

            SettingsRestoredTempraryFlags.Instance.ObserveProperty(x => x.IsPinSettingsRestored)
                .Where(x => x)
                .Subscribe(_ => 
                {
                    Pins.Clear();
                    Pins.AddRange(PinSettings.ReadAllItems());

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

            var pinsMenuItem = new PinsMenuSubItemViewModel("Pins".Translate(), PinSettings);

            // メニュー項目の初期化
            MenuItems_LoggedIn = new ObservableCollection<HohoemaListingPageItemBase>()
            {
                pinsMenuItem,
                new MenuItemViewModel(HohoemaPageType.RankingCategoryList.Translate(), HohoemaPageType.RankingCategoryList),
                new MenuItemViewModel(HohoemaPageType.NicoRepo.Translate(), HohoemaPageType.NicoRepo),
                new MenuItemViewModel(HohoemaPageType.FollowManage.Translate(), HohoemaPageType.FollowManage),
                new MenuItemViewModel(HohoemaPageType.WatchHistory.Translate(), HohoemaPageType.WatchHistory),
                new SeparatorMenuItemViewModel(),
                _watchAfterMenuItemViewModel,
                new MenuItemViewModel(HohoemaPageType.SubscriptionManagement.Translate(), HohoemaPageType.SubscriptionManagement),
                new MenuItemViewModel(HohoemaPageType.CacheManagement.Translate(), HohoemaPageType.CacheManagement),
            };

            MenuItems_Offline = new ObservableCollection<HohoemaListingPageItemBase>()
            {
                pinsMenuItem,
                new MenuItemViewModel(HohoemaPageType.RankingCategoryList.Translate(), HohoemaPageType.RankingCategoryList),
                new SeparatorMenuItemViewModel(),
                _watchAfterMenuItemViewModel,
                new MenuItemViewModel(HohoemaPageType.SubscriptionManagement.Translate(), HohoemaPageType.SubscriptionManagement),
                new MenuItemViewModel(HohoemaPageType.CacheManagement.Translate(), HohoemaPageType.CacheManagement),
            };
        }



        // call from PrimaryWindowsCoreLayout.xaml.cs
        public void AddPin(HohoemaPin pin)
        {
            var currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{currentMethod.DeclaringType.Name}#{currentMethod.Name}");

            if (pin != null)
            {
                PinSettings.CreateItem(pin);
                Pins.Add(pin);
            }
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



        private DelegateCommand<HohoemaPin> _RemovePinCommand;
        public DelegateCommand<HohoemaPin> RemovePinCommand =>
            _RemovePinCommand ?? (_RemovePinCommand = new DelegateCommand<HohoemaPin>(ExecuteRemovePinCommand));

        void ExecuteRemovePinCommand(HohoemaPin pin)
        {
            var currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{currentMethod.DeclaringType.Name}#{currentMethod.Name}");

            PinSettings.DeleteItem(pin.Id);
            Pins.Remove(pin);
        }



        private DelegateCommand<HohoemaPin> _ChangePinOverrideLabelCommand;
        public DelegateCommand<HohoemaPin> ChangePinOverrideLabelCommand =>
            _ChangePinOverrideLabelCommand ?? (_ChangePinOverrideLabelCommand = new DelegateCommand<HohoemaPin>(ExecuteChangePinOverrideLabelCommand));

        async void ExecuteChangePinOverrideLabelCommand(HohoemaPin pin)
        {
            var currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{currentMethod.DeclaringType.Name}#{currentMethod.Name}");

            var name = pin.OverrideLabel ?? $"{pin.Label} ({pin.PageType.Translate()})";
            var result = await _dialogService.GetTextAsync(
                $"RenameX".Translate(name),
                "PinRenameDialogPlacefolder_EmptyToDefault".Translate(),
                name,
                (s) => true
                );

            pin.OverrideLabel = string.IsNullOrEmpty(result) ? null : result;
            PinSettings.UpdateItem(pin);
        }




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

        public PinsMenuSubItemViewModel(string label, PinSettings pinSettings)
        {
            Label = label;

            IsSelected = false;
            _pinSettings = pinSettings;

            Items = new ObservableCollection<MenuItemViewModel>(_pinSettings.ReadAllItems().Select(x => new PinMenuItemViewModel(x)));
        }
    }


    public class PinMenuItemViewModel : MenuItemViewModel
    {
        private readonly HohoemaPin _pin;

        public PinMenuItemViewModel(HohoemaPin pin) 
            : base(pin.OverrideLabel ?? pin.Label, pin.PageType, new NavigationParameters(pin.Parameter))
        {
            _pin = pin;
        }
    }
}
