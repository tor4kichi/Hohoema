using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NicoPlayerHohoema.Views
{
	public class MediaElementExtention 
	{
		#region CustomStream Attached Behavior

		public static object GetCustomStream(DependencyObject obj)
		{
			return (object)obj.GetValue(CustomStreamProperty);
		}

		public static void SetCustomStream(DependencyObject obj, object value)
		{
			obj.SetValue(CustomStreamProperty, value);
		}

		public static readonly DependencyProperty CustomStreamProperty =
			DependencyProperty.RegisterAttached("CustomStream", typeof(object), typeof(MediaElementExtention), new PropertyMetadata(default(IRandomAccessStream), CustomStreamPropertyChanged));


		public static void CustomStreamPropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
		{
			if (s is MediaElement)
			{
				var mediaElement = s as MediaElement;

				mediaElement.Stop();

				if (e.NewValue is IRandomAccessStream)
				{
					var stream = e.NewValue as IRandomAccessStream;

					string contentType = "";
					if (stream is IRandomAccessStreamWithContentType)
					{
						contentType = (stream as IRandomAccessStreamWithContentType).ContentType;
					}



					if (stream == null)
					{
//						mediaElement.Stop();
					}
					else
					{
						mediaElement.SetSource(stream, contentType);
					}
				}
				else if (e.NewValue is FFmpegInterop.FFmpegInteropMSS)
				{
//					mediaElement.Stop();

					var mss = e.NewValue as FFmpegInterop.FFmpegInteropMSS;
					mediaElement.SetMediaStreamSource(mss.GetMediaStreamSource());
				}
				else if (e.NewValue is MediaStreamSource)
				{
					mediaElement.SetMediaStreamSource(e.NewValue as MediaStreamSource);
				}
			}

		}

		#endregion


		#region CustomMediaStream Attached Behavior

		public static IMediaSource GetCustomMediaStream(DependencyObject obj)
		{
			return (IMediaSource)obj.GetValue(CustomMediaStreamProperty);
		}

		public static void SetCustomMediaStream(DependencyObject obj, IMediaSource value)
		{
			obj.SetValue(CustomMediaStreamProperty, value);
		}

		public static readonly DependencyProperty CustomMediaStreamProperty =
			DependencyProperty.RegisterAttached("CustomMediaStream", typeof(IMediaSource), typeof(MediaElementExtention), new PropertyMetadata(default(IMediaSource), CustomMediaStreamPropertyChanged));


		public static void CustomMediaStreamPropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
		{
			if (s is MediaElement)
			{
				var mediaElement = s as MediaElement;
				var source = e.NewValue as IMediaSource;

				if (source == null)
				{
					mediaElement.Source = null;
				}
				else
				{
					mediaElement.SetMediaStreamSource(source);
				}
			}
		}

		#endregion
	}
}
