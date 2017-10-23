using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace NicoPlayerHohoema.Views.Controls
{
    public sealed partial class ListingWithHeader : UserControl
    {
        public static readonly DependencyProperty NowLoadingProperty =
            DependencyProperty.Register(nameof(NowLoading)
                    , typeof(bool)
                    , typeof(ListingWithHeader)
                    , new PropertyMetadata(false)
                );

        public bool NowLoading
        {
            get { return (bool)GetValue(NowLoadingProperty); }
            set { SetValue(NowLoadingProperty, value); }
        }



        public static readonly DependencyProperty IsTVModeEnabledProperty =
            DependencyProperty.Register(nameof(IsTVModeEnabled)
                    , typeof(bool)
                    , typeof(ListingWithHeader)
                    , new PropertyMetadata(false)
                );

        public bool IsTVModeEnabled
        {
            get { return (bool)GetValue(IsTVModeEnabledProperty); }
            set { SetValue(IsTVModeEnabledProperty, value); }
        }


        public static readonly DependencyProperty ItemsSourceProperty =
           DependencyProperty.Register("ItemsSource"
                   , typeof(object)
                   , typeof(ListingWithHeader)
                   , new PropertyMetadata(default(object))
               );

        public object ItemsSource
        {
            get { return (object)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }


        public static readonly DependencyProperty ListItemTemplateProperty =
            DependencyProperty.Register(nameof(ListItemTemplate)
                    , typeof(DataTemplate)
                    , typeof(ListingWithHeader)
                    , new PropertyMetadata(default(DataTemplate))
                );

        public DataTemplate ListItemTemplate
        {
            get { return (DataTemplate)GetValue(ListItemTemplateProperty); }
            set { SetValue(ListItemTemplateProperty, value); }
        }


        public static readonly DependencyProperty GridItemTemplateProperty =
            DependencyProperty.Register(nameof(GridItemTemplate)
                    , typeof(DataTemplate)
                    , typeof(ListingWithHeader)
                    , new PropertyMetadata(default(DataTemplate))
                );

        public DataTemplate GridItemTemplate
        {
            get { return (DataTemplate)GetValue(GridItemTemplateProperty); }
            set { SetValue(GridItemTemplateProperty, value); }
        }


        public static readonly DependencyProperty ItemCommandProperty =
            DependencyProperty.Register("ItemCommand"
                    , typeof(ICommand)
                    , typeof(ListingWithHeader)
                    , new PropertyMetadata(default(ICommand))
                );

        public ICommand ItemCommand
        {
            get { return (ICommand)GetValue(ItemCommandProperty); }
            set { SetValue(ItemCommandProperty, value); }
        }


        public static readonly DependencyProperty IsSelectionEnabledProperty =
            DependencyProperty.Register("IsSelectionEnabled"
                    , typeof(bool)
                    , typeof(ListingWithHeader)
                    , new PropertyMetadata(default(bool))
                );

        public bool IsSelectionEnabled
        {
            get { return (bool)GetValue(IsSelectionEnabledProperty); }
            set { SetValue(IsSelectionEnabledProperty, value); }
        }


        public static readonly DependencyProperty RefreshCommandProperty =
            DependencyProperty.Register("RefreshCommand"
                    , typeof(ICommand)
                    , typeof(ListingWithHeader)
                    , new PropertyMetadata(default(ICommand))
                );

        public ICommand RefreshCommand
        {
            get { return (ICommand)GetValue(RefreshCommandProperty); }
            set { SetValue(RefreshCommandProperty, value); }
        }


        

        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register("SelectedItems"
                    , typeof(IList)
                    , typeof(ListingWithHeader)
                    , new PropertyMetadata(default(IList))
                );

        public IList SelectedItems
        {
            get { return (IList)GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }



        public static readonly DependencyProperty HeaderTemplateProperty =
            DependencyProperty.Register(nameof(HeaderTemplate)
                    , typeof(DataTemplate)
                    , typeof(ListingWithHeader)
                    , new PropertyMetadata(default(DataTemplate))
                );

        public DataTemplate HeaderTemplate
        {
            get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }


        public static readonly DependencyProperty HeaderTemplateSelectorProperty =
            DependencyProperty.Register(nameof(HeaderTemplateSelector)
                    , typeof(DataTemplateSelector)
                    , typeof(ListingWithHeader)
                    , new PropertyMetadata(default(DataTemplateSelector))
                );

        public DataTemplateSelector HeaderTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(HeaderTemplateSelectorProperty); }
            set { SetValue(HeaderTemplateSelectorProperty, value); }
        }


        public static readonly DependencyProperty HeaderContentProperty =
            DependencyProperty.Register(nameof(HeaderContent)
                    , typeof(object)
                    , typeof(ListingWithHeader)
                    , new PropertyMetadata(default(object))
                );

        public object HeaderContent
        {
            get { return (object)GetValue(HeaderContentProperty); }
            set { SetValue(HeaderContentProperty, value); }
        }


        public static readonly DependencyProperty ListPositionProperty =
            DependencyProperty.Register(nameof(ListPosition)
                    , typeof(double)
                    , typeof(ListingWithHeader)
                    , new PropertyMetadata(0.0)
                );

        public double ListPosition
        {
            get { return (double)GetValue(ListPositionProperty); }
            set { SetValue(ListPositionProperty, value); }
        }


        public ListingWithHeader()
        {
            this.InitializeComponent();
        }


        private void HohoemaListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (ItemCommand != null)
            {
                if (ItemCommand.CanExecute(e.ClickedItem))
                {
                    ItemCommand.Execute(e.ClickedItem);
                }
            }
        }
    }
}
