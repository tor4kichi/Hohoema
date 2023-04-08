#nullable enable
using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.ViewModels.Pages.Niconico.Activity;
using Windows.UI.Xaml.Controls;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Hohoema.Views.Pages.Niconico.Activity;

/// <summary>
/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
/// </summary>
public sealed partial class WatchHistoryPage : Page
	{
		public WatchHistoryPage()
		{
			this.InitializeComponent();
			DataContext = _vm = Ioc.Default.GetRequiredService<WatchHistoryPageViewModel>();
		}

		private readonly WatchHistoryPageViewModel _vm;
}
