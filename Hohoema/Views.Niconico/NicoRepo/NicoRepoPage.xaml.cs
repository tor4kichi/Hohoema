#nullable enable
using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.Models.Application;
using Hohoema.Services;
using Hohoema.ViewModels.Pages.Niconico.NicoRepo;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Hohoema.Views.Pages.Niconico.NicoRepo;

/// <summary>
/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
/// </summary>
public sealed partial class NicoRepoPage : Page
{

    public double ScrollPosition
    {
        get { return (double)GetValue(ScrollPositionProperty); }
        set { SetValue(ScrollPositionProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ScrollPosition.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ScrollPositionProperty =
        DependencyProperty.Register("ScrollPosition", typeof(double), typeof(NicoRepoPage), new PropertyMetadata(0.0));


    public void ResetScrollPosition()
    {
        var scrollViweer = ItemsList.FindFirstChild<ScrollViewer>();
        scrollViweer.ChangeView(null, 0, null);
    }



    public NicoRepoPage()
    {
        this.InitializeComponent();
        DataContext = _vm = Ioc.Default.GetRequiredService<NicoRepoPageViewModel>();

        _layoutManager = Ioc.Default.GetRequiredService<ApplicationLayoutManager>();
        _appearanceSettings = Ioc.Default.GetRequiredService<AppearanceSettings>();
        Loaded += NicoRepoPage_Loaded;
    }

    private void NicoRepoPage_Loaded(object sender, RoutedEventArgs e)
    {
        if ((_layoutManager.IsMouseInteractionDefault || _layoutManager.IsTouchInteractionDefault)
             && _appearanceSettings.IsVideoListItemDoubleClickOrDoubleTapToPlayEnabled
             )
        {
            // センタークリック操作のためにItemClickは有効化しておきたい
            ItemsList.IsItemClickEnabled = true;
            ItemsList.IsDoubleTapEnabled = true;
            ItemsList.DoubleTapped += ItemsList_DoubleTapped;
            ItemsList.ItemClick -= ItemsList_ItemClick;
        }
        else
        {
            ItemsList.IsItemClickEnabled = true;
            ItemsList.IsDoubleTapEnabled = false;
            ItemsList.ItemClick += ItemsList_ItemClick;
        }
    }

    private void ItemsList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement fe
            && _vm.OpenNicoRepoItemCommand.CanExecute(fe.DataContext))
        {
            _vm.OpenNicoRepoItemCommand.Execute(fe.DataContext);
            e.Handled = true;
        }
    }

    private void ItemsList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (_vm.OpenNicoRepoItemCommand.CanExecute(e.ClickedItem))
        {
            _vm.OpenNicoRepoItemCommand.Execute(e.ClickedItem);
        }
    }

    private readonly NicoRepoPageViewModel _vm;
    private readonly ApplicationLayoutManager _layoutManager;
    private readonly AppearanceSettings _appearanceSettings;
}


public sealed class NicoRepoTimelineItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate VideoItem { get; set; }
    public DataTemplate LiveItem { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return SelectTemplateCore(item, null);
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            NicoRepoVideoTimeline => VideoItem,
            NicoRepoLiveTimeline => LiveItem,
            _ => throw new NotSupportedException(),
        };
    }
}
