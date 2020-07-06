using Microsoft.Services.Store.Engagement;
using Hohoema.Models;
using Hohoema.Services;
using Hohoema.ViewModels.Pages;
using Hohoema.UseCase;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.Store;
using Windows.Storage;
using Windows.System;
using Windows.UI;
using Windows.UI.StartScreen;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Hohoema.Models.Pages;
using Hohoema.Models.Repository.App;
using Hohoema.Models.Repository.Niconico.NicoVideo.Ranking;
using Hohoema.Models.Repository.NicoRepo;
using Hohoema.Models.Repository.VideoCache;
using Hohoema.Models.Repository.Playlist;
using System.Collections.ObjectModel;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Helpers;
using Uno.Extensions;

namespace Hohoema.ViewModels
{
    public class SettingsPageViewModel : HohoemaViewModelBase, INavigatedAwareAsync
	{
        private static Uri AppIssuePageUri = new Uri("https://github.com/tor4kichi/Hohoema/issues");


        public SettingsPageViewModel(
            PageManager pageManager,
            LatestUpdateNoticeDialogService latestUpdateNoticeDialogService,
            PlayerSettingsRepository playerSettings,
            VideoListFilterSettings videoListFilterSettings,
            RankingSettingsRepository rankingSettings,
            NicoRepoSettingsRepository nicoRepoSettingsRepository,
            AppearanceSettingsRepository appearanceSettings,
            CacheSettingsRepository cacheSettings,
            ApplicationLayoutManager applicationLayoutManager
            )
        {
            _latestUpdateNoticeDialogService = latestUpdateNoticeDialogService;
            _playerSettings = playerSettings;
            _videoListFilterSettings = videoListFilterSettings;
            _rankingSettings = rankingSettings;
            _nicoRepoSettingsRepository = nicoRepoSettingsRepository;
            _appearanceSettings = appearanceSettings;
            _cacheSettings = cacheSettings;
            ApplicationLayoutManager = applicationLayoutManager;

            // NG Video Owner User Id
            NGVideoOwnerUserIdEnable = _videoListFilterSettings.ToReactivePropertyAsSynchronized(x => x.NGVideoOwnerUserIdEnable);
            NGVideoOwnerUserIds = new ObservableCollection<Models.Repository.App.UserIdInfo>();

            OpenUserPageCommand = new DelegateCommand<Models.Repository.App.UserIdInfo>(userIdInfo =>
            {
                pageManager.OpenPageWithId(HohoemaPageType.UserInfo, userIdInfo.UserId);
            });

            // NG Keyword on Video Title
            NGKeywordList = new ObservableCollection<NGKeywordViewModel>();
            NGVideoTitleKeywordEnable = _videoListFilterSettings.ToReactivePropertyAsSynchronized(x => x.NGVideoTitleKeywordEnable);

            // アピアランス

            var currentTheme = App.GetTheme();
            SelectedTheme = new ReactiveProperty<ElementTheme>(_appearanceSettings.AppTheme, mode: ReactivePropertyMode.DistinctUntilChanged);

            SelectedTheme.Subscribe(theme =>
            {
                _appearanceSettings.AppTheme = theme;

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
            });



            IsDefaultFullScreen = new ReactiveProperty<bool>(ApplicationView.PreferredLaunchWindowingMode == ApplicationViewWindowingMode.FullScreen);
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
            });

            StartupPageType = _appearanceSettings
                .ToReactivePropertyAsSynchronized(x => x.StartupPageType);

            _appearanceSettings.ObserveProperty(x => x.Locale, isPushCurrentValueAtFirst: false).Subscribe(locale => 
            {
                I18NPortable.I18N.Current.Locale = locale;
            });

            // キャッシュ
            DefaultCacheQuality = _cacheSettings.ToReactivePropertyAsSynchronized(x => x.DefaultCacheQuality);
            
            // シェア
            IsLoginTwitter = new ReactiveProperty<bool>(/*TwitterHelper.IsLoggedIn*/ false);
            TwitterAccountScreenName = new ReactiveProperty<string>(/*TwitterHelper.TwitterUser?.ScreenName ?? ""*/);


            // アバウト
            var version = Windows.ApplicationModel.Package.Current.Id.Version;
#if DEBUG
            VersionText = $"{version.Major}.{version.Minor}.{version.Build} DEBUG";
#else
            VersionText = $"{version.Major}.{version.Minor}.{version.Build}";
#endif


