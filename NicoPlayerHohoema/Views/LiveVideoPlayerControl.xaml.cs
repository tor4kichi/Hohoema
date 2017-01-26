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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace NicoPlayerHohoema.Views
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class LiveVideoPlayerControl : UserControl
    {
		public LiveVideoPlayerControl()
		{
			this.InitializeComponent();
		}
	}


	public class LiveInfoContentTemplateSelector : DataTemplateSelector
	{
		public DataTemplate Summary { get; set; }
		public DataTemplate Comment { get; set; }
		public DataTemplate Shere { get; set; }
		public DataTemplate Settings { get; set; }

		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			if (item is ViewModels.LiveVideoInfoContent.SummaryLiveInfoContentViewModel)
			{
				return Summary;
			}
			else if (item is ViewModels.LiveVideoInfoContent.CommentLiveInfoContentViewModel)
			{
				return Comment;
			}
			else if (item is ViewModels.LiveVideoInfoContent.ShereLiveInfoContentViewModel)
			{
				return Shere;
			}
			else if (item is ViewModels.LiveVideoInfoContent.SettingsLiveInfoContentViewModel)
			{
				return Settings;
			}

			return base.SelectTemplateCore(item, container);
		}
	}
}
