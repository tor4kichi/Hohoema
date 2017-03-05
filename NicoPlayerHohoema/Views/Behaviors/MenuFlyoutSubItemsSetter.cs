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

        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register(
                "ItemTemplate"
                , typeof(DataTemplate)
                , typeof(MenuFlyoutSubItemsSetter)
                , new PropertyMetadata(default(DataTemplate))
            );


        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }



        protected override void OnAttached()
        {
            base.OnAttached();

            this.AssociatedObject.Loaded += AssociatedObject_Loaded;
            this.AssociatedObject.Unloaded += AssociatedObject_Unloaded;
        }

        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            var subItem = sender as MenuFlyoutSubItem;
            subItem.Items.Clear();
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            
            var subItem = sender as MenuFlyoutSubItem;

            var itemsSrouce = ItemsSource.Cast<object>().ToList();
            foreach (var item in itemsSrouce)
            {
                var elem = subItem.Items.ElementAtOrDefault(itemsSrouce.IndexOf(item));
                if (elem == null)
                {
                    elem = ItemTemplate.LoadContent() as MenuFlyoutItemBase;
                    elem.DataContext = item;
                    subItem.Items.Add(elem);
                    elem.UpdateLayout();
                }

                elem.DataContext = item;
            }
            
            
        }


    }


}
