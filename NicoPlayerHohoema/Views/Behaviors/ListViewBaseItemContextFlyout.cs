using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Gaming.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace NicoPlayerHohoema.Views.Behaviors
{
    public class ListViewBaseItemContextFlyout : Behavior<ListViewBase>
    {
        protected override void OnAttached()
        {
            AssociatedObject.Loaded += AssociatedObject_Loaded;

            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject.ContextFlyout != null)
            {
                AssociatedObject.RightTapped -= HohoemaListView_RightTapped;
                AssociatedObject.ContextFlyout.Opening -= ContextFlyout_Opening;
                AssociatedObject.ContextFlyout.Closed -= ItemFlyout_Closed;
            }
            base.OnDetaching();
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            if (AssociatedObject.ContextFlyout != null)
            {
                AssociatedObject.RightTapped += HohoemaListView_RightTapped;

                // Note: コントローラーのMenuボタンでフライアウト表示された時に対応する
                // Menuボタン押下ではRightTappedが発火しないため
                if (UINavigationController.UINavigationControllers.Count > 0)
                {
                    AssociatedObject.ContextFlyout.Opening += ContextFlyout_Opening;
                    AssociatedObject.ContextFlyout.Closed += ItemFlyout_Closed;
                }
            }
        }

        private void ContextFlyout_Opening(object sender, object e)
        {
            var selectedItem = FocusManager.GetFocusedElement();
            if (selectedItem is FrameworkElement)
            {
                var item = AssociatedObject.ItemFromContainer((selectedItem as FrameworkElement));
                FlyoutSettingDataContext(AssociatedObject.ContextFlyout, item);
            }
        }

        private void ItemFlyout_Closed(object sender, object e)
        {
            FlyoutSettingDataContext(AssociatedObject.ContextFlyout, null);
        }

        private void HohoemaListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var originalSource = e.OriginalSource;
            var originalDataContext = (originalSource as FrameworkElement)?.DataContext;

            if (originalDataContext == null) { return; }

            FlyoutSettingDataContext(AssociatedObject.ContextFlyout, originalDataContext);
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
