using I18NPortable;
using Hohoema.Commands;
using Hohoema.Interfaces;
using Hohoema.Models;
using Hohoema.Services;
using Hohoema.Services.Player;
using Hohoema.UseCase;
using Hohoema.UseCase.Page.Commands;
using Hohoema.UseCase.Pin;
using Hohoema.UseCase.Playlist;
using Hohoema.ViewModels.PrimaryWindowCoreLayout;
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
using Hohoema.Models.Niconico;
using Hohoema.ViewModels.Pages;
using Hohoema.Models.Repository.App;
using Hohoema.Models.Pages;
using Hohoema.ViewModels.Pages.Commands;
using Uno.Extensions;
using Hohoema.UseCase.Services;
using Prism.Ioc;

namespace Hohoema.ViewModels
{  
    public sealed class PrimaryWindowCoreLayoutViewModel : BindableBase
    {
        public PrimaryWindowCoreLayoutViewModel(
            IEventAggregator eventAggregator,
            NiconicoSession niconicoSession,
            PageManager pageManager,
            PinRepository pinSettings,
            AppearanceSettingsRepository appearanceSettings,
            UseCase.Page.Commands.SearchCommand searchCommand,
            PinRemoveCommand pinRemoveCommand,
            PinChangeOverrideLabelCommand pinChangeOverrideLabelCommand,
            VideoMenuSubPageContent videoMenuSubPageContent,
            PrimaryViewPlayerManager primaryViewPlayerManager,
            ObservableMediaPlayer observableMediaPlayer,
            NiconicoLoginService niconicoLoginService,
            LogoutFromNiconicoCommand logoutFromNiconicoCommand,
            VideoItemsSelectionContext videoItemsSelectionContext,
            WindowService windowService,
            ApplicationLayoutManager applicationLayoutManager,
            OpenPageCommand openPageCommand,
            ITextInputDialogService textInputDialogService
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
            PrimaryViewPlayerManager = primaryViewPlayerManager;
            ObservableMediaPlayer = observableMediaPlayer;
            NiconicoLoginService = niconicoLoginService;
            LogoutFromNiconicoCommand = logoutFromNiconicoCommand;
            VideoItemsSelectionContext = videoItemsSelectionContext;
            WindowService = windowService;
            ApplicationLayoutManager = applicationLayoutManager;
            OpenPageCommand = openPageCommand;
            _textInputDialogService = textInputDialogService;

            Pins = PinSettings.GetSortedPins()
                .Select(x => new PinViewModel(x, PinSettings, this, _textInputDialogService))
                .ToObservableCollection()
                ;
        }

        public IEventAggregator EventAggregator { get; }
        public NiconicoSession NiconicoSession { get; }
        public PageManager PageManager { get; }
        public PinRepository PinSettings { get; }
        public AppearanceSettingsRepository AppearanceSettings { get; }
        public SearchCommand SearchCommand { get; }
        public PinRemoveCommand PinRemoveCommand { get; }
        public PinChangeOverrideLabelCommand PinChangeOverrideLabelCommand { get; }
        public VideoMenuSubPageContent VideoMenu { get; set; }
        public PrimaryViewPlayerManager PrimaryViewPlayerManager { get; }
        public ObservableMediaPlayer ObservableMediaPlayer { get; }
        public NiconicoLoginService NiconicoLoginService { get; }
        public LogoutFromNiconicoCommand LogoutFromNiconicoCommand { get; }
        public VideoItemsSelectionContext VideoItemsSelectionContext { get; }
        public WindowService WindowService { get; }
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public OpenPageCommand OpenPageCommand { get; }

        public ObservableCollection<PinViewModel> Pins { get; }

        // call from PrimaryWindowsCoreLayout.xaml.cs
        public void AddPin(Models.Pages.HohoemaPin pin)
        {
            if (pin != null)
            {
                var createdPin = PinSettings.CreateItem(pin);
                var pinVM = new PinViewModel(createdPin, PinSettings, this, _textInputDialogService);
                Pins.Insert(0, pinVM);
            }
        }

        



        private DelegateCommand _OpenAccountInfoCommand;
        private readonly ITextInputDialogService _textInputDialogService;

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


    
    public class PinViewModel : BindableBase
    {
        public PinViewModel(
            Models.Pages.HohoemaPin pin, 
            PinRepository pinRepository, 
            PrimaryWindowCoreLayoutViewModel parentVM,
            ITextInputDialogService textInputDialogService
            )
        {
            Pin = pin;
            _pinRepository = pinRepository;
            _parentVM = parentVM;
            _textInputDialogService = textInputDialogService;
        }

        private readonly PinRepository _pinRepository;
        private readonly PrimaryWindowCoreLayoutViewModel _parentVM;
        private readonly ITextInputDialogService _textInputDialogService;

        public Models.Pages.HohoemaPin Pin { get; }

        public HohoemaPageType PageType => Pin.PageType;
        public string Parameter => Pin.Parameter;
        public string Label => Pin.Label;

        private string _OverrideLabel;
        public string OverrideLabel
        {
            get { return _OverrideLabel; }
            set 
            {
                if (SetProperty(ref _OverrideLabel, value))
                {
                    Pin.OverrideLabel = value;
                    _pinRepository.UpdateItem(Pin);
                }
            }
        }


        private DelegateCommand _RemovePinCommand;
        public DelegateCommand RemovePinCommand =>
            _RemovePinCommand ??= new DelegateCommand(() =>
            {
                if (_pinRepository.DeleteItem(Pin.Id))
                {
                    _parentVM.Pins.Remove(this);
                }
            });

        private DelegateCommand _PinChangeOverrideLabelCommand;
        public DelegateCommand PinChangeOverrideLabelCommand =>
            _PinChangeOverrideLabelCommand ??= new DelegateCommand(async () =>
            {
                var name = Pin.OverrideLabel ?? $"{Pin.Label} ({Pin.PageType.Translate()})";
                var result = await _textInputDialogService.GetTextAsync(
                    $"RenameX".Translate(name),
                    "PinRenameDialogPlacefolder_EmptyToDefault".Translate(),
                    name,
                    (s) => true
                    );

                OverrideLabel = string.IsNullOrEmpty(result) ? null : result;
            });
        
}

}
