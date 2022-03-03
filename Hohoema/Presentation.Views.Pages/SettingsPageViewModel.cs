using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Application;
using Hohoema.Models.Domain.Niconico.NicoRepo;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Player;
using Hohoema.Models.Helpers;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.Niconico.Player;
using Hohoema.Presentation.Services;
using Hohoema.Models.UseCase.PageNavigation;
using I18NPortable;
using Microsoft.Services.Store.Engagement;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.Store;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.StartScreen;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Microsoft.Toolkit.Mvvm.Messaging;
using Hohoema.Models.Domain.VideoCache;
using Hohoema.Models.UseCase.VideoCache;
using Hohoema.Models.UseCase.Niconico.Player.Comment;
using System.Text;
using Microsoft.Toolkit.Uwp.Helpers;
using Xamarin.Essentials;
using Microsoft.Extensions.Logging;
using ZLogger;
using Hohoema.Presentation.Navigations;

namespace Hohoema.Presentation.ViewModels.Pages.Hohoema
{
    public class SettingsPageViewModel : HohoemaPageViewModelBase
	{
        private static Uri AppIssuePageUri = new Uri("https://github.com/tor4kichi/Hohoema/issues");

        public SettingsPageViewModel(
            PageManager pageManager,
            NotificationService toastService,
            Services.DialogService dialogService,
            PlayerSettings playerSettings,
            VideoRankingSettings rankingSettings,
            NicoRepoSettings nicoRepoSettings,
            AppearanceSettings appearanceSettings,
            VideoCacheSettings cacheSettings,
            VideoCacheFolderManager videoCacheFolderManager,
            ApplicationLayoutManager applicationLayoutManager,
            VideoFilteringSettings videoFilteringRepository,
            BackupManager backupManager,
            ILoggerFactory loggerFactory
            )
        {
            _notificationService = toastService;
            RankingSettings = rankingSettings;
            _HohoemaDialogService = dialogService;
            PlayerSettings = playerSettings;
            ActivityFeedSettings = nicoRepoSettings;
            AppearanceSettings = appearanceSettings;
            VideoCacheSettings = cacheSettings;
            _videoCacheFolderManager = videoCacheFolderManager;
            ApplicationLayoutManager = applicationLayoutManager;
            _videoFilteringRepository = videoFilteringRepository;
            _backupManager = backupManager;
            _logger = loggerFactory.CreateLogger<SettingsPageViewModel>();

            // NG Video Owner User Id
            NGVideoOwnerUserIdEnable = _videoFilteringRepository.ToReactivePropertyAsSynchronized(x => x.NGVideoOwnerUserIdEnable)
                .AddTo(_CompositeDisposable);
            NGVideoOwnerUserIds = _videoFilteringRepository.GetVideoOwnerIdFilteringEntries();

            OpenUserPageCommand = new RelayCommand<VideoOwnerIdFilteringEntry>(userIdInfo =>
            {
                pageManager.OpenPageWithId(HohoemaPageType.UserInfo, userIdInfo.UserId);
            });

            // NG Keyword on Video Title
            VideoTitleFilteringItems = new ObservableCollection<VideoFilteringTitleViewModel>();

            NGVideoTitleKeywordEnable = _videoFilteringRepository.ToReactivePropertyAsSynchronized(x => x.NGVideoTitleKeywordEnable)
                .AddTo(_CompositeDisposable);

            TestText = _videoFilteringRepository.ToReactivePropertyAsSynchronized(x => x.NGVideoTitleTestText)
                .AddTo(_CompositeDisposable);
            /*
            NGVideoTitleKeywordError = NGVideoTitleKeywords
                .Select(x =>
                {
                    if (x == null) { return null; }

                    var keywords = x.Split('\r');
                    var invalidRegex = keywords.FirstOrDefault(keyword =>
                    {
                        Regex regex = null;
                        try
                        {
                            regex = new Regex(keyword);
                        }
                        catch { }
                        return regex == null;
                    });

                    if (invalidRegex == null)
                    {
                        return null;
                    }
                    else
                    {
                        return $"Error in \"{invalidRegex}\"";
                    }
                })
                .ToReadOnlyReactiveProperty()
                .AddTo(_CompositeDisposable);
            */
            // アピアランス

            StartupPageType = AppearanceSettings.ToReactivePropertyAsSynchronized(x => x.FirstAppearPageType)
                .AddTo(_CompositeDisposable);

            var currentTheme = App.GetTheme();
            SelectedTheme = new ReactiveProperty<ElementTheme>(AppearanceSettings.ApplicationTheme, mode: ReactivePropertyMode.DistinctUntilChanged)
                .AddTo(_CompositeDisposable);

            SelectedTheme.Subscribe(theme =>
            {
                AppearanceSettings.ApplicationTheme = theme;

                ApplicationTheme appTheme;
                if (theme == ElementTheme.Default)
                {
                    appTheme = Views.Helpers.SystemThemeHelper.GetSystemTheme();
                }
                else if (theme == ElementTheme.Dark)
                {
                    appTheme = ApplicationTheme.Dark;
                }
                else
                {
                    appTheme = ApplicationTheme.Light;
                }

                App.SetTheme(appTheme);

                var appView = ApplicationView.GetForCurrentView();
                if (appTheme == ApplicationTheme.Light)
                {
                    appView.TitleBar.ButtonForegroundColor = Colors.Black;
                    appView.TitleBar.ButtonHoverBackgroundColor = Colors.DarkGray;
                    appView.TitleBar.ButtonHoverForegroundColor = Colors.Black;
                    appView.TitleBar.ButtonInactiveForegroundColor = Colors.Gray;
                }
                else
                {
                    appView.TitleBar.ButtonForegroundColor = Colors.White;
                    appView.TitleBar.ButtonHoverBackgroundColor = Colors.DimGray;
                    appView.TitleBar.ButtonHoverForegroundColor = Colors.White;
                    appView.TitleBar.ButtonInactiveForegroundColor = Colors.DarkGray;
                }
            })
                .AddTo(_CompositeDisposable);



            IsDefaultFullScreen = new ReactiveProperty<bool>(ApplicationView.PreferredLaunchWindowingMode == ApplicationViewWindowingMode.FullScreen)
                .AddTo(_CompositeDisposable);
            IsDefaultFullScreen.Subscribe(x =>
            {
                if (x)
                {
                    ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;
                }
                else
                {
                    ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto;
                }
            })
                .AddTo(_CompositeDisposable);

            AppearanceSettings.ObserveProperty(x => x.Locale, isPushCurrentValueAtFirst: false).Subscribe(locale => 
            {
                I18NPortable.I18N.Current.Locale = locale;
            })
                .AddTo(_CompositeDisposable);

            // キャッシュ
            DefaultCacheQuality = VideoCacheSettings.ToReactivePropertyAsSynchronized(x => x.DefaultCacheQuality)
                .AddTo(_CompositeDisposable);
            IsAllowDownloadOnMeteredNetwork = VideoCacheSettings.ToReactivePropertyAsSynchronized(x => x.IsAllowDownloadOnMeteredNetwork)
                .AddTo(_CompositeDisposable);
            MaxVideoCacheStorageSize = VideoCacheSettings.ToReactivePropertyAsSynchronized(x => x.MaxVideoCacheStorageSize)
                .AddTo(_CompositeDisposable);
            OpenCurrentCacheFolderCommand = new RelayCommand(async () =>
            {
                var folder = _videoCacheFolderManager.VideoCacheFolder;
                if (folder != null)
                {
                    await Windows.System.Launcher.LaunchFolderAsync(folder);
                }
            });

            // シェア
            IsLoginTwitter = new ReactiveProperty<bool>(/*TwitterHelper.IsLoggedIn*/ false)
                .AddTo(_CompositeDisposable);
            TwitterAccountScreenName = new ReactiveProperty<string>(/*TwitterHelper.TwitterUser?.ScreenName ?? ""*/)
                .AddTo(_CompositeDisposable);


            StringBuilder sb = new StringBuilder();
            sb.Append(SystemInformation.Instance.ApplicationName)
                .Append(" v").Append(SystemInformation.Instance.ApplicationVersion.ToFormattedString())
                .AppendLine();
            sb.Append(SystemInformation.Instance.OperatingSystem).Append(" ").Append(SystemInformation.Instance.OperatingSystemArchitecture)
                .Append("(").Append(SystemInformation.Instance.OperatingSystemVersion).Append(")")
                .Append(" ").Append(DeviceInfo.Idiom)
                ;
            VersionText = sb.ToString();

            IsDebugModeEnabled = new ReactiveProperty<bool>((App.Current as App).IsDebugModeEnabled, mode: ReactivePropertyMode.DistinctUntilChanged)
                .AddTo(_CompositeDisposable);
            IsDebugModeEnabled.Subscribe(isEnabled =>
            {
                (App.Current as App).IsDebugModeEnabled = isEnabled;
            })
            .AddTo(_CompositeDisposable);
        }

