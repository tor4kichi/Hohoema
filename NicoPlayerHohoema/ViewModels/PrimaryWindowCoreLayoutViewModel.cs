using I18NPortable;
using Mntone.Nico2.Live;
using NicoPlayerHohoema.Commands;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.RestoreNavigation;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.Services.Page;
using NicoPlayerHohoema.Services.Player;
using NicoPlayerHohoema.UseCase;
using NicoPlayerHohoema.UseCase.Page.Commands;
using NicoPlayerHohoema.UseCase.Pin;
using NicoPlayerHohoema.UseCase.Playlist;
using NicoPlayerHohoema.ViewModels.PrimaryWindowCoreLayout;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels
{


    public enum NiconicoServiceType
    {
        Video,
        Live
    }

    

    

    

  
    public sealed class PrimaryWindowCoreLayoutViewModel : BindableBase
    {
        public PrimaryWindowCoreLayoutViewModel(
            IEventAggregator eventAggregator,
            NiconicoSession niconicoSession,
            PageManager pageManager,
            PinSettings pinSettings,
            AppearanceSettings appearanceSettings,
            UseCase.Page.Commands.SearchCommand searchCommand,
            PinRemoveCommand pinRemoveCommand,
            PinChangeOverrideLabelCommand pinChangeOverrideLabelCommand,
            VideoMenuSubPageContent videoMenuSubPageContent,
            LiveMenuSubPageContent liveMenuSubPageContent,
            PrimaryViewPlayerManager primaryViewPlayerManager,
            ObservableMediaPlayer observableMediaPlayer,
            NiconicoLoginService niconicoLoginService,
            LogoutFromNiconicoCommand logoutFromNiconicoCommand,
            VideoItemsSelectionContext videoItemsSelectionContext,
            WindowService windowService,
            ApplicationLayoutManager applicationLayoutManager,
            RestoreNavigationManager restoreNavigationManager
            )
        {
            EventAggregator = eventAggregator;
            NiconicoSession = niconicoSession;
            PageManager = pageManager;
            PinSettings = pinSettings;
            AppearanceSettings = appearanceSettings;
            SearchCommand = searchCommand;
            PinRemoveCommand = pinRemoveCommand;
            PinChangeOverrideLabelCommand = pinChangeOverrideLabelCommand;
            VideoMenu = videoMenuSubPageContent;
            LiveMenu = liveMenuSubPageContent;
            PrimaryViewPlayerManager = primaryViewPlayerManager;
            ObservableMediaPlayer = observableMediaPlayer;
            NiconicoLoginService = niconicoLoginService;
            LogoutFromNiconicoCommand = logoutFromNiconicoCommand;
            VideoItemsSelectionContext = videoItemsSelectionContext;
            WindowService = windowService;
            ApplicationLayoutManager = applicationLayoutManager;
            RestoreNavigationManager = restoreNavigationManager;
        }

        public IEventAggregator EventAggregator { get; }
        public NiconicoSession NiconicoSession { get; }
        public PageManager PageManager { get; }
        public PinSettings PinSettings { get; }
        public AppearanceSettings AppearanceSettings { get; }
        public SearchCommand SearchCommand { get; }
        public PinRemoveCommand PinRemoveCommand { get; }
        public PinChangeOverrideLabelCommand PinChangeOverrideLabelCommand { get; }
        public VideoMenuSubPageContent VideoMenu { get; set; }
        public LiveMenuSubPageContent LiveMenu { get; set; }
        public PrimaryViewPlayerManager PrimaryViewPlayerManager { get; }
        public ObservableMediaPlayer ObservableMediaPlayer { get; }
        public NiconicoLoginService NiconicoLoginService { get; }
        public LogoutFromNiconicoCommand LogoutFromNiconicoCommand { get; }
        public VideoItemsSelectionContext VideoItemsSelectionContext { get; }
        public WindowService WindowService { get; }
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public RestoreNavigationManager RestoreNavigationManager { get; }


        // call from PrimaryWindowsCoreLayout.xaml.cs
        public void AddPin(HohoemaPin pin)
        {
            if (pin != null)
            {
                PinSettings.Pins.Add(pin);
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


    }


    

    
}
