using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Graphics.Canvas;

namespace NicoPlayerHohoema.Views.UIEffects
{
	public static class Win2DBitmapExtention
	{
		public static async Task<CanvasBitmap> CreateBitmapFromUIElement(this ICanvasResourceCreator sender, UIElement ui)
		{
			using (var stream = new InMemoryRandomAccessStream())
			{
				var target = new RenderTargetBitmap();
				await target.RenderAsync(ui);

				var pixelBuffer = await target.GetPixelsAsync();
				var pixels = pixelBuffer.ToArray();

				var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
				encoder.SetPixelData(BitmapPixelFormat.Bgra8
					, BitmapAlphaMode.Premultiplied
					, (uint)target.PixelWidth
					, (uint)target.PixelHeight
					, 96
					, 96
					, pixels);

				await encoder.FlushAsync();
				stream.Seek(0);

				return await CanvasBitmap.LoadAsync(sender, stream);
			}
		}
	}
}
