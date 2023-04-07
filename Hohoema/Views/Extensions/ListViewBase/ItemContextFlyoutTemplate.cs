using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace Hohoema.Views.Extensions
{
    /*
     * ■ 基本的な使い方
     * 
     * ItemContextFlyoutTemplate または ItemContextFlyoutTemplateSelector を設定する
     * 
     * 例）
     * xmlns:myExtensions="using:Hohoema.Views.Extensions"
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


        private static void ItemContextFlyoutTemplatePropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            if (s is Windows.UI.Xaml.Controls.ListViewBase target && e.NewValue is DataTemplate template)
            {
                target.ContainerContentChanging += (sender, args) =>
                {
                    if (args.Phase != 0) { return; }

                    args.RegisterUpdateCallback((_, __) => 
                    {
                        if (args.ItemContainer is FrameworkElement fe)
                        {
                            // Note: コントローラー操作含めたContextFlyoutを仕掛ける
                            // fe = ListViewItem(GridViewItem)に対してContextFlyoutを仕掛けるだけでは
                            // DataContextにViewModelが渡らないため不十分。
                            // contentPresenterのContextFlyoutを利用した場合は、
                            // コントローラー操作（カーソル操作）時のフライアウトメニュー表示が反応しないため要件を満たさない
                            var contentPresenter = fe.FindFirstChild<FrameworkElement>();
                            var flyout = template.LoadContent() as Windows.UI.Xaml.Controls.Primitives.FlyoutBase;
                            fe.ContextFlyout = flyout;
                            fe.DataContext = contentPresenter.DataContext;
                            var customContext = GetItemContextFlyoutCustomObjectToTag(target);
                            if (customContext != null)
                            {
                                FlyoutOpenerDataContextSetToTag(flyout, customContext);
                            }
                        }

                    });
                };
            }            
        }


        private static void ItemContextFlyoutTemplateSelectorPropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            if (s is Windows.UI.Xaml.Controls.ListViewBase target && e.NewValue is DataTemplateSelector templateSelector)
            {
                target.ContainerContentChanging += (sender, args) =>
                {
                    if (args.Phase != 0) { return; }

                    args.RegisterUpdateCallback((_, __) =>
                    {
                        if (args.ItemContainer is FrameworkElement fe)
                        {
                            var dataContext = args.ItemContainer.Content;
                            var dataTemplate = templateSelector.SelectTemplate(dataContext, args.ItemContainer);
                            var flyout = dataTemplate.LoadContent() as Windows.UI.Xaml.Controls.Primitives.FlyoutBase;
                            var contentPresenter = fe.FindFirstChild<FrameworkElement>();
                            fe.ContextFlyout = flyout;
                            fe.DataContext = contentPresenter.DataContext ?? dataContext;
                            var customContext = GetItemContextFlyoutCustomObjectToTag(target);
                            if (customContext != null)
                            {
                                FlyoutOpenerDataContextSetToTag(flyout, customContext);
                            }
                        }

                    });
                };
            }
        }

        


       

        private static void FlyoutOpenerDataContextSetToTag(Windows.UI.Xaml.Controls.Primitives.FlyoutBase flyoutbase, object dataContextToTag)
        {
            if (flyoutbase is MenuFlyout menuFlyout)
            {
                foreach (var menuItem in menuFlyout.Items)
                {
                    RecurciveSettingDataContext(menuItem, dataContextToTag);
                }
            }
            else if (flyoutbase is Flyout flyout)
            {
                if (flyout.Content is FrameworkElement fe)
                {
//                    fe.DataContext = dataContext;
                    fe.Tag = dataContextToTag;
                }
            }
        }

        private static void RecurciveSettingDataContext(MenuFlyoutItemBase item, object dataContextToTag)
        {
            item.Tag = dataContextToTag;
            if (item is MenuFlyoutSubItem subItem)
            {
                foreach (var child in subItem.Items)
                {
                    RecurciveSettingDataContext(child, dataContextToTag);
                }
            }
        }

    
    }
}
