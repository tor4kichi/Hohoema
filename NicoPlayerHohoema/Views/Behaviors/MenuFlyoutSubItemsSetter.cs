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
                "ItemsSource"
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
                "ItemTemplate"
                , typeof(DataTemplate)
                , typeof(MenuFlyoutSubItemsSetter)
                , new PropertyMetadata(default(DataTemplate))
            );

        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }




        public static readonly DependencyProperty IsRequireInsertSeparateBetweenDefaultItemsProperty =
            DependencyProperty.Register(
                nameof(IsRequireInsertSeparateBetweenDefaultItems)
                , typeof(bool)
                , typeof(MenuFlyoutSubItemsSetter)
                , new PropertyMetadata(true)
            );

        public bool IsRequireInsertSeparateBetweenDefaultItems
        {
            get { return (bool)GetValue(IsRequireInsertSeparateBetweenDefaultItemsProperty); }
            set { SetValue(IsRequireInsertSeparateBetweenDefaultItemsProperty, value); }
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
        public bool IsAssignParentDataContextToTag
        {
            get { return (bool)GetValue(IsAssignParentDataContextToTagProperty); }
            set { SetValue(IsAssignParentDataContextToTagProperty, value); }
        }






        bool IsInitialized = false;






        protected override void OnAttached()
        {
            base.OnAttached();

            if (!IsInitialized)
            {
                this.AssociatedObject.Loaded += AssociatedObject_Loaded;
                this.AssociatedObject.Unloaded += AssociatedObject_Unloaded;

                IsInitialized = true;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
        }

        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            var subItem = sender as MenuFlyoutSubItem;
            foreach (var removeTarget in subItem.Items.Skip(AddedItemsCount).ToList())
            {
                subItem.Items.Remove(removeTarget);
            }

            AddedSubItems.Clear();
            AddedItemsCount = 0;
        }

        int AddedItemsCount = 0;
        List<MenuFlyoutItemBase> AddedSubItems = new List<MenuFlyoutItemBase>();
        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            
            var subItem = sender as MenuFlyoutSubItem;

            var itemsSrouce = ItemsSource.Cast<object>().ToList();
            AddedItemsCount = itemsSrouce.Count;

            // サブアイテムとデフォルトで配置したサブアイテムとの間にセパレータが必要な場合は配置する
            // Unloaded時にこのセパレータも削除される（DefaultSubItemsCountの個数にセパレータが含まれないため）
            if (IsRequireInsertSeparateBetweenDefaultItems)
            {
                if (itemsSrouce.Count >= 1 && subItem.Items.Count >= 1)
                {
                    subItem.Items.Add(new MenuFlyoutSeparator());
                }
            }

            bool isAssignParentDataContextToTag = IsAssignParentDataContextToTag;
            foreach (var item in itemsSrouce)
            {
                var elem = ItemTemplate.LoadContent() as MenuFlyoutItemBase;

                elem.DataContext = item;

                if (isAssignParentDataContextToTag)
                {
                    elem.Tag = subItem.DataContext;
                }

                subItem.Items.Add(elem);
                AddedSubItems.Add(elem);
            }
        }


    }


}
