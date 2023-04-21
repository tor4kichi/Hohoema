#nullable enable
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Hohoema.Contracts.Subscriptions;
using Hohoema.Models.Subscriptions;
using Hohoema.ViewModels.Pages.Hohoema.Subscription;
using Hohoema.Views.Flyouts;
using I18NPortable;
using LiteDB;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Hohoema.Views.Pages.Hohoema.Subscription;

/// <summary>
/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
/// </summary>
public sealed partial class SubscriptionManagementPage : Page
{
    public SubscriptionManagementPage()
    {
        this.InitializeComponent();
        DataContext = _vm = Ioc.Default.GetRequiredService<SubscriptionManagementPageViewModel>();
    }

    private readonly SubscriptionManagementPageViewModel _vm;

    private void SubscriptionVideoList_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        if (sender == args.OriginalSource) { return; }

        if (sender is ListViewBase listViewBase)
        {
            if (args.OriginalSource is SelectorItem selectorItem)
            {
                selectorItem.DataContext = selectorItem.Content;
            }
            
            var subscVM = (listViewBase.DataContext as SubscriptionViewModel);
            var flyout = new VideoItemFlyout()
            {
                SourceVideoItems = new[] { subscVM.SampleVideo },
                AllowSelection = false,
            };

            flyout.ShowAt(args.OriginalSource as FrameworkElement);
            args.Handled = true;
        }
    }

    private void ListView_ItemClick(object sender, ItemClickEventArgs e)
    {

    }

    private void MenuFlyout_Opened(object sender, object e)
    {
        var menuFlyout = sender as MenuFlyout;
        var subscriptionGroupMenuItem = menuFlyout!.Items.First(x => x.Name == "SubscriptionGroupMenuSubsItem") as MenuFlyoutSubItem;

        Guard.IsNotNull(subscriptionGroupMenuItem, nameof(subscriptionGroupMenuItem));

        SubscriptionGroupRepository subscGroupRepo = Ioc.Default.GetRequiredService<SubscriptionGroupRepository>();

        var subscVM = (subscriptionGroupMenuItem.DataContext as SubscriptionViewModel)!;
        subscriptionGroupMenuItem.Items.Clear();
        subscriptionGroupMenuItem.Items.Add(new MenuFlyoutItem()
        {
            Text = "SubscGroup_CreateGroup".Translate(),
            Command = _vm.AddSubscriptionGroupCommand,
            CommandParameter = subscVM
        });
        subscriptionGroupMenuItem.Items.Add(new MenuFlyoutSeparator());
        foreach (var subscGroupVM in _vm.SubscriptionGroups)
        {
            subscriptionGroupMenuItem.Items.Add(new ToggleMenuFlyoutItem()
            {
                Text = subscGroupVM.SubscriptionGroup.Name,
                Command = subscVM.ChangeSubscGroupCommand,
                CommandParameter = subscGroupVM.SubscriptionGroup,
                IsChecked = subscVM.Group != null 
                    ? subscVM.Group.GroupId == subscGroupVM.SubscriptionGroup.GroupId 
                    : subscGroupVM.SubscriptionGroup.GroupId == SubscriptionGroupId.DefaultGroupId
            });
        }        
    }

    private void MenuFlyout_Opening(object sender, object e)
    {
        var menuFlyout = sender as MenuFlyout;
    }
}


