using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace NicoPlayerHohoema.Views.UIEffects
{
	public static class CanvasControlExtention
	{
		public static async Task<CanvasBitmap> CreateUIElementBitmap(this ICanvasResourceCreator sender, UIElement ui)
		{
			// UIElementをWin2Dのエフェクトソースとして使うために
			// UIElementの画像データをRenderTargetBitmapに一時的に描画し、
			// RenderTargetBitmap
			using (var stream = new InMemoryRandomAccessStream())
			{
				// get the stream from the background image
				var target = new RenderTargetBitmap();
				ui.UpdateLayout();
				await target.RenderAsync(ui);

				var pixelBuffer = await target.GetPixelsAsync();
				var pixels = pixelBuffer.ToArray();

				var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
				encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)target.PixelWidth, (uint)target.PixelHeight, 96, 96, pixels);

				await encoder.FlushAsync();
				stream.Seek(0);

				// load the stream into our bitmap
				return await CanvasBitmap.LoadAsync(sender, stream);
			}
		}
	}
}