        Services.DialogService _HohoemaDialogService;
        private readonly VideoCacheFolderManager _videoCacheFolderManager;
        private readonly VideoFilteringSettings _videoFilteringRepository;
        private readonly BackupManager _backupManager;
        private readonly ILogger _logger;
        private readonly CommentFilteringFacade _commentFiltering;

        public NotificationService _notificationService { get; private set; }
        public PlayerSettings PlayerSettings { get; }
        public VideoRankingSettings RankingSettings { get; }
        public NicoRepoSettings ActivityFeedSettings { get; }
        public AppearanceSettings AppearanceSettings { get; }
        public VideoCacheSettings VideoCacheSettings { get; }
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        

        // フィルタ
        public ReactiveProperty<bool> NGVideoOwnerUserIdEnable { get; private set; }
        public List<VideoOwnerIdFilteringEntry> NGVideoOwnerUserIds { get; private set; }
        public RelayCommand<VideoOwnerIdFilteringEntry> OpenUserPageCommand { get; }


        public ReactiveProperty<bool> NGVideoTitleKeywordEnable { get; private set; }

        public ObservableCollection<VideoFilteringTitleViewModel> VideoTitleFilteringItems { get; private set; }
        public ReadOnlyReactiveProperty<string> NGVideoTitleKeywordError { get; private set; }

