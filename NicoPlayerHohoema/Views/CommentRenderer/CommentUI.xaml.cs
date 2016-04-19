using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace NicoPlayerHohoema.Views.CommentRenderer
{
	public sealed partial class CommentUI : UserControl
	{
		public Comment CommentData
		{
			get
			{
				return DataContext as Comment;
			}
		}

		public CommentUI()
		{
			this.InitializeComponent();

			Unloaded += CommentUI_Unloaded;
		}

		private void CommentUI_Unloaded(object sender, RoutedEventArgs e)
		{
			_bitmap?.Dispose();
			_bitmap = null;
		}

		public bool IsEndDisplay(uint currentVpos)
		{
			return CommentData.EndPosition <= currentVpos;
		}

		public int GetHorizontalPosition(int screenWidth, uint currentVpos)
		{
			// (Comment.EndPositioin - Comment.VideoPosition) の長さまでにコメント全体を表示しなければいけない
			// コメントの移動距離＝ screenWidth + Width

			//                                        コメント
			// ------------|--------------------------|-----------

			//                    コメント
			// ------------|--------------------------|-----------

			//      コメント
			// ------------|--------------------------|-----------

			// distance
			//      |---------------------------------|

			//

			var comment = CommentData;
			var width = this.DesiredSize.Width;

			var distance = screenWidth + width;
			var displayTime = (comment.EndPosition - comment.VideoPosition);
			var localVpos = displayTime - (comment.EndPosition - currentVpos);
			var lerp = localVpos / (float)displayTime;

			// 理論的にlocalVposはdisplayTimeを越えることはない

			return (int)Math.Floor(distance * lerp);
		}

		void TextBGCanvas_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
		{
			args.TrackAsyncAction(CreateResourcesAsync(sender).AsAsyncAction());
		}

		async Task CreateResourcesAsync(CanvasControl sender)
		{
			// give it a little bit delay to ensure the image is load, ideally you want to Image.ImageOpened event instead
//			await Task.Delay(200);

			using (var stream = new InMemoryRandomAccessStream())
			{
				// get the stream from the background image
				var target = new RenderTargetBitmap();
				await target.RenderAsync(this.TextUI);

				var pixelBuffer = await target.GetPixelsAsync();
				var pixels = pixelBuffer.ToArray();

				var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
				encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)target.PixelWidth, (uint)target.PixelHeight, 96, 96, pixels);

				await encoder.FlushAsync();
				stream.Seek(0);

				// load the stream into our bitmap
				_bitmap = await CanvasBitmap.LoadAsync(sender, stream);
			}
		}

		void TextBGCanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
		{
			if (_bitmap== null)
			{
				return;
			}

			using (var session = args.DrawingSession)
			{
				var blur = new ShadowEffect
				{
					BlurAmount = 3.0f, // increase this to make it more blurry or vise versa.
										//Optimization = EffectOptimization.Balanced, // default value
										//BorderMode = EffectBorderMode.Soft // default value

					Source = _bitmap
				};

				session.DrawImage(blur, new Rect(0, 0, sender.ActualWidth, sender.ActualHeight),
					new Rect(0, 0, _bitmap.SizeInPixels.Width, _bitmap.SizeInPixels.Height), 0.9f);
			}
		}

		CanvasBitmap _bitmap;

	}
}
