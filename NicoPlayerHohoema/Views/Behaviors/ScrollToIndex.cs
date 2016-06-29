using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using WinRTXamlToolkit.Controls.Extensions;

namespace NicoPlayerHohoema.Views.Behaviors
{
	public class ScrollToIndex : Behavior<ListViewBase>, IAction
	{
		#region Position Property

		public static readonly DependencyProperty IndexProperty =
			DependencyProperty.Register("Index"
					, typeof(int)
					, typeof(ScrollToIndex)
					, new PropertyMetadata(default(int))
				);

		public int Index
		{
			get { return (int)GetValue(IndexProperty); }
			set { SetValue(IndexProperty, value); }
		}

		#endregion

		public object Execute(object sender, object parameter)
		{
			if (this.AssociatedObject == null) { return false; }

			this.AssociatedObject.ScrollToIndex(Index).ConfigureAwait(false);

			return true;
		}



		
	}

	// http://stackoverflow.com/questions/32557216/windows-10-scrollintoview-is-not-scrolling-to-the-items-in-the-middle-of-a-lis
	public static class Helper
	{
		public async static Task ScrollToIndex(this ListViewBase listViewBase, int index)
		{
			bool isVirtualizing = default(bool);
			double previousHorizontalOffset = default(double), previousVerticalOffset = default(double);

			// get the ScrollViewer withtin the ListView/GridView
			var scrollViewer = listViewBase.GetScrollViewer();
			// get the SelectorItem to scroll to
			var selectorItem = listViewBase.ContainerFromIndex(index) as SelectorItem;

			// when it's null, means virtualization is on and the item hasn't been realized yet
			if (selectorItem == null)
			{
				isVirtualizing = true;

				previousHorizontalOffset = scrollViewer.HorizontalOffset;
				previousVerticalOffset = scrollViewer.VerticalOffset;

				// call task-based ScrollIntoViewAsync to realize the item
				await listViewBase.ScrollIntoViewAsync(listViewBase.Items[index]);

				// this time the item shouldn't be null again
				selectorItem = (SelectorItem)listViewBase.ContainerFromIndex(index);
			}

			// calculate the position object in order to know how much to scroll to
			var transform = selectorItem.TransformToVisual((UIElement)scrollViewer.Content);
			var position = transform.TransformPoint(new Point(0, 0));

			// when virtualized, scroll back to previous position without animation
			if (isVirtualizing)
			{
				await scrollViewer.ChangeViewAsync(previousHorizontalOffset, previousVerticalOffset, true);
			}

			// scroll to desired position with animation!
			scrollViewer.ChangeView(position.X, position.Y, null);
		}

		public async static Task ScrollToItem(this ListViewBase listViewBase, object item)
		{
			bool isVirtualizing = default(bool);
			double previousHorizontalOffset = default(double), previousVerticalOffset = default(double);

			// get the ScrollViewer withtin the ListView/GridView
			var scrollViewer = listViewBase.GetScrollViewer();
			// get the SelectorItem to scroll to
			var selectorItem = listViewBase.ContainerFromItem(item) as SelectorItem;

			// when it's null, means virtualization is on and the item hasn't been realized yet
			if (selectorItem == null)
			{
				isVirtualizing = true;

				previousHorizontalOffset = scrollViewer.HorizontalOffset;
				previousVerticalOffset = scrollViewer.VerticalOffset;

				// call task-based ScrollIntoViewAsync to realize the item
				await listViewBase.ScrollIntoViewAsync(item);

				// this time the item shouldn't be null again
				selectorItem = (SelectorItem)listViewBase.ContainerFromItem(item);
			}

			// calculate the position object in order to know how much to scroll to
			var transform = selectorItem.TransformToVisual((UIElement)scrollViewer.Content);
			var position = transform.TransformPoint(new Point(0, 0));

			// when virtualized, scroll back to previous position without animation
			if (isVirtualizing)
			{
				await scrollViewer.ChangeViewAsync(previousHorizontalOffset, previousVerticalOffset, true);
			}

			// scroll to desired position with animation!
			scrollViewer.ChangeView(position.X, position.Y, null);
		}

		public static async Task ScrollIntoViewAsync(this ListViewBase listViewBase, object item)
		{
			var tcs = new TaskCompletionSource<object>();
			var scrollViewer = listViewBase.GetScrollViewer();

			EventHandler<ScrollViewerViewChangedEventArgs> viewChanged = (s, e) => tcs.TrySetResult(null);
			try
			{
				scrollViewer.ViewChanged += viewChanged;
				listViewBase.ScrollIntoView(item, ScrollIntoViewAlignment.Leading);
				await tcs.Task;
			}
			finally
			{
				scrollViewer.ViewChanged -= viewChanged;
			}
		}

		public static async Task ChangeViewAsync(this ScrollViewer scrollViewer, double? horizontalOffset, double? verticalOffset, bool disableAnimation)
		{
			var tcs = new TaskCompletionSource<object>();

			EventHandler<ScrollViewerViewChangedEventArgs> viewChanged = (s, e) => tcs.TrySetResult(null);
			try
			{
				scrollViewer.ViewChanged += viewChanged;
				scrollViewer.ChangeView(horizontalOffset, verticalOffset, null, disableAnimation);
				await tcs.Task;
			}
			finally
			{
				scrollViewer.ViewChanged -= viewChanged;
			}
		}
	}
		

}
