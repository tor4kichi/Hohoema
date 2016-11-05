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
	public sealed partial class FollowManagePage : Page
	{
		public FollowManagePage()
		{
			this.InitializeComponent();
		}
	}

	public class FollowTypeToSymbolIconConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is FollowItemType)
			{
				var type = (FollowItemType)value;

				switch (type)
				{
					case FollowItemType.Tag:
						return Symbol.Tag;
					case FollowItemType.Mylist:
						return Symbol.List;
					case FollowItemType.User:
						return Symbol.Contact;
					case FollowItemType.Community:
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