        private RelayCommand _AddVideoTitleFilterEntryCommand;
        public RelayCommand AddVideoTitleFilterEntryCommand =>
            _AddVideoTitleFilterEntryCommand ?? (_AddVideoTitleFilterEntryCommand = new RelayCommand(ExecuteAddVideoTitleFilterEntryCommand));

        void ExecuteAddVideoTitleFilterEntryCommand()
        {
            var entry = _videoFilteringRepository.CreateVideoTitleFiltering();
            VideoTitleFilteringItems.Insert(0, new VideoFilteringTitleViewModel(entry, OnRemoveVideoTitleFilterEntry, _videoFilteringRepository, TestText));
        }


        public List<NavigationViewPaneDisplayMode> PaneDisplayModeItems { get; } = Enum.GetValues(typeof(NavigationViewPaneDisplayMode)).Cast<NavigationViewPaneDisplayMode>().ToList();

        
        public ReactiveProperty<ElementTheme> SelectedTheme { get; private set; }
        public static bool ThemeChanged { get; private set; } = false;


        public ReactiveProperty<bool> IsDefaultFullScreen { get; private set; }

        public ReactiveProperty<HohoemaPageType> StartupPageType { get; private set; }

        public List<HohoemaPageType> StartupPageTypeList { get; } = new List<HohoemaPageType>()
        {
            HohoemaPageType.NicoRepo,
            HohoemaPageType.Search,
            HohoemaPageType.RankingCategoryList,
            HohoemaPageType.CacheManagement,
            HohoemaPageType.SubscriptionManagement,
            HohoemaPageType.FollowManage,
            HohoemaPageType.UserMylist,
            HohoemaPageType.Timeshift,
        };

