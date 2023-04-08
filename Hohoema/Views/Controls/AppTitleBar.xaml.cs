#nullable enable
using Hohoema.Services;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace Hohoema.Views.Controls;

public sealed partial class AppTitleBar : UserControl
{
    static AppTitleBar()
    {
        _applicationLayoutManager = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<ApplicationLayoutManager>();
    }

    private static readonly ApplicationLayoutManager _applicationLayoutManager;

    public ApplicationLayoutManager ApplicationLayoutManager => _applicationLayoutManager;

    public AppTitleBar()
    {
        this.InitializeComponent();

        Loaded += AppTitleBar_Loaded;
        Unloaded += AppTitleBar_Unloaded;
    }

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
            ApplicationLayoutManager.ObserveProperty(x => x.NavigationViewPaneDisplayMode, isPushCurrentValueAtFirst: false).ToUnit(),
            ApplicationLayoutManager.ObserveProperty(x => x.NavigationViewIsBackButtonVisible, isPushCurrentValueAtFirst: false).ToUnit(),
            ApplicationLayoutManager.ObserveProperty(x => x.NavigationViewDisplayMode, isPushCurrentValueAtFirst: false).ToUnit()
        }
        .Merge()
        .Subscribe(_ => RefreshDisplay());
        
        RefreshDisplay();
    }


    void RefreshDisplay()
    {
        if ((
            ApplicationLayoutManager.NavigationViewPaneDisplayMode is Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode.LeftMinimal
            || ApplicationLayoutManager.NavigationViewDisplayMode is Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode.Minimal
            )
            && ApplicationLayoutManager.NavigationViewIsBackButtonVisible is not Microsoft.UI.Xaml.Controls.NavigationViewBackButtonVisible.Collapsed
            )
        {
            TitleText.Margin = new Thickness(80, 0, 0, 0);
            TitleText.FontSize = 16;
        }
        else if (ApplicationLayoutManager.NavigationViewPaneDisplayMode is Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode.LeftMinimal
            || ApplicationLayoutManager.NavigationViewDisplayMode is Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode.Minimal
            )
        {
            TitleText.Margin = new Thickness(48, 0, 0, 0);
            TitleText.FontSize = 16;
        }
        else
        {
            TitleText.Margin = new Thickness(0, 0, 0, 0);
            TitleText.FontSize = 16;
        }
    }

    public string Title
    {
        get { return (string)GetValue(TitleProperty); }
        set { SetValue(TitleProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Title.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register("Title", typeof(string), typeof(AppTitleBar), new PropertyMetadata(string.Empty));





    public string SubTitle
    {
        get { return (string)GetValue(SubTitleProperty); }
        set { SetValue(SubTitleProperty, value); }
    }

    // Using a DependencyProperty as the backing store for SubTitle.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty SubTitleProperty =
        DependencyProperty.Register("SubTitle", typeof(string), typeof(AppTitleBar), new PropertyMetadata(string.Empty));
    
}
