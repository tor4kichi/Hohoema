#nullable enable
using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.ViewModels.Pages.Hohoema.Subscription;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Hohoema.Views.Pages.Hohoema.Subscription;

/// <summary>
/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
/// </summary>
public sealed partial class SubscVideoListPage : Page
{
    public SubscVideoListPage()
    {
        this.InitializeComponent();
        DataContext = _vm = Ioc.Default.GetRequiredService<SubscVideoListPageViewModel>();
    }

    private readonly SubscVideoListPageViewModel _vm;
}


public sealed class SubscVideoListItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? SubscListItem { get; set; }
    public DataTemplate? Separator { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        if (item is ViewModels.Pages.Hohoema.Subscription.SubscVideoListItemViewModel)
        {
            return SubscListItem ?? throw new InvalidOperationException();
        }
        else if (item is SubscVideoSeparatorListItemViewModel)
        {
            return Separator ?? throw new InvalidOperationException();
        }

        return base.SelectTemplateCore(item);
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return this.SelectTemplateCore(item);
    }
}