        private RelayCommand _ToggleFullScreenCommand;
        public RelayCommand ToggleFullScreenCommand
        {
            get
            {
                return _ToggleFullScreenCommand
                    ?? (_ToggleFullScreenCommand = new RelayCommand(() =>
                    {
                        var appView = ApplicationView.GetForCurrentView();

                        if (!appView.IsFullScreenMode)
                        {
                            appView.TryEnterFullScreenMode();
                        }
                        else
                        {
                            appView.ExitFullScreenMode();
                        }
                    }));
            }
        }


        // キャッシュ
        public ReactiveProperty<NicoVideoQuality> DefaultCacheQuality { get; private set; }

        public List<NicoVideoQuality> AvairableCacheQualities { get; } = new List<NicoVideoQuality>()
        {
            NicoVideoQuality.SuperHigh,
            NicoVideoQuality.High,
            NicoVideoQuality.Midium,
            NicoVideoQuality.Low,
            NicoVideoQuality.Mobile,
        };

        public ReactiveProperty<bool> IsAllowDownloadOnMeteredNetwork { get; private set; }
        public ReactiveProperty<long?> MaxVideoCacheStorageSize { get; private set; }
        public RelayCommand OpenCurrentCacheFolderCommand { get; }

        private RelayCommand _ChangeCacheVideoFolderCommand;
        public RelayCommand ChangeCacheVideoFolderCommand =>
            _ChangeCacheVideoFolderCommand ??= new RelayCommand(async () =>
            {
                await _videoCacheFolderManager.ChangeVideoCacheFolder();
            });





        // シェア
        public ReactiveProperty<bool> IsLoginTwitter { get; private set; }
        public ReactiveProperty<string> TwitterAccountScreenName { get; private set; }
        private RelayCommand _LogInToTwitterCommand;
        public RelayCommand LogInToTwitterCommand
        {
            get
            {
                return _LogInToTwitterCommand
                    ?? (_LogInToTwitterCommand = new RelayCommand(async () =>
                    {
                        /*
                        if (await TwitterHelper.LoginOrRefreshToken())
                        {
                            IsLoginTwitter.Value = TwitterHelper.IsLoggedIn;
                            TwitterAccountScreenName.Value = TwitterHelper.TwitterUser?.ScreenName ?? "";
                        }
                        */

                        await Task.CompletedTask;
                    }
                    ));
            }
        }

        private RelayCommand _LogoutTwitterCommand;
        public RelayCommand LogoutTwitterCommand
        {
            get
            {
                return _LogoutTwitterCommand
                    ?? (_LogoutTwitterCommand = new RelayCommand(() =>
                    {
//                        TwitterHelper.Logout();

                        IsLoginTwitter.Value = false;
                        TwitterAccountScreenName.Value = "";
                    }
                    ));
            }
        }

        // アバウト
        public string VersionText { get; private set; }

        public List<LisenceItemViewModel> LisenceItems { get; private set; }

        public List<ProductViewModel> PurchaseItems { get; private set; }

        private Version _CurrentVersion;
        public Version CurrentVersion
        {
            get
            {
                var ver = Windows.ApplicationModel.Package.Current.Id.Version;
                return _CurrentVersion
                    ?? (_CurrentVersion = new Version(ver.Major, ver.Minor, ver.Build));
            }
        }

        private RelayCommand _ShowUpdateNoticeCommand;
        public RelayCommand ShowUpdateNoticeCommand
        {
            get
            {
                return _ShowUpdateNoticeCommand
                    ?? (_ShowUpdateNoticeCommand = new RelayCommand(async () =>
                    {
                        await AppUpdateNotice.ShowReleaseNotePageOnBrowserAsync();
                    }));
            }
        }

        private RelayCommand<ProductViewModel> _ShowCheerPurchaseCommand;
        public RelayCommand<ProductViewModel> ShowCheerPurchaseCommand
        {
            get
            {
                return _ShowCheerPurchaseCommand
                    ?? (_ShowCheerPurchaseCommand = new RelayCommand<ProductViewModel>(async (product) =>
                    {
                        var result = await Models.Domain.Purchase.HohoemaPurchase.RequestPurchase(product.ProductListing);

                        product.Update();

                        Debug.WriteLine(result.ToString());
                    }));
            }
        }


