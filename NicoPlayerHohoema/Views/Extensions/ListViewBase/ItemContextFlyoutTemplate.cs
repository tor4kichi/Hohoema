using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace NicoPlayerHohoema.Views.Extensions
{
    /*
     * ■ 基本的な使い方
     * 
     * ItemContextFlyoutTemplate または ItemContextFlyoutTemplateSelector を設定する
     * 
     * 例）
     * xmlns:myExtensions="using:NicoPlayerHohoema.Views.Extensions"
     * myExtensions:ListViewBase.ItemContextFlyoutTemplate="{StaticResource VideoListItemFlyoutTemplate}"
     * 
     * 
     * ■ DataContext伝搬に関する補助機能
     * 
     * コードビハインドで生成したFlyoutはVisualTreeを辿って
     * 親要素のDataContextを得ることが出来ないケースがあります。
     * そういった場合に対応する場合に以下のオプションが有用です。
     * 
     * 1. IsItemContextFlyoutParentDataContextToTag
     *   デフォルトで有効
     *   対象のListViewBase系の要素から得られるDataContextを
     *   Flyout/MenuFlyoutのコンテンツ部分のTagに与えるかどうかを指定します
     *   ただし、ItemContextFlyoutCustomObjectToTagが設定されている場合、無視されます
     * 
     * 2. ItemContextFlyoutCustomObjectToTag
     *   Flyout/MenuFlyoutのコンテンツ部分のTagに任意のobjectを与えたい場合に使用します
     *   
     */


    public partial class ListViewBase
    {
        
        public static readonly DependencyProperty IsItemContextFlyoutParentDataContextToTagProperty =
            DependencyProperty.RegisterAttached(
                "IsItemContextFlyoutParentDataContextToTag",
                typeof(bool),
                typeof(ListViewBase),
                new PropertyMetadata(true)
            );

        public static void SetIsItemContextFlyoutParentDataContextToTag(UIElement element, bool value)
        {
            element.SetValue(IsItemContextFlyoutParentDataContextToTagProperty, value);
        }
        public static bool GetIsItemContextFlyoutParentDataContextToTag(UIElement element)
        {
            return (bool)element.GetValue(IsItemContextFlyoutParentDataContextToTagProperty);
        }


        public static readonly DependencyProperty ItemContextFlyoutCustomObjectToTagProperty =
            DependencyProperty.RegisterAttached(
                "ItemContextFlyoutCustomObjectToTag",
                typeof(object),
                typeof(ListViewBase),
                new PropertyMetadata(default(object))
            );

        public static void SetItemContextFlyoutCustomObjectToTag(UIElement element, object value)
        {
            element.SetValue(ItemContextFlyoutCustomObjectToTagProperty, value);
        }
        public static object GetItemContextFlyoutCustomObjectToTag(UIElement element)
        {
            return (object)element.GetValue(ItemContextFlyoutCustomObjectToTagProperty);
        }




        public static readonly DependencyProperty ItemContextFlyoutTemplateProperty =
            DependencyProperty.RegisterAttached(
                "ItemContextFlyoutTemplate",
                typeof(DataTemplate),
                typeof(ListViewBase),
                new PropertyMetadata(default(DataTemplate), ItemContextFlyoutTemplatePropertyChanged)
            );

        public static void SetItemContextFlyoutTemplate(UIElement element, DataTemplate value)
        {
            element.SetValue(ItemContextFlyoutTemplateProperty, value);
        }
        public static DataTemplate GetItemContextFlyoutTemplate(UIElement element)
        {
            return (DataTemplate)element.GetValue(ItemContextFlyoutTemplateProperty);
        }





        public static readonly DependencyProperty ItemContextFlyoutTemplateSelectorProperty =
            DependencyProperty.RegisterAttached(
                "ItemContextFlyoutTemplateSelector",
                typeof(DataTemplateSelector),
                typeof(ListViewBase),
                new PropertyMetadata(default(DataTemplateSelector), ItemContextFlyoutTemplateSelectorPropertyChanged)
            );

        public static void SetItemContextFlyoutTemplateSelector(UIElement element, DataTemplateSelector value)
        {
            element.SetValue(ItemContextFlyoutTemplateSelectorProperty, value);
        }
        public static DataTemplateSelector GetItemContextFlyoutTemplateSelector(UIElement element)
        {
            return (DataTemplateSelector)element.GetValue(ItemContextFlyoutTemplateSelectorProperty);
        }



        private static void ItemContextFlyoutTemplatePropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            if (s is Windows.UI.Xaml.Controls.ListViewBase target && e.NewValue is DataTemplate template)
            {
                target.ContainerContentChanging += (sender, args) =>
                {
                    if (args.InRecycleQueue) { return; }

                    var customObjectToTag = GetItemContextFlyoutCustomObjectToTag(target) 
                    ?? (GetIsItemContextFlyoutParentDataContextToTag(target) ? target.DataContext : null);

                    var flyout = template.LoadContent() as FlyoutBase;
                    args.ItemContainer.ContextRequested += (_sender, _args) => OnListViewBaseItemContextRequested(
                        (__d, __f) => flyout, _args, customObjectToTag
                        );
                };
            }            
        }


        private static void ItemContextFlyoutTemplateSelectorPropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            if (s is Windows.UI.Xaml.Controls.ListViewBase target && e.NewValue is DataTemplateSelector templateSelector)
            {
                target.ContainerContentChanging += (sender, args) =>
                {
                    if (args.InRecycleQueue) { return; }

                    var customObjectToTag = GetItemContextFlyoutCustomObjectToTag(target)
                    ?? (GetIsItemContextFlyoutParentDataContextToTag(target) ? target.DataContext : null);

                    Func<object, DependencyObject, FlyoutBase> flyoutSelector = (dataContext, placementTarget) => 
                    {
                        var dataTemplate = templateSelector.SelectTemplate(dataContext, placementTarget);
                        return dataTemplate.LoadContent() as FlyoutBase;
                    };
                    args.ItemContainer.ContextRequested += (_sender, _args) => OnListViewBaseItemContextRequested(
                        flyoutSelector, _args, customObjectToTag
                        );
                };
            }
        }


        private static void OnListViewBaseItemContextRequested(
            Func<object, DependencyObject, FlyoutBase> flyoutSelector, 
            ContextRequestedEventArgs args,
            object customObjectToTag
            )
        {
            FrameworkElement flyoutPlacementTarget = null;
            object dataContext = null;

            // カーソル操作（コントローラー操作など）によるContextRequestに対応する
            // カーソル操作時はListViewItem/GridViewItemの一つ上階層にあるItemContainerがargs.OriginalSourceに渡される
            if (args.OriginalSource is ContentControl contentControl)
            {
                flyoutPlacementTarget = contentControl;
                dataContext = contentControl.Content;
            }
            else if (args.OriginalSource is FrameworkElement fe)
            {
                flyoutPlacementTarget = fe;
                dataContext = fe?.DataContext;
            }

            var flyout = flyoutSelector?.Invoke(dataContext, flyoutPlacementTarget);

            if (flyout != null)
            {
                FlyoutSettingDataContext(flyout, dataContext, customObjectToTag);

                flyout.ShowAt(flyoutPlacementTarget);
            }
        }


        private static void FlyoutSettingDataContext(FlyoutBase flyoutbase, object dataContext, object customObjectToTag)
        {
            if (flyoutbase is MenuFlyout menuFlyout)
            {
                foreach (var menuItem in menuFlyout.Items)
                {
                    RecurciveSettingDataContext(menuItem, dataContext, customObjectToTag);
                }
            }
            else if (flyoutbase is Flyout flyout)
            {
                if (flyout.Content is FrameworkElement fe)
                {
                    fe.DataContext = dataContext;
                    fe.Tag = customObjectToTag;
                }
            }
        }

        private static void RecurciveSettingDataContext(MenuFlyoutItemBase item, object dataContext, object customObjectToTag)
        {
            item.DataContext = dataContext;
            item.Tag = customObjectToTag;
            if (item is MenuFlyoutSubItem subItem)
            {
                foreach (var child in subItem.Items)
                {
                    RecurciveSettingDataContext(child, dataContext, customObjectToTag);
                }
            }
        }
    }
}
