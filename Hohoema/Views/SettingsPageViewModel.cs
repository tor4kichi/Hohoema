#nullable enable
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Helpers;
using Hohoema.Models;
using Hohoema.Models.Application;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Player;
using Hohoema.Models.VideoCache;
using Hohoema.Services;
using Hohoema.Services.Player.Videos;
using Hohoema.Services.VideoCache;
using I18NPortable;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.UI.Xaml.Controls;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Store;
using Windows.Foundation;
using Windows.Services.Store;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.StartScreen;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Xamarin.Essentials;
using ZLogger;

namespace Hohoema.ViewModels.Pages.Hohoema;

public sealed partial class SettingsPageViewModel : HohoemaPageViewModelBase
{
    private static Uri AppIssuePageUri = new Uri("https://github.com/tor4kichi/Hohoema/issues");

    private readonly IMessenger _messenger;
    private readonly ILogger _logger;
    private readonly IDialogService _HohoemaDialogService;
    private readonly VideoCacheFolderManager _videoCacheFolderManager;
    private readonly VideoFilteringSettings _videoFilteringRepository;
    private readonly BackupManager _backupManager;
    private readonly CommentFilteringFacade _commentFiltering;    
    private readonly INotificationService _notificationService;
    private readonly PlayerSettings PlayerSettings;
    private readonly VideoRankingSettings RankingSettings;
    public AppearanceSettings AppearanceSettings { get; }
    private readonly VideoCacheSettings VideoCacheSettings;
    public ApplicationLayoutManager ApplicationLayoutManager { get; }


    public SettingsPageViewModel(
        IMessenger messenger,
        ILoggerFactory loggerFactory,
        IDialogService dialogService,        
        INotificationService toastService,
        PlayerSettings playerSettings,
        VideoRankingSettings rankingSettings,
        AppearanceSettings appearanceSettings,
        VideoCacheSettings cacheSettings,
        VideoCacheFolderManager videoCacheFolderManager,
        ApplicationLayoutManager applicationLayoutManager,
        VideoFilteringSettings videoFilteringRepository,
        BackupManager backupManager
        )
    {
        _messenger = messenger;
        _notificationService = toastService;
        RankingSettings = rankingSettings;
        _HohoemaDialogService = dialogService;
        PlayerSettings = playerSettings;
        AppearanceSettings = appearanceSettings;
        VideoCacheSettings = cacheSettings;
        _videoCacheFolderManager = videoCacheFolderManager;
        ApplicationLayoutManager = applicationLayoutManager;
        _videoFilteringRepository = videoFilteringRepository;
        _backupManager = backupManager;
        _logger = loggerFactory.CreateLogger<SettingsPageViewModel>();

        _isDebugModeEnabled = (App.Current as App).IsDebugModeEnabled;

        InitializeAppearanceSettings();
        InitializeFilters();
        InitializeCache();

#if DEBUG
        this.ObservePropertiesDebugOutput()
            .AddTo(_CompositeDisposable);
#endif
    }

    public override async Task OnNavigatedToAsync(INavigationParameters parameters)
    {
        HasAppUpdate = false;

        await base.OnNavigatedToAsync(parameters);

        foreach (var item in _videoFilteringRepository.GetVideoTitleFilteringEntries().Select(x =>
                new VideoFilteringTitleViewModel(x, OnRemoveVideoTitleFilterEntry, _videoFilteringRepository, this.ObserveProperty(x => x.TestText)))
                )
        {
            VideoTitleFilteringItems.Add(item);
        }

        _ = LoadAddonsAsync(NavigationCancellationToken);
        _ = LoadLisenceItemsAsync(NavigationCancellationToken);
        _update = await App.Current.CheckUpdateAsync();
        CurrentAppVersion = Windows.ApplicationModel.AppInfo.Current.Package.Id.Version;
        var updateVer = _update.AppUpdate?.Package.Id.Version ?? default;
        UpdateAppVersion = updateVer;
        HasAppUpdate = _update.HasAppUpdate;
    }

    private CheckUpdateResult? _update;

    [ObservableProperty]
    private PackageVersion _currentAppVersion;