        private RelayCommand _LaunchAppReviewCommand;
        public RelayCommand LaunchAppReviewCommand
        {
            get
            {
                return _LaunchAppReviewCommand
                    ?? (_LaunchAppReviewCommand = new RelayCommand(async () =>
                    {
                        await Microsoft.Toolkit.Uwp.Helpers.SystemInformation.LaunchStoreForReviewAsync();
                        //await Launcher.LaunchUriAsync(AppReviewUri);
                    }));
            }
        }

        private RelayCommand _LaunchFeedbackHubCommand;
        public RelayCommand LaunchFeedbackHubCommand
        {
            get
            {
                return _LaunchFeedbackHubCommand
                    ?? (_LaunchFeedbackHubCommand = new RelayCommand(async () =>
                    {
                        if (IsSupportedFeedbackHub)
                        {
                            await StoreServicesFeedbackLauncher.GetDefault().LaunchAsync();
                        }
                    }));
            }
        }

        private RelayCommand _ShowIssuesWithBrowserCommand;
        public RelayCommand ShowIssuesWithBrowserCommand
        {
            get
            {
                return _ShowIssuesWithBrowserCommand
                    ?? (_ShowIssuesWithBrowserCommand = new RelayCommand(async () =>
                    {
                        await Windows.System.Launcher.LaunchUriAsync(AppIssuePageUri);
                    }));
            }
        }


        public ReactiveProperty<bool> IsDebugModeEnabled { get; }


        private RelayCommand _CopyVersionTextToClipboardCommand;
        public RelayCommand CopyVersionTextToClipboardCommand
        {
            get
            {
                return _CopyVersionTextToClipboardCommand
                    ?? (_CopyVersionTextToClipboardCommand = new RelayCommand(() =>
                    {
                        ClipboardHelper.CopyToClipboard(CurrentVersion.ToString());
                    }));
            }
        }

        public bool IsSupportedFeedbackHub { get; } = StoreServicesFeedbackLauncher.IsSupported();




        // セカンダリタイル関連

        private RelayCommand _AddTransparencySecondaryTile;
        public RelayCommand AddTransparencySecondaryTile
        {
            get
            {
                return _AddTransparencySecondaryTile
                    ?? (_AddTransparencySecondaryTile = new RelayCommand(async () =>
                    {
                        Uri square150x150Logo = new Uri("ms-appx:///Assets/Square150x150Logo.scale-100.png");
                        SecondaryTile secondaryTile = new SecondaryTile(@"Hohoema",
                                                "Hohoema",
                                                "temp",
                                                square150x150Logo,
                                                TileSize.Square150x150);
                        secondaryTile.VisualElements.BackgroundColor = Windows.UI.Colors.Transparent;
                        secondaryTile.VisualElements.Square150x150Logo = new Uri("ms-appx:///Assets/Square150x150Logo.scale-100.png");
                        secondaryTile.VisualElements.Square71x71Logo = new Uri("ms-appx:///Assets/Square71x71Logo.scale-100.png");
                        secondaryTile.VisualElements.Square310x310Logo = new Uri("ms-appx:///Assets/Square310x310Logo.scale-100.png");
                        secondaryTile.VisualElements.Wide310x150Logo = new Uri("ms-appx:///Assets/Wide310x150Logo.scale-100.png");
                        secondaryTile.VisualElements.Square44x44Logo = new Uri("ms-appx:///Assets/Square44x44Logo.targetsize-48.png");
//                        secondaryTile.VisualElements.Square30x30Logo = new Uri("ms-appx:///Assets/Square30x30Logo.scale-100.png");
                        secondaryTile.VisualElements.ShowNameOnSquare150x150Logo = true;
                        secondaryTile.VisualElements.ShowNameOnSquare310x310Logo = true;
                        secondaryTile.VisualElements.ShowNameOnWide310x150Logo = true;


                        
                        if (false == await secondaryTile.RequestCreateAsync())
                        {
                            _logger.ZLogError("Failed secondary tile creation.");
                        }
                    }
                    ));
            }
        }