            var dispatcher = Window.Current.CoreWindow.Dispatcher;
            LisenceSummary.Load()
                .ContinueWith(async prevResult =>
                {
                    await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        var lisenceSummary = prevResult.Result;

                        LisenceItems = lisenceSummary.Items
                            .OrderBy(x => x.Name)
                            .Select(x => new LisenceItemViewModel(x))
                            .ToList();
                        RaisePropertyChanged(nameof(LisenceItems));
                    });
                });


            IsDebugModeEnabled = new ReactiveProperty<bool>((App.Current as App).IsDebugModeEnabled, mode: ReactivePropertyMode.DistinctUntilChanged);
            IsDebugModeEnabled.Subscribe(isEnabled =>
            {
                (App.Current as App).IsDebugModeEnabled = isEnabled;
            })
            .AddTo(_CompositeDisposable);
        }

        public ApplicationLayoutManager ApplicationLayoutManager { get; }


        // フィルタ
        public ReactiveProperty<bool> NGVideoOwnerUserIdEnable { get; private set; }
        public ObservableCollection<Models.Repository.App.UserIdInfo> NGVideoOwnerUserIds { get; private set; }
        public DelegateCommand<Models.Repository.App.UserIdInfo> OpenUserPageCommand { get; }


        public ReactiveProperty<bool> NGVideoTitleKeywordEnable { get; private set; }

        public ObservableCollection<NGKeywordViewModel> NGKeywordList { get; }
        
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
            HohoemaPageType.FeedGroupManage,
            HohoemaPageType.FollowManage,
            HohoemaPageType.UserMylist,
            HohoemaPageType.Timeshift,
        };

        private DelegateCommand _ToggleFullScreenCommand;
        public DelegateCommand ToggleFullScreenCommand
        {
            get
            {
                return _ToggleFullScreenCommand
                    ?? (_ToggleFullScreenCommand = new DelegateCommand(() =>
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
            NicoVideoQuality.Dmc_SuperHigh,
            NicoVideoQuality.Dmc_High,
            NicoVideoQuality.Dmc_Midium,
            NicoVideoQuality.Dmc_Low,
            NicoVideoQuality.Dmc_Mobile,
        };

        public ReactiveProperty<bool> IsAllowDownloadOnMeteredNetwork { get; private set; }

        public ReactiveProperty<NicoVideoQuality> DefaultCacheQualityOnMeteredNetwork { get; private set; }


        // Note: 従量課金時のキャッシュ画質指定は「デフォルトのキャッシュ画質の設定を継承」できるようにしたい
        // 本来的には「キャッシュ用のNicoVIdeoQuality」として別のEnumを用意して対応すべきだが
        // 
        public List<NicoVideoQuality> AvairableDefaultCacheQualitiesOnMeteredNetwork { get; } = new List<NicoVideoQuality>()
        {
            NicoVideoQuality.Unknown,
            NicoVideoQuality.Dmc_SuperHigh,
            NicoVideoQuality.Dmc_High,
            NicoVideoQuality.Dmc_Midium,
            NicoVideoQuality.Dmc_Low,
            NicoVideoQuality.Dmc_Mobile,
        };


        // シェア
        public ReactiveProperty<bool> IsLoginTwitter { get; private set; }
        public ReactiveProperty<string> TwitterAccountScreenName { get; private set; }
        private DelegateCommand _LogInToTwitterCommand;
        public DelegateCommand LogInToTwitterCommand
        {
            get
            {
                return _LogInToTwitterCommand
                    ?? (_LogInToTwitterCommand = new DelegateCommand(async () =>
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

        private DelegateCommand _LogoutTwitterCommand;
        public DelegateCommand LogoutTwitterCommand
        {
            get
            {
                return _LogoutTwitterCommand
                    ?? (_LogoutTwitterCommand = new DelegateCommand(() =>
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

        private DelegateCommand _ShowUpdateNoticeCommand;
        public DelegateCommand ShowUpdateNoticeCommand
        {
            get
            {
                return _ShowUpdateNoticeCommand
                    ?? (_ShowUpdateNoticeCommand = new DelegateCommand(async () =>
                    {
                        await _latestUpdateNoticeDialogService.ShowLatestUpdateNotice();
                    }));
            }
        }

        private DelegateCommand<ProductViewModel> _ShowCheerPurchaseCommand;
        public DelegateCommand<ProductViewModel> ShowCheerPurchaseCommand
        {
            get
            {
                return _ShowCheerPurchaseCommand
                    ?? (_ShowCheerPurchaseCommand = new DelegateCommand<ProductViewModel>(async (product) =>
                    {
                        var result = await Models.Purchase.HohoemaPurchase.RequestPurchase(product.ProductListing);

                        product.Update();

                        Debug.WriteLine(result.ToString());
                    }));
            }
        }


        private DelegateCommand _LaunchAppReviewCommand;
        public DelegateCommand LaunchAppReviewCommand
        {
            get
            {
                return _LaunchAppReviewCommand
                    ?? (_LaunchAppReviewCommand = new DelegateCommand(async () =>
                    {
                        await Microsoft.Toolkit.Uwp.Helpers.SystemInformation.LaunchStoreForReviewAsync();
                        //await Launcher.LaunchUriAsync(AppReviewUri);
                    }));
            }
        }

        private DelegateCommand _LaunchFeedbackHubCommand;
        public DelegateCommand LaunchFeedbackHubCommand
        {
            get
            {
                return _LaunchFeedbackHubCommand
                    ?? (_LaunchFeedbackHubCommand = new DelegateCommand(async () =>
                    {
                        if (IsSupportedFeedbackHub)
                        {
                            await StoreServicesFeedbackLauncher.GetDefault().LaunchAsync();
                        }
                    }));
            }
        }

        private DelegateCommand _ShowIssuesWithBrowserCommand;
        public DelegateCommand ShowIssuesWithBrowserCommand
        {
            get
            {
                return _ShowIssuesWithBrowserCommand
                    ?? (_ShowIssuesWithBrowserCommand = new DelegateCommand(async () =>
                    {
                        await Launcher.LaunchUriAsync(AppIssuePageUri);
                    }));
            }
        }


        public ReactiveProperty<bool> IsDebugModeEnabled { get; }


        private bool _IsExistErrorFilesFolder;
        public bool IsExistErrorFilesFolder
        {
            get { return _IsExistErrorFilesFolder; }
            set { SetProperty(ref _IsExistErrorFilesFolder, value); }
        }

        private DelegateCommand _ShowErrorFilesFolderCommand;
        public DelegateCommand ShowErrorFilesFolderCommand
        {
            get
            {
                return _ShowErrorFilesFolderCommand
                    ?? (_ShowErrorFilesFolderCommand = new DelegateCommand(async () =>
                    {
                        await (App.Current as App).ShowErrorLogFolder();
                    }));
            }
        }


        private DelegateCommand _CopyVersionTextToClipboardCommand;
        public DelegateCommand CopyVersionTextToClipboardCommand
        {
            get
            {
                return _CopyVersionTextToClipboardCommand
                    ?? (_CopyVersionTextToClipboardCommand = new DelegateCommand(() =>
                    {
                        ClipboardHelper.CopyToClipboard(CurrentVersion.ToString());
                    }));
            }
        }

        public bool IsSupportedFeedbackHub { get; } = StoreServicesFeedbackLauncher.IsSupported();




        // セカンダリタイル関連

        private DelegateCommand _AddTransparencySecondaryTile;
        private readonly LatestUpdateNoticeDialogService _latestUpdateNoticeDialogService;
        private readonly PlayerSettingsRepository _playerSettings;
        private readonly VideoListFilterSettings _videoListFilterSettings;
        private readonly RankingSettingsRepository _rankingSettings;
        private readonly NicoRepoSettingsRepository _nicoRepoSettingsRepository;
        private readonly AppearanceSettingsRepository _appearanceSettings;
        private readonly CacheSettingsRepository _cacheSettings;

        public DelegateCommand AddTransparencySecondaryTile
        {
            get
            {
                return _AddTransparencySecondaryTile
                    ?? (_AddTransparencySecondaryTile = new DelegateCommand(async () =>
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
                        secondaryTile.VisualElements.Square30x30Logo = new Uri("ms-appx:///Assets/Square30x30Logo.scale-100.png");
                        secondaryTile.VisualElements.ShowNameOnSquare150x150Logo = true;
                        secondaryTile.VisualElements.ShowNameOnSquare310x310Logo = true;
                        secondaryTile.VisualElements.ShowNameOnWide310x150Logo = true;

                        if (false == await secondaryTile.RequestCreateAsync())
                        {
                            throw new Exception("Failed secondary tile creation.");
                        }
                    }
                    ));
            }
        }


        public async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            var keywords = _videoListFilterSettings.GetAllNGVideoTitleKeyword();

            NGKeywordList.Clear();
            NGKeywordList.AddRange(keywords.Select(x => new NGKeywordViewModel(x, _videoListFilterSettings, this)));

            try
            {
                var listing = await Models.Purchase.HohoemaPurchase.GetAvailableCheersAddOn();
                PurchaseItems = listing.ProductListings.Select(x => new ProductViewModel(x.Value)).ToList();
                RaisePropertyChanged(nameof(PurchaseItems));
            }
            catch { }

            try
            {
                IsExistErrorFilesFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("error") != null;
            }
            catch { }

        }

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
            base.OnNavigatedFrom(parameters);
        }
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

			RemoveCommand = new DelegateCommand(() => 
			{
				onRemovedAction(Source);
			});
		}


		public ICommand RemoveCommand { get; private set; }
	}


	public class NGKeywordViewModel : IRemovableListItem, IDisposable
    {
		public NGKeywordViewModel(Models.Repository.App.NGKeyword ngKeyword, VideoListFilterSettings videoListFilterSettings, SettingsPageViewModel settingsPageViewModel)
		{
			NGKeywordInfo = ngKeyword;
            _videoListFilterSettings = videoListFilterSettings;
            _settingsPageViewModel = settingsPageViewModel;

            Label = NGKeywordInfo.Keyword;
            TestText = new ReactiveProperty<string>(NGKeywordInfo.TestText);
			Keyword = new ReactiveProperty<string>(NGKeywordInfo.Keyword);

			TestText.Subscribe(x => 
			{
				NGKeywordInfo.TestText = x;
			});

			Keyword.Subscribe(x =>
			{
				NGKeywordInfo.Keyword = x;
			});

			IsValidKeyword =
				Observable.CombineLatest(
					TestText,
					Keyword
					)
					.Where(x => x[0].Length > 0)
					.Select(x =>
					{
						var result = -1 != TestText.Value.IndexOf(Keyword.Value);
						return result;
					})
					.ToReactiveProperty();

			IsInvalidKeyword = IsValidKeyword.Select(x => !x)
				.ToReactiveProperty();

            RemoveCommand = new DelegateCommand(() => 
			{
                _videoListFilterSettings.RemoveNgVideoTitleKeyword(NGKeywordInfo);
                _settingsPageViewModel.NGKeywordList.Remove(this);
            });
        }

		public void Dispose()
		{
			TestText?.Dispose();
			Keyword?.Dispose();

			IsValidKeyword?.Dispose();
			IsInvalidKeyword?.Dispose();
			
		}



		public ReactiveProperty<string> TestText { get; private set; }
		public ReactiveProperty<string> Keyword { get; private set; }

		public ReactiveProperty<bool> IsValidKeyword { get; private set; }
		public ReactiveProperty<bool> IsInvalidKeyword { get; private set; }

        public string Label { get; private set; }
		public ICommand RemoveCommand { get; private set; }
		
		public Models.Repository.App.NGKeyword NGKeywordInfo { get; }

        private readonly VideoListFilterSettings _videoListFilterSettings;
        private readonly SettingsPageViewModel _settingsPageViewModel;
    }

	public interface IRemovableListItem
    {
        string Label { get; }
        ICommand RemoveCommand { get; }
    }



#region About 

    public class ProductViewModel : BindableBase
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
            LisenceType = LisenceTypeToText(item.LisenceType.Value);
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
                case Models.Helpers.LisenceType.MIT:
                    return "MIT";
                case Models.Helpers.LisenceType.MS_PL:
                    return "Microsoft Public Lisence";
                case Models.Helpers.LisenceType.Apache_v2:
                    return "Apache Lisence version 2.0";
                case Models.Helpers.LisenceType.GPL_v3:
                    return "GNU General Public License Version 3";
                case Models.Helpers.LisenceType.Simplified_BSD:
                    return "二条項BSDライセンス";
                case Models.Helpers.LisenceType.CC_BY_40:
                    return "クリエイティブ・コモンズ 表示 4.0 国際";
                case Models.Helpers.LisenceType.SIL_OFL_v1_1:
                    return "SIL OPEN FONT LICENSE Version 1.1";
                default:
                    throw new NotSupportedException(type.ToString());
            }
        }

    }

#endregion

}
