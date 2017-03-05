using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Gaming.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace NicoPlayerHohoema.Views.Controls
{
	public sealed partial class HohoemaIncrementalLoadingList : UserControl
	{
        public DataTemplate ItemTemplate { get; set; }
        public FlyoutBase ItemFlyout { get; set; }

        public bool IsFocusFirstItemEnable { get; set; } = true;

		public HohoemaIncrementalLoadingList()
		{
            this.InitializeComponent();

            if (this.ItemTemplate == null)
            {
                var defaultTemplate = Resources["DefaultListItemTemplate"] as DataTemplate;
                ItemTemplate = defaultTemplate;
            }
            this.Loaded += HohoemaIncrementalLoadingList_Loaded;
        }


        private void HohoemaIncrementalLoadingList_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.ItemFlyout != null)
            {
                HohoemaListView.RightTapped += HohoemaListView_RightTapped;

                // Note: コントローラーのMenuボタンでフライアウト表示された時に対応する
                // Menuボタン押下ではRightTappedが発火しないため
                if (UINavigationController.UINavigationControllers.Count > 0)
                {
                    this.ItemFlyout.Opened += ItemFlyout_Opened;
                    this.ItemFlyout.Closed += ItemFlyout_Closed;
                }
            }

            if (this.ItemTemplate == null)
            {
                var defaultTemplate = Resources["DefaultListItemTemplate"] as DataTemplate;
                ItemTemplate = defaultTemplate;
            }
        }

        private void ItemFlyout_Closed(object sender, object e)
        {
            FlyoutSettingDataContext(this.ItemFlyout, null);
        }

        private void ItemFlyout_Opened(object sender, object e)
        {
            var selectedItem = HohoemaListView.SelectedItem;

            if (GetFlyoutDataContext(this.ItemFlyout) != null)
            {
                return;
            }

            if (selectedItem != null)
            {
                FlyoutSettingDataContext(this.ItemFlyout, selectedItem);
            }
        }

        private void HohoemaListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var originalSource = e.OriginalSource;
            var originalDataContext = (originalSource as FrameworkElement)?.DataContext;

            if (originalDataContext == null) { return; }

            FlyoutSettingDataContext(this.ItemFlyout, originalDataContext);
        }

        private static void FlyoutSettingDataContext(FlyoutBase flyoutbase, object dataContext)
        {
            if (flyoutbase is MenuFlyout)
            {
                var menuFlyout = flyoutbase as MenuFlyout;
                foreach (var menuItem in menuFlyout.Items)
                {
                    RecurciveSettingDataContext(menuItem, dataContext);
                }
            }
            else
            {
                var flyout = flyoutbase as Flyout;
                if (flyout.Content is FrameworkElement)
                {
                    (flyout.Content as FrameworkElement).DataContext = dataContext;
                }
            }
        }

        private static object GetFlyoutDataContext(FlyoutBase flyoutbase)
        {
            if (flyoutbase is MenuFlyout)
            {
                var menuFlyout = flyoutbase as MenuFlyout;
                return menuFlyout.Items.FirstOrDefault()?.DataContext;
            }
            else
            {
                var flyout = flyoutbase as Flyout;
                if (flyout.Content is FrameworkElement)
                {
                    return (flyout.Content as FrameworkElement).DataContext;
                }
            }

            return null;
        }


        private static void RecurciveSettingDataContext(MenuFlyoutItemBase item, object dataContext)
        {
            item.DataContext = dataContext;
            if (item is MenuFlyoutSubItem)
            {
                var subItem = item as MenuFlyoutSubItem;
                foreach (var child in subItem.Items)
                {
                    RecurciveSettingDataContext(child, dataContext);
                }
            }
        }
    }


	
	
}