        bool _NowNavigateProccess = false;


        public override async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            await base.OnNavigatedToAsync(parameters);

            _NowNavigateProccess = true;

            try
            {
                VideoTitleFilteringItems.Clear();
                foreach (var item in _videoFilteringRepository.GetVideoTitleFilteringEntries().Select(x =>
                    new VideoFilteringTitleViewModel(x, OnRemoveVideoTitleFilterEntry, _videoFilteringRepository, TestText))
                    )
                {
                    VideoTitleFilteringItems.Add(item);
                }
                

                try
                {
                    var listing = await Models.Domain.Purchase.HohoemaPurchase.GetAvailableCheersAddOn();
                    PurchaseItems = listing.ProductListings.Select(x => new ProductViewModel(x.Value)).ToList();
                    OnPropertyChanged(nameof(PurchaseItems));
                }
                catch { }


                
                var lisenceSummary = await LisenceSummary.Load();
                LisenceItems = lisenceSummary.Items
                    .OrderBy(x => x.Name)
                    .Select(x => new LisenceItemViewModel(x))
                    .ToList();
                OnPropertyChanged(nameof(LisenceItems));
            }
            finally
            {
                _NowNavigateProccess = false;
            }
        }


        private void OnRemoveVideoTitleFilterEntry(VideoTitleFilteringEntry entry)
        {
            var removeTarget = VideoTitleFilteringItems.FirstOrDefault(x => x.NGKeywordInfo.Id == entry.Id);
            if (removeTarget != null)
            {
                VideoTitleFilteringItems.Remove(removeTarget);
                _videoFilteringRepository.RemoveVideoTitleFiltering(entry);
                removeTarget.Dispose();
            }
        }


