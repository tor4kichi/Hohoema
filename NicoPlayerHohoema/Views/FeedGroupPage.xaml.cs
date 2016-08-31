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
	public sealed partial class FeedGroupPage : Page
	{
		public FeedGroupPage()
		{
			this.InitializeComponent();
		}
	}

	public class FeedItemSourceTemplateSelector : DataTemplateSelector
	{
		public DataTemplate Tag { get; set; }
		public DataTemplate Mylist { get; set; }
		public DataTemplate User { get; set; }

		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			if (item is ViewModels.FeedItemSourceViewModel)
			{
				var itemSourceVM = item as ViewModels.FeedItemSourceViewModel;

				switch (itemSourceVM.ItemType)
				{
					case Models.FavoriteItemType.Tag:
						return Tag;
					case Models.FavoriteItemType.Mylist:
						return Mylist;
					case Models.FavoriteItemType.User:
						return User;
					default:
						break;
				}
			}

			return base.SelectTemplateCore(item, container);
		}
	}
}
