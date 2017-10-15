using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace NicoPlayerHohoema.Views.Controls
{
    public partial class IncrementalLoadingList : Control
    {

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource"
                    , typeof(object)
                    , typeof(IncrementalLoadingList)
                    , new PropertyMetadata(default(object))
                );

        public object ItemsSource
        {
            get { return (object)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }






        public static readonly DependencyProperty IsSelectionEnabledProperty =
            DependencyProperty.Register("IsSelectionEnabled"
                    , typeof(bool)
                    , typeof(IncrementalLoadingList)
                    , new PropertyMetadata(default(bool), (x, y) =>
                    {
                        var _this = x as IncrementalLoadingList;
                        _this.IsNotSelectionEnabled = !_this.IsSelectionEnabled;
                        _this.SetSelectionVisualState();
                    })
                );

        public bool IsSelectionEnabled
        {
            get { return (bool)GetValue(IsSelectionEnabledProperty); }
            set { SetValue(IsSelectionEnabledProperty, value); }
        }





        public static readonly DependencyProperty IsNotSelectionEnabledProperty =
            DependencyProperty.Register("IsNotSelectionEnabled"
                    , typeof(bool)
                    , typeof(IncrementalLoadingList)
                    , new PropertyMetadata(default(bool))
                );

        public bool IsNotSelectionEnabled
        {
            get { return (bool)GetValue(IsNotSelectionEnabledProperty); }
            private set { SetValue(IsNotSelectionEnabledProperty, value); }
        }





        public static readonly DependencyProperty RefreshCommandProperty =
            DependencyProperty.Register("RefreshCommand"
                    , typeof(ICommand)
                    , typeof(IncrementalLoadingList)
                    , new PropertyMetadata(default(ICommand))
                );

        public ICommand RefreshCommand
        {
            get { return (ICommand)GetValue(RefreshCommandProperty); }
            set { SetValue(RefreshCommandProperty, value); }
        }





        public static readonly DependencyProperty ItemCommandProperty =
            DependencyProperty.Register("ItemCommand"
                    , typeof(ICommand)
                    , typeof(IncrementalLoadingList)
                    , new PropertyMetadata(default(ICommand))
                );

        public ICommand ItemCommand
        {
            get { return (ICommand)GetValue(ItemCommandProperty); }
            set { SetValue(ItemCommandProperty, value); }
        }


        public static readonly DependencyProperty HeaderProperty =
           DependencyProperty.Register(nameof(Header)
                   , typeof(object)
                   , typeof(IncrementalLoadingList)
                   , new PropertyMetadata(default(object))
               );

        public object Header
        {
            get { return (object)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }


        public static readonly DependencyProperty HeaderTemplateProperty =
           DependencyProperty.Register(nameof(HeaderTemplate)
                   , typeof(DataTemplate)
                   , typeof(IncrementalLoadingList)
                   , new PropertyMetadata(default(object))
               );

        public DataTemplate HeaderTemplate
        {
            get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }


        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register("ItemTemplate"
                    , typeof(DataTemplate)
                    , typeof(IncrementalLoadingList)
                    , new PropertyMetadata(default(DataTemplate))
                );

        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }





        public static readonly DependencyProperty RefreshIndicatorContentProperty =
            DependencyProperty.Register("RefreshIndicatorContent"
                    , typeof(object)
                    , typeof(IncrementalLoadingList)
                    , new PropertyMetadata(default(object))
                );

        public object RefreshIndicatorContent
        {
            get { return (object)GetValue(RefreshIndicatorContentProperty); }
            set { SetValue(RefreshIndicatorContentProperty, value); }
        }





        public static readonly DependencyProperty ItemsPanelProperty =
            DependencyProperty.Register("ItemsPanel"
                    , typeof(ItemsPanelTemplate)
                    , typeof(IncrementalLoadingList)
                    , new PropertyMetadata(default(ItemsPanelTemplate))
                );

        public ItemsPanelTemplate ItemsPanel
        {
            get { return (ItemsPanelTemplate)GetValue(ItemsPanelProperty); }
            set { SetValue(ItemsPanelProperty, value); }
        }



        

        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register("SelectedItems"
                    , typeof(object)
                    , typeof(IncrementalLoadingList)
                    , new PropertyMetadata(default(object))
                );

        public object SelectedItems
        {
            get { return (object)GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }



        public static readonly DependencyProperty ItemContextFlyoutProperty =
            DependencyProperty.Register("ItemContextFlyout"
                    , typeof(FlyoutBase)
                    , typeof(IncrementalLoadingList)
                    , new PropertyMetadata(default(FlyoutBase))
                );

        public FlyoutBase ItemContextFlyout
        {
            get { return (FlyoutBase)GetValue(ItemContextFlyoutProperty); }
            set { SetValue(ItemContextFlyoutProperty, value); }
        }
    }
}
