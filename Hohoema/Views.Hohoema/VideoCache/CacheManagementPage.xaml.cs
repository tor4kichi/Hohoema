using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.ViewModels.Pages.Hohoema.VideoCache;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Hohoema.Views.Pages.Hohoema.VideoCache;

/// <summary>
/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
/// </summary>
public sealed partial class CacheManagementPage : Page
	{
		public CacheManagementPage()
		{
			this.InitializeComponent();
			DataContext = _vm = Ioc.Default.GetRequiredService<CacheManagementPageViewModel>();
		}

		private readonly CacheManagementPageViewModel _vm;
	}



public class ProgressTemplateSelector : DataTemplateSelector
	{
		public DataTemplate Progress { get; set; }
		public DataTemplate Empty { get; set; }


		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			if (item == null)
			{
				return Empty;
			}
			else
			{
				return Progress;
			}
		}
	}
