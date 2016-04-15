using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NicoPlayerHohoema.Views
{
	public class MediaElementExtention 
	{
		#region CustomStream Attached Behavior

		public static IRandomAccessStream GetCustomStream(DependencyObject obj)
		{
			return (IRandomAccessStream)obj.GetValue(CustomStreamProperty);
		}

		public static void SetCustomStream(DependencyObject obj, IRandomAccessStream value)
		{
			obj.SetValue(CustomStreamProperty, value);
		}

		public static readonly DependencyProperty CustomStreamProperty =
			DependencyProperty.RegisterAttached("CustomStream", typeof(IRandomAccessStream), typeof(MediaElementExtention), new PropertyMetadata(default(IRandomAccessStream), PropertyChanged));


		public static void PropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
		{
			if (s is MediaElement)
			{
				var mediaElement = s as MediaElement;
				var stream = e.NewValue as IRandomAccessStream;

				string contentType = "";
				if (stream is Util.HttpRandomAccessStream)
				{
					contentType = (stream as Util.HttpRandomAccessStream).ContentType;
				}

				if (stream == null)
				{
					mediaElement.Source = null;
				}
				else
				{
					mediaElement.SetSource(stream, contentType);
				}
			}
		}

		#endregion
	}
}