    [ObservableProperty]
    private PackageVersion _updateAppVersion;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AppUpdateCommand))]
    private bool _hasAppUpdate;

    [ObservableProperty]
    private bool _nowProgressUpdateDownloadAndInstall;

    [ObservableProperty]
    private float _updateProgress;

    [ObservableProperty]
    private bool _requiredRestartForUpdateCompleted;

   
    [RelayCommand(CanExecute = nameof(HasAppUpdate))]
    async Task AppUpdateAsync()
    {
        if (_update == null || _update.HasAppUpdate is false) { return; }

        UpdateProgress = 0;
        NowProgressUpdateDownloadAndInstall = true;
        try
        {
            var op = _update.DownloadAndInstallAllUpdatesAsync();

            op.Progress = (x, y) =>
            {
                UpdateProgress = (float)y.TotalDownloadProgress;
            };
            var result = await op.AsTask();
            Debug.WriteLine($"{result.OverallState}");

            RequiredRestartForUpdateCompleted = result.OverallState == StorePackageUpdateState.Completed;
        }
        finally
        {
            NowProgressUpdateDownloadAndInstall = false;
        }

        if (AppearanceSettings.AutoRestartOnUpdateInstalled)
        {
            await RestartForAppUpdateInstalledAsync();
        }
    }

    [RelayCommand]
    async Task RestartForAppUpdateInstalledAsync()
    {
        if (RequiredRestartForUpdateCompleted)
        {
            await CoreApplication.RequestRestartAsync("");
        }
    }

    async Task LoadAddonsAsync(CancellationToken ct)
    {
        try
        {
            var listing = await Models.Purchase.HohoemaPurchase.GetAvailableCheersAddonsAsync(ct);
            PurchaseItems = listing.ProductListings.Select(x => new ProductViewModel(x.Value)).ToList();
            OnPropertyChanged(nameof(PurchaseItems));
        }
        catch { }
    }

    async Task LoadLisenceItemsAsync(CancellationToken ct)
    {
        var lisenceSummary = await LisenceSummary.LoadAsync(ct);
        LisenceItems = lisenceSummary.Items
            .OrderBy(x => x.Name)
            .Select(x => new LisenceItemViewModel(x))
            .ToList();
        OnPropertyChanged(nameof(LisenceItems));
    }

    public override void OnNavigatedFrom(INavigationParameters parameters)
    {
        base.OnNavigatedFrom(parameters);

        foreach (var item in VideoTitleFilteringItems)
        {
            item.Dispose();
        }
        VideoTitleFilteringItems.Clear();
    }




    // デバッグ設定
    [ObservableProperty]
    private bool _isDebugModeEnabled;
    partial void OnIsDebugModeEnabledChanged(bool value)
    {
        (App.Current as App).IsDebugModeEnabled = value;
    }

    
    // アプリの表示設定

    private void InitializeAppearanceSettings()
    {
        var currentTheme = App.GetTheme();
        _selectedTheme = AppearanceSettings.ApplicationTheme;
        _startupPageType = AppearanceSettings.FirstAppearPageType;
        _isDefaultFullScreen = ApplicationView.PreferredLaunchWindowingMode == ApplicationViewWindowingMode.FullScreen;

        AppearanceSettings.ObserveProperty(x => x.Locale, isPushCurrentValueAtFirst: false).Subscribe(locale =>
        {
            I18NPortable.I18N.Current.Locale = locale;
        })
            .AddTo(_CompositeDisposable);

        AppearanceSettings.ObserveProperty(x => x.VideoListThumbnailCacheMaxCount)
            .Subscribe(x => ImageCache.Instance.MaxMemoryCacheCount = x)
            .AddTo(_CompositeDisposable);        
    }

    [RelayCommand]
    async Task ClearCacheAsync()
    {
        await ImageCache.Instance.ClearAsync();
    }


    public List<NavigationViewPaneDisplayMode> PaneDisplayModeItems { get; } = Enum.GetValues(typeof(NavigationViewPaneDisplayMode)).Cast<NavigationViewPaneDisplayMode>().ToList();



    public List<HohoemaPageType> StartupPageTypeList { get; } = new List<HohoemaPageType>()
    {
        HohoemaPageType.FollowingsActivity,
        HohoemaPageType.Search,
        HohoemaPageType.RankingCategoryList,
        HohoemaPageType.CacheManagement,
        HohoemaPageType.SubscriptionManagement,
        HohoemaPageType.FollowManage,
        HohoemaPageType.UserMylist,
        HohoemaPageType.Timeshift,
    };

    [ObservableProperty]
    private HohoemaPageType _startupPageType;
    partial void OnStartupPageTypeChanged(HohoemaPageType value)
    {
        AppearanceSettings.FirstAppearPageType = value;
    }


    public static bool ThemeChanged { get; private set; } = false;

    [ObservableProperty]
    private ElementTheme _selectedTheme;
    partial void OnSelectedThemeChanged(ElementTheme theme)
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
    }



    [ObservableProperty]
    private bool _isDefaultFullScreen;
    partial void OnIsDefaultFullScreenChanged(bool value)
    {
        ApplicationView.PreferredLaunchWindowingMode = value
            ? ApplicationViewWindowingMode.FullScreen
            : ApplicationViewWindowingMode.Auto
            ;

        // PrimaryViewPlayerManagerの実装が問題で以下のコードを実行すると画面からUIが消える
        //if (value)
        //{
        //    ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
        //}
        //else
        //{
        //    ApplicationView.GetForCurrentView().ExitFullScreenMode();
        //}
    }

    // フィルタ

    private void InitializeFilters()
    {
        // NG Video Owner User Id
        _ngVideoOwnerUserIdEnable = _videoFilteringRepository.NGVideoOwnerUserIdEnable;
        _nGVideoOwnerUserIds = _videoFilteringRepository.GetVideoOwnerIdFilteringEntries();

        // NG Keyword on Video Title       
        _nGVideoTitleKeywordEnable = _videoFilteringRepository.NGVideoTitleKeywordEnable;

        _testText = _videoFilteringRepository.NGVideoTitleTestText;
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

    }

    [ObservableProperty]
    private bool _ngVideoOwnerUserIdEnable;

    partial void OnNgVideoOwnerUserIdEnableChanged(bool value)
    {
        _videoFilteringRepository.NGVideoOwnerUserIdEnable = value;
    }

    [ObservableProperty]
    private List<VideoOwnerIdFilteringEntry> _nGVideoOwnerUserIds;


    [RelayCommand]
    async Task OpenUserPage(VideoOwnerIdFilteringEntry entry)
    {
        await _messenger.OpenPageWithIdAsync(HohoemaPageType.UserInfo, entry.UserId);
    }


    [ObservableProperty]
    private bool _nGVideoTitleKeywordEnable;

    partial void OnNGVideoTitleKeywordEnableChanged(bool value)
    {
        _videoFilteringRepository.NGVideoTitleKeywordEnable = value;
    }

    public ObservableCollection<VideoFilteringTitleViewModel> VideoTitleFilteringItems { get; } = new();

    [ObservableProperty]
    private string _nGVideoTitleKeywordError;



    [RelayCommand]
    private void AddVideoTitleFilterEntry()
    {
        var entry = _videoFilteringRepository.CreateVideoTitleFiltering();
        VideoTitleFilteringItems.Insert(0, new VideoFilteringTitleViewModel(entry, OnRemoveVideoTitleFilterEntry, _videoFilteringRepository, this.ObserveProperty(x => x.TestText)));
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

    [ObservableProperty]
    private string _testText;
    partial void OnTestTextChanged(string value)
    {
        _videoFilteringRepository.NGVideoTitleTestText = value;
    }

    private void OnRemoveNGCommentUserIdFromList(string userId)
    {
        _videoFilteringRepository.RemoveHiddenVideoOwnerId(userId);
    }




    // キャッシュ
    private void InitializeCache()
    {        
        _defaultCacheQuality = VideoCacheSettings.DefaultCacheQuality;
        _isAllowDownloadOnMeteredNetwork = VideoCacheSettings.IsAllowDownloadOnMeteredNetwork;
        _maxVideoCacheStorageSize = VideoCacheSettings.MaxVideoCacheStorageSize;        
    }

    public ImmutableArray<NicoVideoQuality> AvairableCacheQualities { get; } = new NicoVideoQuality[]
    {
        NicoVideoQuality.SuperHigh,
        NicoVideoQuality.High,
        NicoVideoQuality.Midium,
        NicoVideoQuality.Low,
        NicoVideoQuality.Mobile,
    }.ToImmutableArray();

    [ObservableProperty]
    private NicoVideoQuality _defaultCacheQuality;
    partial void OnDefaultCacheQualityChanged(NicoVideoQuality value)
    {
        VideoCacheSettings.DefaultCacheQuality = value;
    }

    [ObservableProperty]
    private bool _isAllowDownloadOnMeteredNetwork;
    partial void OnIsAllowDownloadOnMeteredNetworkChanged(bool value)
    {
        VideoCacheSettings.IsAllowDownloadOnMeteredNetwork = value;
    }

    [ObservableProperty]
    private long? _maxVideoCacheStorageSize;
    private StorePackageUpdate _appUpdate;

    partial void OnMaxVideoCacheStorageSizeChanged(long? value)
    {
        VideoCacheSettings.MaxVideoCacheStorageSize = value;
    }

    [RelayCommand]
    async Task OpenCurrentCacheFolder()
    {
        var folder = _videoCacheFolderManager.VideoCacheFolder;
        if (folder != null)
        {
            await Windows.System.Launcher.LaunchFolderAsync(folder);
        }
    }

    [RelayCommand]
    async Task ChangeCacheVideoFolder()
    {
        await _videoCacheFolderManager.ChangeVideoCacheFolder();
    }



    // アバウト
    public string VersionText { get; } 
        = $"{SystemInformation.Instance.ApplicationName} v{SystemInformation.Instance.ApplicationVersion.ToFormattedString()} {DeviceInfo.Idiom}\n{SystemInformation.Instance.OperatingSystem} {SystemInformation.Instance.OperatingSystemArchitecture} (build {SystemInformation.Instance.OperatingSystemVersion})";

    public List<LisenceItemViewModel> LisenceItems { get; private set; }

    public List<ProductViewModel> PurchaseItems { get; private set; }

    [RelayCommand]
    private Task ShowUpdateNotice() => AppUpdateNotice.ShowReleaseNotePageOnBrowserAsync();

    [RelayCommand]
    async Task ShowCheerPurchase(ProductViewModel productVM)
    {
        var result = await Models.Purchase.HohoemaPurchase.RequestPurchase(productVM.ProductListing);
        productVM.Update();
        Debug.WriteLine(result.ToString());
    }

    [RelayCommand]
    private async Task LaunchAppReview()
    {
        await Microsoft.Toolkit.Uwp.Helpers.SystemInformation.LaunchStoreForReviewAsync();
    }

    [RelayCommand]
    async Task ShowIssuesWithBrowser()
    {
        await Windows.System.Launcher.LaunchUriAsync(AppIssuePageUri);
    }

    [RelayCommand]
    void CopyVersionTextToClipboard()
    {
        ClipboardHelper.CopyToClipboard(VersionText);
    }


    #region Backup

    [RelayCommand]
    async Task ExportBackupAsync()
    {
        var picker = new FileSavePicker
        {
            DefaultFileExtension = ".json",
            SuggestedFileName = $"hohoema-backup-{DateTime.Today:yyyy-MM-dd}",
            FileTypeChoices =
                {
                    {  "Hohoema backup File", new List<string>() { ".json" } }
                }
        };
        var file = await picker.PickSaveFileAsync();
        if (file == null)
        {
            return;
        }

        try
        {            
            await _backupManager.BackupAsync(file, default);
            _notificationService.ShowLiteInAppNotification_Success("BackupSaveComplete".Translate());
        }
        catch (Exception e)
        {
            _logger.ZLogError(e, "Backup export failed");
            _notificationService.ShowLiteInAppNotification_Fail("BackupSaveFailed".Translate(e.Message));
        }
    }


    [RelayCommand]
    async Task ImportBackupAsync()
    {
        var picker = new FileOpenPicker()
        {
            ViewMode = PickerViewMode.List,
            FileTypeFilter = { ".json" }
        };
        var file = await picker.PickSingleFileAsync();
        if (file == null)
        {
            return;
        }

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
                //_backupManager.RestoreFollowingsActivitySettings,
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

            _messenger.Send<SettingsRestoredMessage>();
            _notificationService.ShowLiteInAppNotification_Success("BackupRestoreComplete".Translate());

            NGVideoOwnerUserIds = _videoFilteringRepository.GetVideoOwnerIdFilteringEntries();
            OnPropertyChanged(nameof(NGVideoOwnerUserIds));

            VideoTitleFilteringItems.Clear();
            foreach (var item in _videoFilteringRepository.GetVideoTitleFilteringEntries().Select(x =>
                new VideoFilteringTitleViewModel(x, OnRemoveVideoTitleFilterEntry, _videoFilteringRepository, this.ObserveProperty(x => x.TestText))))
            {
                VideoTitleFilteringItems.Add(item);
            }

            if (exceptions.Any())
            {
                throw new AggregateException(exceptions);
            }
        }
        catch (Exception e)
        {
            _notificationService.ShowLiteInAppNotification_Fail("BackupRestoreFailed".Translate(e.Message));
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


public sealed partial class VideoFilteringTitleViewModel 
    : ObservableObject
    , IRemovableListItem
    , IDisposable
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
        Keyword = new ReactiveProperty<string>(NGKeywordInfo.Keyword)
            .AddTo(_disposables);

        Keyword.Subscribe(x =>
            {
                NGKeywordInfo.Keyword = x;
            })
            .AddTo(_disposables);

        IsValidKeyword = Keyword
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

        IsInvalidKeyword = IsValidKeyword
            .Select(x => !x)
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
            .ToReadOnlyReactivePropertySlim()
            .AddTo(_disposables);

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
        IsActive = Models.Purchase.HohoemaPurchase.ProductIsActive(ProductListing);
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

    public string Name { get; }
    public Uri Site { get; }
    public List<string> Authors { get; }
    public string LisenceType { get; }
    public Uri LisencePageUrl { get; }
}

#endregion
