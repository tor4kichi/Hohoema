using NicoPlayerHohoema.Models;
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
	public sealed partial class FavoriteManagePage : Page
	{
		public FavoriteManagePage()
		{
			this.InitializeComponent();
		}
	}

	public class FavTypeToSymbolIconConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is FavoriteItemType)
			{
				var type = (FavoriteItemType)value;

				switch (type)
				{
					case FavoriteItemType.Tag:
						return Symbol.Tag;
					case FavoriteItemType.Mylist:
						return Symbol.List;
					case FavoriteItemType.User:
						return Symbol.Contact;
					case FavoriteItemType.Community:
						return Symbol.People;
					default:
						throw new NotSupportedException();
				}
			}
			else
			{
				throw new NotSupportedException();
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
