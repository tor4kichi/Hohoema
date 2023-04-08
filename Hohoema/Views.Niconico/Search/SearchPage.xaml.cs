using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Hohoema.ViewModels.Pages.Niconico.Search;
using Hohoema.ViewModels.Pages.Niconico.VideoRanking;
using Hohoema.Contracts.Services.Navigations;
using CommunityToolkit.Mvvm.DependencyInjection;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Hohoema.Views.Pages.Niconico.Search
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class SearchPage : Page
    {
        public static INavigationService ContentNavigationService { get; private set; }

        public SearchPage()
        {
            this.InitializeComponent();

            ContentNavigationService = NavigationService.Create(SearchResultFrame);
            DataContext = _vm = Ioc.Default.GetRequiredService<SearchPageViewModel>();

            Loaded += AppTitleBar_Loaded;
            Unloaded += AppTitleBar_Unloaded;
        }

        private readonly SearchPageViewModel _vm;
        IDisposable _layoutChangeMonitorDisposer;


        private void AppTitleBar_Unloaded(object sender, RoutedEventArgs e)
        {
            _layoutChangeMonitorDisposer?.Dispose();
            _layoutChangeMonitorDisposer = null;
        }

        private void AppTitleBar_Loaded(object sender, RoutedEventArgs e)
        {
            _layoutChangeMonitorDisposer = new[]
            {
                _vm.ApplicationLayoutManager.ObserveProperty(x => x.NavigationViewPaneDisplayMode, isPushCurrentValueAtFirst: false).ToUnit(),
                _vm.ApplicationLayoutManager.ObserveProperty(x => x.NavigationViewIsBackButtonVisible, isPushCurrentValueAtFirst: false).ToUnit(),
                _vm.ApplicationLayoutManager.ObserveProperty(x => x.NavigationViewDisplayMode, isPushCurrentValueAtFirst: false).ToUnit()
            }
            .Merge()
            .Subscribe(_ => RefreshDisplay());

            RefreshDisplay();
        }


        void RefreshDisplay()
        {
            if ((
                _vm.ApplicationLayoutManager.NavigationViewPaneDisplayMode is Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode.LeftMinimal
                || _vm.ApplicationLayoutManager.NavigationViewDisplayMode is Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode.Minimal
                )
                && _vm.ApplicationLayoutManager.NavigationViewIsBackButtonVisible is not Microsoft.UI.Xaml.Controls.NavigationViewBackButtonVisible.Collapsed
                )
            {
                SearchUIContainer.Margin = new Thickness(96, 0, 0, 0);
            }
            else if (_vm.ApplicationLayoutManager.NavigationViewPaneDisplayMode is Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode.LeftMinimal
                || _vm.ApplicationLayoutManager.NavigationViewDisplayMode is Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode.Minimal
                )
            {
                SearchUIContainer.Margin = new Thickness(48, 0, 0, 0);
            }
            else
            {
                SearchUIContainer.Margin = new Thickness(0, 0, 0, 0);
            }
        }        
    }
}
