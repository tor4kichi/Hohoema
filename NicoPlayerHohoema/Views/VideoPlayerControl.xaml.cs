using Prism.Windows.Mvvm;
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
	public sealed partial class VideoPlayerControl : UserControl
    {
		public VideoPlayerControl()
		{
			this.InitializeComponent();

			this.Unloaded += VideoPlayerPage_Unloaded;
		}

		private void VideoPlayerPage_Unloaded(object sender, RoutedEventArgs e)
		{
			(this.DataContext as IDisposable)?.Dispose();
		}
	}



	public class VideoInfoContentTemplateSelector : DataTemplateSelector
	{
		public DataTemplate Summary { get; set; }
		public DataTemplate Mylist { get; set; }
		public DataTemplate Comment { get; set; }
		public DataTemplate Shere { get; set; }
		public DataTemplate Settings { get; set; }


		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			if (item is ViewModels.VideoInfoContent.SummaryVideoInfoContentViewModel)
			{
				return Summary;
			}
			else if (item is ViewModels.VideoInfoContent.MylistVideoInfoContentViewModel)
			{
				return Mylist;
			}
			else if (item is ViewModels.VideoInfoContent.CommentVideoInfoContentViewModel)
			{
				return Comment;
			}
			else if (item is ViewModels.VideoInfoContent.ShereVideoInfoContentViewModel)
			{
				return Shere;
			}
			else if (item is ViewModels.VideoInfoContent.SettingsVideoInfoContentViewModel)
			{
				return Settings;
			}
			

			return base.SelectTemplateCore(item, container);
		}
	}
}
