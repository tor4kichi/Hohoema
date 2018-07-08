using Microsoft.Xaml.Interactivity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace NicoPlayerHohoema.Views.Behaviors
{
    public class MenuFlyoutSubItemsSetter : Behavior<MenuFlyoutSubItem>
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource)
                , typeof(IEnumerable)
                , typeof(MenuFlyoutSubItemsSetter)
                , new PropertyMetadata(Enumerable.Empty<object>())
            );

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }




        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register(
                nameof(ItemTemplate)
                , typeof(DataTemplate)
                , typeof(MenuFlyoutSubItemsSetter)
                , new PropertyMetadata(default(DataTemplate))
            );

        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }




        public static readonly DependencyProperty IsRequireInsertSeparaterBetweenDefaultItemsProperty =
            DependencyProperty.Register(
                nameof(IsRequireInsertSeparaterBetweenDefaultItems)
                , typeof(bool)
                , typeof(MenuFlyoutSubItemsSetter)
                , new PropertyMetadata(true)
            );

        /// <summary>
        /// デフォルトで配置された子アイテムとItemsSourceから生成された子アイテムの間にセパレータを配置するかのフラグです<br />
        /// </summary>
        /// <remarks>default is true</remarks>
        public bool IsRequireInsertSeparaterBetweenDefaultItems
        {
            get { return (bool)GetValue(IsRequireInsertSeparaterBetweenDefaultItemsProperty); }
            set { SetValue(IsRequireInsertSeparaterBetweenDefaultItemsProperty, value); }
        }




        public static readonly DependencyProperty IsAssignParentDataContextToTagProperty =
            DependencyProperty.Register(
                nameof(IsAssignParentDataContextToTag)
                , typeof(bool)
                , typeof(MenuFlyoutSubItemsSetter)
                , new PropertyMetadata(true)
            );

        /// <summary>
        /// 子アイテムのTagプロパティに親MenuFlyoutSubItemに設定されたDataContextを与えるかを決めるフラグです。<br />
        /// コードビハインドで生成されたDataTemplateからは親MenuFlyoutSubItemとの参照が切れてしまうことを回避する目的で利用します。
        /// </summary>
        /// <remarks>default is true</remarks>
        public bool IsAssignParentDataContextToTag
        {
            get { return (bool)GetValue(IsAssignParentDataContextToTagProperty); }
            set { SetValue(IsAssignParentDataContextToTagProperty, value); }
        }



        protected override void OnAttached()
        {
            base.OnAttached();

            this.AssociatedObject.Loaded += AssociatedObject_Loaded;
        }


        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            var subItem = sender as MenuFlyoutSubItem;
            var itemsSrouce = ItemsSource.Cast<object>().ToList();

            // サブアイテムとデフォルトで配置したサブアイテムとの間にセパレータが必要な場合は配置する
            if (IsRequireInsertSeparaterBetweenDefaultItems)
            {
                if (itemsSrouce.Count >= 1 && subItem.Items.Count >= 1)
                {
                    subItem.Items.Add(new MenuFlyoutSeparator());
                }
            }

            // ItemTemplateからMenuFlyoutItemのインスタンスを生成してMenuFlyoutSubItemに追加する
            bool isAssignParentDataContextToTag = IsAssignParentDataContextToTag;
            foreach (var item in itemsSrouce)
            {
                var flyoutItem = ItemTemplate.LoadContent() as MenuFlyoutItemBase;
                if (flyoutItem == null)
                {
                    throw new Exception("MenuFlyoutSubItemsSetter.DataTemplate is must be MenuFlyoutItemBase inherit class.");
                }
               
                flyoutItem.DataContext = item;
                if (isAssignParentDataContextToTag)
                {
                    flyoutItem.Tag = subItem.DataContext;
                }

                subItem.Items.Add(flyoutItem);
            }

            this.AssociatedObject.Loaded -= AssociatedObject_Loaded;
        }


    }


}
