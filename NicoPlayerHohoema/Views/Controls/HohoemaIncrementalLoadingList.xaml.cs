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

        public static readonly DependencyProperty ItemFlyoutProperty =
            DependencyProperty.Register("ItemFlyout"
                    , typeof(FlyoutBase)
                    , typeof(IncrementalLoadingList)
                    , new PropertyMetadata(default(FlyoutBase))
                );

        public FlyoutBase ItemFlyout
        {
            get { return (FlyoutBase)GetValue(ItemFlyoutProperty); }
            set { SetValue(ItemFlyoutProperty, value); }
        }


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
            if (this.ItemTemplate == null)
            {
                var defaultTemplate = Resources["DefaultListItemTemplate"] as DataTemplate;
                ItemTemplate = defaultTemplate;
            }
        }
    }


	
	
}