        public ReactiveProperty<string> TestText { get; }

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
            base.OnNavigatedFrom(parameters);
        }

        private void OnRemoveNGCommentUserIdFromList(string userId)
        {
            _videoFilteringRepository.RemoveHiddenVideoOwnerId(userId);
        }



        #region Backup

        private RelayCommand _ExportBackupCommand;
        public RelayCommand ExportBackupCommand =>
            _ExportBackupCommand ?? (_ExportBackupCommand = new RelayCommand(ExecuteExportBackupCommand));

        async void ExecuteExportBackupCommand()
        {
            try
            {
                var picker = new FileSavePicker();
                picker.DefaultFileExtension = ".json";
                picker.SuggestedFileName = $"hohoema-backup-{DateTime.Today:yyyy-MM-dd}";
                picker.FileTypeChoices.Add("Hohoema backup File", new List<string>() { ".json" });
                var file = await picker.PickSaveFileAsync();
                if (file != null)
                {
                    await _backupManager.BackupAsync(file, default);

                    _notificationService.ShowLiteInAppNotification_Success("バックアップを保存しました");
                }
            }
            catch (Exception e)
            {
                _logger.ZLogError(e, "Backup export failed");
            }
        }


        private RelayCommand _ImportBackupCommand;
        public RelayCommand ImportBackupCommand =>
            _ImportBackupCommand ?? (_ImportBackupCommand = new RelayCommand(ExecuteImportBackupCommand));

        async void ExecuteImportBackupCommand()
        {
            try
            {
                var picker = new FileOpenPicker();
                picker.ViewMode = PickerViewMode.List;
                picker.FileTypeFilter.Add(".json");
                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    try
                    {
                        var backup = await _backupManager.ReadBackupContainerAsync(file, default);

                        Action<BackupContainer>[] BackupActions = new Action<BackupContainer>[]
                        {
                        _backupManager.RestoreLocalMylist,
                        _backupManager.RestoreSubscription,
                        _backupManager.RestorePin,
                        _backupManager.RestoreRankingSettings,
                        _backupManager.RestoreVideoFilteringSettings,
                        _backupManager.RestorePlayerSettings,
                        _backupManager.RestoreAppearanceSettings,
                        _backupManager.RestoreNicoRepoSettings,
                        _backupManager.RestoreCommentSettings,
                        };

                        List<Exception> exceptions = new List<Exception>();
                        foreach (var backupAction in BackupActions)
                        {
                            try
                            {
                                backupAction(backup);
                            }
                            catch (Exception e)
                            {
                                exceptions.Add(e);
                            }
                        }

                        WeakReferenceMessenger.Default.Send<SettingsRestoredMessage>();

                        _notificationService.ShowLiteInAppNotification_Success("BackupRestoreComplete".Translate());

                        NGVideoOwnerUserIds = _videoFilteringRepository.GetVideoOwnerIdFilteringEntries();
                        OnPropertyChanged(nameof(NGVideoOwnerUserIds));

                        VideoTitleFilteringItems.Clear();
                        foreach (var item in _videoFilteringRepository.GetVideoTitleFilteringEntries().Select(x =>
                            new VideoFilteringTitleViewModel(x, OnRemoveVideoTitleFilterEntry, _videoFilteringRepository, TestText)))
                        {
                            VideoTitleFilteringItems.Add(item);
                        }

                        if (exceptions.Any())
                        {
                            throw new AggregateException(exceptions);
                        }
                    }
                    catch
                    {
                        _notificationService.ShowLiteInAppNotification_Fail("BackupRestoreFailed".Translate());
                        throw;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.ZLogError(e, "Backup import failed");
            }
        }


        #endregion
    }



    public class RemovableListItem<T> : IRemovableListItem

    {
		public T Source { get; private set; }
		public Action<T> OnRemove { get; private set; }

		public string Label { get; private set; }
		public RemovableListItem(T source, string content, Action<T> onRemovedAction)
		{
			Source = source;
			Label = content;
			OnRemove = onRemovedAction;

			RemoveCommand = new RelayCommand(() => 
			{
				onRemovedAction(Source);
			});
		}


		public ICommand RemoveCommand { get; private set; }
	}


	public class VideoFilteringTitleViewModel : IRemovableListItem, IDisposable
    {
		public VideoFilteringTitleViewModel(
            VideoTitleFilteringEntry ngTitleInfo, 
            Action<VideoTitleFilteringEntry> onRemoveAction, 
            VideoFilteringSettings videoFilteringRepository,
            IObservable<string> testKeyword
            )
		{
			NGKeywordInfo = ngTitleInfo;
			_OnRemoveAction = onRemoveAction;
            _videoFilteringRepository = videoFilteringRepository;
            Label = NGKeywordInfo.Keyword;
			Keyword = new ReactiveProperty<string>(NGKeywordInfo.Keyword);

            Keyword.Subscribe(x =>
			{
				NGKeywordInfo.Keyword = x;
            })
                .AddTo(_disposables);

            IsValidKeyword =
                    Keyword
                    .Where(x => !string.IsNullOrWhiteSpace(x))
					.Select(x =>
					{
                        try
                        {
                            _ = new Regex(x);
                            return true;
                        }
                        catch 
                        {
                            return false;
                        }
                    })
					.ToReactiveProperty(mode: ReactivePropertyMode.RaiseLatestValueOnSubscribe)
                .AddTo(_disposables);

            IsInvalidKeyword = IsValidKeyword.Select(x => !x)
				.ToReactiveProperty()
                .AddTo(_disposables);

            IsValidKeyword.Where(x => x).Subscribe(_ => 
            {
                _videoFilteringRepository.UpdateVideoTitleFiltering(NGKeywordInfo);
            })
                .AddTo(_disposables);

            IsTestPassed = testKeyword
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Where(_ => IsValidKeyword.Value)
                .Select(x =>
                {
                    return Regex.IsMatch(Keyword.Value, x);
                })
                .ToReadOnlyReactivePropertySlim();

            RemoveCommand = new RelayCommand(() => 
			{
				_OnRemoveAction(this.NGKeywordInfo);
			});
		}

        CompositeDisposable _disposables = new CompositeDisposable();

		public void Dispose()
		{
            _disposables.Dispose();
        }

		public ReactiveProperty<string> Keyword { get; private set; }

		public ReactiveProperty<bool> IsValidKeyword { get; private set; }
		public ReactiveProperty<bool> IsInvalidKeyword { get; private set; }


        public ReadOnlyReactivePropertySlim<bool> IsTestPassed { get; private set; }


        public string Label { get; private set; }
		public ICommand RemoveCommand { get; private set; }
		
		public VideoTitleFilteringEntry NGKeywordInfo { get; }
		Action<VideoTitleFilteringEntry> _OnRemoveAction;
        private readonly VideoFilteringSettings _videoFilteringRepository;
    }

    /*
	public static class RemovableSettingsListItemHelper
	{
		public static RemovableListItem<string> VideoIdInfoToRemovableListItemVM(VideoIdInfo info, Action<string> removeAction)
		{
			var roundedDesc = info.Description.Substring(0, Math.Min(info.Description.Length - 1, 10));
			return new RemovableListItem<string>(info.VideoId, $"{info.VideoId} | {roundedDesc}", removeAction);
		}

		public static RemovableListItem<string> UserIdInfoToRemovableListItemVM(UserIdInfo info, Action<string> removeAction)
		{
			var roundedDesc = info.Description.Substring(0, Math.Min(info.Description.Length - 1, 10));
			return new RemovableListItem<string>(info.UserId, $"{info.UserId} | {roundedDesc}", removeAction);
		}
	}
    */

	public interface IRemovableListItem
    {
        string Label { get; }
        ICommand RemoveCommand { get; }
    }



