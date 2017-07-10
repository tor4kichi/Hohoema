using NicoPlayerHohoema.Models;
using Prism.Commands;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.ViewModels
{
	public sealed class AppearanceSettingsPageContentViewModel : SettingsPageContentViewModel
	{
        public HohoemaApp HohoemaApp { get; private set; }
        public static List<string> ThemeList { get; private set; } =
			Enum.GetValues(typeof(ApplicationTheme)).Cast<ApplicationTheme>()
			.Select(x => x.ToString())
			.ToList();

		public ReactiveProperty<string> SelectedApplicationTheme { get; private set; }
		public static bool ThemeChanged { get; private set; } = false;

        public ReactiveProperty<bool> IsTVModeEnable { get; private set; }
        public bool IsXbox { get; private set; }

        public ReactiveProperty<bool> IsDefaultFullScreen { get; private set; }

        public ReactiveProperty<HohoemaPageType> StartupPageType { get; private set; }

        public List<HohoemaPageType> StartupPageTypeList { get; } = new List<HohoemaPageType>()
        {
            HohoemaPageType.Search,
            HohoemaPageType.RankingCategoryList,
            HohoemaPageType.CacheManagement,
            HohoemaPageType.FeedGroupManage,
            HohoemaPageType.FollowManage,
            HohoemaPageType.UserMylist,
        };

        public AppearanceSettingsPageContentViewModel(HohoemaApp hohoemaApp, Views.Service.ToastNotificationService toastService) 
			: base("アプリのUI", HohoemaSettingsKind.Appearance)
		{
            HohoemaApp = hohoemaApp;

            var currentTheme = App.GetTheme();
			SelectedApplicationTheme = new ReactiveProperty<string>(currentTheme.ToString(), mode:ReactivePropertyMode.DistinctUntilChanged);

			SelectedApplicationTheme.Subscribe(x => 
			{
				var type = (ApplicationTheme)Enum.Parse(typeof(ApplicationTheme), x);
				App.SetTheme(type);

                // 一度だけトースト通知
                if (!ThemeChanged)
				{
					toastService.ShowText("Hohoemaを再起動するとテーマが適用されます。", "");
				}

                ThemeChanged = true;
                OnPropertyChanged(nameof(ThemeChanged));
            });

            IsTVModeEnable = HohoemaApp.UserSettings.AppearanceSettings
                .ToReactivePropertyAsSynchronized(x => x.IsForceTVModeEnable);
            IsXbox = Util.DeviceTypeHelper.IsXbox;


            
            

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

            StartupPageType = HohoemaApp.UserSettings.AppearanceSettings
                .ToReactivePropertyAsSynchronized(x => x.StartupPageType);
        }

		protected override void OnEnter(ICollection<IDisposable> focusingDisposable)
		{
            Observable.Merge(
                IsTVModeEnable.ToUnit(),
                StartupPageType.ToUnit()
                )
                .Throttle(TimeSpan.FromSeconds(1))
                .Subscribe(_ =>
                {
                    HohoemaApp.UserSettings.AppearanceSettings.Save().ConfigureAwait(false);
                })
                .AddTo(focusingDisposable);

        }

        /*
        protected override void OnLeave()
        {
            HohoemaApp.UserSettings.AppearanceSettings.Save().ConfigureAwait(false);
        }
        */


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

    }
}
