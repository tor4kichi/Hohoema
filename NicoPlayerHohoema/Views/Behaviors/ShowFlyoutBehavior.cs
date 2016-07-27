using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;




namespace NicoPlayerHohoema.Views.Behaviors
{
	// this class copied from 
	// http://blog.kazuakix.jp/entry/2014/09/03/000537

	public class ShowFlyoutBehavior : DependencyObject, IBehavior
	{
		public DependencyObject AssociatedObject { get; private set; }

		

		// 前準備
		public void Attach(DependencyObject associatedObject)
		{
			this.AssociatedObject = associatedObject;
			// Tapped イベントで Flyout を開くようにする
			((FrameworkElement)this.AssociatedObject).Tapped += OnAssociatedObjectTapped;
		}

		// Flyout を開く
		private void OnAssociatedObjectTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
		{
			var flyout = FlyoutBase.GetAttachedFlyout((FrameworkElement)this.AssociatedObject);
			if (flyout != null)
			{
				FlyoutBase.ShowAttachedFlyout((FrameworkElement)this.AssociatedObject);
			}
		}

		// 後始末
		public void Detach()
		{
			((FrameworkElement)this.AssociatedObject).Tapped -= OnAssociatedObjectTapped;
			this.AssociatedObject = null;
		}
	}
}