#region About 

    public class ProductViewModel : ObservableObject
    {
        private bool _IsActive;
        public bool IsActive
        {
            get { return _IsActive; }
            set { SetProperty(ref _IsActive, value); }
        }

        public ProductListing ProductListing { get; set; }
        public ProductViewModel(ProductListing product)
        {
            ProductListing = product;

            Update();
        }

        internal void Update()
        {
            IsActive = Models.Domain.Purchase.HohoemaPurchase.ProductIsActive(ProductListing);
        }
    }



    public class LisenceItemViewModel
    {
        public LisenceItemViewModel(LisenceItem item)
        {
            Name = item.Name;
            Site = item.Site;
            Authors = item.Authors.ToList();
            LisenceType = item.LisenceType;
            LisencePageUrl = item.LisencePageUrl;
        }

        public string Name { get; private set; }
        public Uri Site { get; private set; }
        public List<string> Authors { get; private set; }
        public string LisenceType { get; private set; }
        public Uri LisencePageUrl { get; private set; }

        string _LisenceText;
        public string LisenceText
        {
            get
            {
                return _LisenceText
                    ?? (_LisenceText = LoadLisenceText());
            }
        }

        string LoadLisenceText()
        {
            string path = Windows.ApplicationModel.Package.Current.InstalledLocation.Path + "\\Assets\\LibLisencies\\" + Name + ".txt";

            try
            {
                var file = StorageFile.GetFileFromPathAsync(path).AsTask();

                file.Wait(3000);

                var task = FileIO.ReadTextAsync(file.Result).AsTask();
                task.Wait(1000);
                return task.Result;
            }
            catch
            {
                return "";
            }
        }


        private string LisenceTypeToText(LisenceType type)
        {
            switch (type)
            {
                case Models.Domain.LisenceType.MIT:
                    return "MIT";
                case Models.Domain.LisenceType.MS_PL:
                    return "Microsoft Public Lisence";
                case Models.Domain.LisenceType.Apache_v2:
                    return "Apache Lisence version 2.0";
                case Models.Domain.LisenceType.GPL_v3:
                    return "GNU General Public License Version 3";
                case Models.Domain.LisenceType.Simplified_BSD:
                    return "二条項BSDライセンス";
                case Models.Domain.LisenceType.CC_BY_40:
                    return "クリエイティブ・コモンズ 表示 4.0 国際";
                case Models.Domain.LisenceType.SIL_OFL_v1_1:
                    return "SIL OPEN FONT LICENSE Version 1.1";
                default:
                    throw new NotSupportedException(type.ToString());
            }
        }

    }

#endregion

}
