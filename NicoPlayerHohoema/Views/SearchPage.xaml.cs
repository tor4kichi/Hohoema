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

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace NicoPlayerHohoema.Views
{
	/// <summary>
	/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
	/// </summary>
	public sealed partial class SearchPage : Page
	{
		public SearchPage()
		{
			this.InitializeComponent();
		}
	}


	public class SearchTargetContentTemplateSelector : DataTemplateSelector
	{
		public DataTemplate Video { get; set; }
		public DataTemplate Mylist { get; set; }
		public DataTemplate Community { get; set; }
		public DataTemplate LiveVideo { get; set; }

		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			if (item is ViewModels.VideoSearchOptionViewModelBase)
			{
				return Video;
			}
			else if (item is ViewModels.MylistSearchOptionViewModel)
			{
				return Mylist;
			}
			else if (item is ViewModels.CommunitySearchOptionViewModel)
			{
				return Community;
			}
			else if (item is ViewModels.LiveSearchOptionViewModel)
			{
				return LiveVideo;
			}

			return base.SelectTemplateCore(item, container);
		}
	}


	
}
