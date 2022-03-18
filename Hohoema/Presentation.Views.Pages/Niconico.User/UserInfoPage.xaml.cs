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
using Hohoema.Presentation.ViewModels.Pages.Niconico.VideoRanking;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Hohoema.Presentation.ViewModels.Pages.Niconico.User;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Hohoema.Presentation.Views.Pages.Niconico.User
{
	/// <summary>
	/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
	/// </summary>
	public sealed partial class UserInfoPage : Page
	{
		public UserInfoPage()
		{
			this.InitializeComponent();

			DataContext = _vm = Ioc.Default.GetRequiredService<UserInfoPageViewModel>();
		}

		private readonly UserInfoPageViewModel _vm;
	}
}
