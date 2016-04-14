using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Streaming.Adaptive;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web;
using Windows.Web.Http;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace NicoPlayerHohoema.Views
{
	public sealed partial class NicoVideoMediaElement : UserControl
	{
		public static readonly DependencyProperty VideoUrlProperty =
		   DependencyProperty.Register(
			   "VideoUrl", // プロパティ名を指定
			   typeof(string), // プロパティの型を指定
			   typeof(NicoVideoMediaElement), // プロパティを所有する型を指定
			   new PropertyMetadata("", VideoUrlPropertyChanged)); // メタデータを指定。ここではデフォルト値を設定してる

		// 依存関係プロパティのCLRのプロパティのラッパー
		public string VideoUrl
		{
			get { return (string)GetValue(VideoUrlProperty); }
			set { SetValue(VideoUrlProperty, value); }
		}

		private static void VideoUrlPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var _this = d as NicoVideoMediaElement;
			_this.RefreshVideoStreaming();
		}


		public static readonly DependencyProperty HttpClientProperty =
		   DependencyProperty.Register(
			   "HttpClient", // プロパティ名を指定
			   typeof(HttpClient), // プロパティの型を指定
			   typeof(NicoVideoMediaElement), // プロパティを所有する型を指定
			   new PropertyMetadata("", VideoUrlPropertyChanged)); // メタデータを指定。ここではデフォルト値を設定してる

		// 依存関係プロパティのCLRのプロパティのラッパー
		public HttpClient HttpClient
		{
			get { return (HttpClient)GetValue(HttpClientProperty); }
			set { SetValue(HttpClientProperty, value); }
		}

		private static void HttpClientPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var _this = d as NicoVideoMediaElement;
			_this.RefreshVideoStreaming();
		}




		public NicoVideoMediaElement()
		{
			this.InitializeComponent();
		}



		private async void RefreshVideoStreaming()
		{
			MediaElem.Stop();

			// can streaming video?
			if (String.IsNullOrWhiteSpace(VideoUrl) || HttpClient == null)
			{
				return;
			}

			// create streaming
			var stream = await Util.HttpRandomAccessStream.CreateAsync(HttpClient, new Uri(VideoUrl));

			MediaElem.SetSource(stream, stream.ContentType);
		}
	}


	public class NicoVideoMediaStreamDiscripter : IMediaStreamDescriptor
	{
		public bool IsSelected
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public string Language
		{
			get
			{
				throw new NotImplementedException();
			}

			set
			{
				throw new NotImplementedException();
			}
		}

		public string Name
		{
			get
			{
				throw new NotImplementedException();
			}

			set
			{
				throw new NotImplementedException();
			}
		}
	}

}
