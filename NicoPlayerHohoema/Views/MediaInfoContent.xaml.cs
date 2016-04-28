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

namespace NicoPlayerHohoema.Views
{
	public sealed partial class MediaInfoContent : UserControl
	{
		public MediaInfoContent()
		{
			this.InitializeComponent();
		}
	}

	public class MediaInfoContentTemplateSelector : DataTemplateSelector
	{
		public MediaInfoContentTemplateSelector()
		{

		}

		public DataTemplate SummaryTemplate { get; set; }
		public DataTemplate CommentTemplate { get; set; }
		public DataTemplate PlaylistTemplate { get; set; }
		public DataTemplate RelatedTemplate { get; set; }
		public DataTemplate IchibaTemplate { get; set; }


		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			if (item is ViewModels.SummaryMediaInfoViewModel)
			{
				return SummaryTemplate;
			}
			else if (item is ViewModels.CommentMediaInfoViewModel)
			{
				return CommentTemplate;
			}
			else if (item is ViewModels.PlaylistMediaInfoViewModel)
			{
				return PlaylistTemplate;
			}
			else if (item is ViewModels.RelatedVideoMediaInfoViewModel)
			{
				return RelatedTemplate;
			}
			else if (item is ViewModels.IchibaMediaInfoViewModel)
			{
				return IchibaTemplate;
			}
			else
			{
				return base.SelectTemplate(item);
			}
		}
	}
}
