using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace NicoPlayerHohoema.Views.Behaviors
{

	// MediaElementのバッファリング・再生の状態遷移について
	// https://msdn.microsoft.com/ja-jp/library/cc189079(v=vs.95).aspx

	public class MediaElementSeekAfterAutoPlayBehavior : Behavior<MediaElement>
	{
		protected override void OnAttached()
		{
			base.OnAttached();

			this.AssociatedObject.Loaded += AssociatedObject_Loaded;
		}


		protected override void OnDetaching()
		{
			base.OnDetaching();

			this.AssociatedObject.SeekCompleted -= AssociatedObject_SeekCompleted;
		}

		private void AssociatedObject_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			this.AssociatedObject.SeekCompleted += AssociatedObject_SeekCompleted;
		}

		private void AssociatedObject_SeekCompleted(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			var mediaElem = this.AssociatedObject;

			if (mediaElem.CurrentState != Windows.UI.Xaml.Media.MediaElementState.Paused)
			{
				mediaElem.Play();
			}
		}
	}
}
