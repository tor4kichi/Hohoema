using I18NPortable;
using Mntone.Nico2.Live;
using Hohoema.Commands;

using Hohoema.Models.Domain;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Presentation.Services;
using Hohoema.Presentation.Services.Page;
using Hohoema.Presentation.Services.Player;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.Page.Commands;
using Hohoema.Models.UseCase.Pin;
using Hohoema.Models.UseCase.Playlist;
using Hohoema.Presentation.ViewModels.PrimaryWindowCoreLayout;
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
using Hohoema.Presentation.ViewModels.UserFeature;
using Hohoema.Models.Domain.Application;
using System.Diagnostics;
using System.Reactive.Linq;
using Uno.Extensions;

namespace Hohoema.Presentation.ViewModels
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
            SearchCommand searchCommand,
            DialogService dialogService,
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
            _dialogService = dialogService;
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
        }

        public IEventAggregator EventAggregator { get; }
        public NiconicoSession NiconicoSession { get; }
        public PageManager PageManager { get; }
        public PinSettings PinSettings { get; }
        public AppearanceSettings AppearanceSettings { get; }
        public SearchCommand SearchCommand { get; }
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
        private readonly DialogService _dialogService;


        public ObservableCollection<HohoemaPin> Pins { get; }


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
    }


    

    
}
