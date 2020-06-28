using Microsoft.Xaml.Interactivity;
using System;
using System.Collections;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Reactive.Bindings;
using System.Reactive.Linq;
using System.Collections.Generic;

namespace Hohoema.Views.Behaviors
{
    public sealed class MenuFlyoutItemsSetter : Behavior<MenuFlyout>
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource)
                , typeof(IEnumerable)
                , typeof(MenuFlyoutItemsSetter)
                , new PropertyMetadata(Enumerable.Empty<object>(), OnItemsSourcePropertyChanged)
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
                , typeof(MenuFlyoutItemsSetter)
                , new PropertyMetadata(default(DataTemplate), OnMenuFlyoutItemsSetterPropertyChanged)
            );

        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }





        public static readonly DependencyProperty CustomObjectToTagProperty =
            DependencyProperty.Register(
                nameof(CustomObjectToTag)
                , typeof(object)
                , typeof(MenuFlyoutItemsSetter)
                , new PropertyMetadata(default(object), OnMenuFlyoutItemsSetterPropertyChanged)
            );

        public object CustomObjectToTag
        {
            get { return (object)GetValue(CustomObjectToTagProperty); }
            set { SetValue(CustomObjectToTagProperty, value); }
        }



        public static readonly DependencyProperty IsRequireInsertSeparaterBetweenDefaultItemsProperty =
            DependencyProperty.Register(
                nameof(IsRequireInsertSeparaterBetweenDefaultItems)
                , typeof(bool)
                , typeof(MenuFlyoutItemsSetter)
                , new PropertyMetadata(true, OnMenuFlyoutItemsSetterPropertyChanged)
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





        #region Event Handler

        public static void OnMenuFlyoutItemsSetterPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            MenuFlyoutItemsSetter source = (MenuFlyoutItemsSetter)sender;

            source.IsRequireReset = true; 
        }

        public static void OnItemsSourcePropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            MenuFlyoutItemsSetter source = (MenuFlyoutItemsSetter)sender;

            if (args.OldValue is System.Collections.Specialized.INotifyCollectionChanged oldNotifyCollectionChanged)
            {
                oldNotifyCollectionChanged.CollectionChanged -= source.OldNotifyCollectionChanged_CollectionChanged;
            }

            if (args.NewValue is System.Collections.Specialized.INotifyCollectionChanged newNotifyCollectionChanged)
            {
                newNotifyCollectionChanged.CollectionChanged += source.OldNotifyCollectionChanged_CollectionChanged;
            }

            source.IsRequireReset = true;
        }

        private void OldNotifyCollectionChanged_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            IsRequireReset = true;
        }

        #endregion




        bool IsRequireReset { get; set; } = true;

        List<MenuFlyoutItemBase> _addedMenuFlyoutItems = new List<MenuFlyoutItemBase>();


        protected override void OnAttached()
        {
            base.OnAttached();

            this.AssociatedObject.Opening += AssociatedObject_Opening;
        }

        protected override void OnDetaching()
        {
            this.AssociatedObject.Opening -= AssociatedObject_Opening;

            if (ItemsSource is System.Collections.Specialized.INotifyCollectionChanged newNotifyCollectionChanged)
            {
                newNotifyCollectionChanged.CollectionChanged -= OldNotifyCollectionChanged_CollectionChanged;
            }

            base.OnDetaching();
        }


        private void AssociatedObject_Opening(object sender, object e)
        {
            if (IsRequireReset)
            {
                ResetItems();
            }
        }


        private void ResetItems()
        {
            var menuFlyout = AssociatedObject;
            var itemsSrouce = ItemsSource.Cast<object>().ToList();
            var customObjectToTag = CustomObjectToTag;


            // 前回追加分のメニューアイテムを削除
            foreach (var oldItem in _addedMenuFlyoutItems)
            {
                menuFlyout.Items.Remove(oldItem);
            }

            _addedMenuFlyoutItems.Clear();


            // これから配置するアイテムとの間にセパレータが必要な場合
            if (IsRequireInsertSeparaterBetweenDefaultItems)
            {
                if (itemsSrouce.Count >= 1 && menuFlyout.Items.Count >= 1)
                {
                    var separator = new MenuFlyoutSeparator();
                    menuFlyout.Items.Add(separator);
                    _addedMenuFlyoutItems.Add(separator);
                }
            }

            // ItemTemplateからMenuFlyoutItem系のインスタンスを生成してMenuFlyoutSubItemに追加する
            foreach (var item in itemsSrouce)
            {
                var templatedContent = ItemTemplate.LoadContent();
                if (templatedContent is MenuFlyoutItemBase flyoutItem)
                {
                    flyoutItem.DataContext = item;
                    flyoutItem.Tag = customObjectToTag ?? flyoutItem.Tag;

                    menuFlyout.Items.Add(flyoutItem);
                    _addedMenuFlyoutItems.Add(flyoutItem);
                }
#if DEBUG
                else
                {
                    throw new Exception($"{nameof(MenuFlyoutItemsSetter)}.{nameof(ItemTemplate)} is must be MenuFlyoutItemBase inherit class.");
                }
#endif
            }
        }


    }
}
