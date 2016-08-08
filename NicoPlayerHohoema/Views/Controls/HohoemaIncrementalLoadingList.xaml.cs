using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
		public HohoemaIncrementalLoadingList()
		{
			this.InitializeComponent();
		}
	}


	public class IncrementalLoadingListItemTemplateSelector : DataTemplateSelector
	{
		public DataTemplate Default { get; set; }
		public DataTemplate History { get; set; }
		public DataTemplate Ranking { get; set; }
		public DataTemplate CacheManagement { get; set; }
		public DataTemplate FavFeed { get; set; }
		public DataTemplate Mylist { get; set; }



		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			if (item is ViewModels.CacheVideoViewModel)
			{
				return CacheManagement;
			}
			else if (item is ViewModels.HistoryVideoInfoControlViewModel)
			{
				return History;
			}
			else if (item is ViewModels.RankedVideoInfoControlViewModel)
			{
				return Ranking;
			}
			else if (item is ViewModels.FavoriteVideoInfoControlViewModel)
			{
				return FavFeed;
			}
			else if (item is ViewModels.VideoInfoControlViewModel)
			{
				return Default;
			}
			else if (item is ViewModels.MylistSearchListingItem)
			{
				return Mylist;
			}

			return base.SelectTemplateCore(item, container);
		}
	}
}
