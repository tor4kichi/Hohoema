using NicoPlayerHohoema.Views.DownloadProgress;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
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
	public sealed partial class DownloadProgressBar : UserControl
	{
		public DownloadProgressBar()
		{
			this.InitializeComponent();

			this.Loaded += DownloadProgressBar_Loaded;
		}

		private void DownloadProgressBar_Loaded(object sender, RoutedEventArgs e)
		{
			ResetProgressBarFragmentItemsPosition();

			this.SizeChanged += DownloadProgressBar_SizeChanged;
		}

		private void DownloadProgressBar_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			ResetProgressBarFragmentItemsPosition();
		}


		#region Dependency Properties

		public ObservableCollection<ProgressFragment> ProgressFragmentItems
		{
			get { return (ObservableCollection<ProgressFragment>)GetValue(ProgressFragmentItemsProperty); }
			set { SetValue(ProgressFragmentItemsProperty, value); }
		}


		// Using a DependencyProperty as the backing store for WorkItems.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ProgressFragmentItemsProperty =
			DependencyProperty.Register("ProgressFragmentItems"
				, typeof(ObservableCollection<ProgressFragment>)
				, typeof(DownloadProgressBar)
				, new PropertyMetadata(null, OnProgressFragmentItemsChanged)
				);

		private static void OnProgressFragmentItemsChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			DownloadProgressBar me = sender as DownloadProgressBar;

			var old = e.OldValue as ObservableCollection<ProgressFragment>;

			if (old != null)
				old.CollectionChanged -= me.OnProgressFragmentItemsCollectionChanged;

			var n = e.NewValue as ObservableCollection<ProgressFragment>;

			if (n != null)
				n.CollectionChanged += me.OnProgressFragmentItemsCollectionChanged;
		}

		private void OnProgressFragmentItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{

			if (e.Action == NotifyCollectionChangedAction.Reset)
			{
				// Clear and update entire collection
				ProgressBarCanvas.Children.Clear();
			}

			if (e.NewItems != null)
			{
				foreach (ProgressFragment item in e.NewItems)
				{
					// Subscribe for changes on item


					// Add item to internal collection
					AddProgressFragment(item);
				}
			}

			if (e.OldItems != null && e.Action == NotifyCollectionChangedAction.Remove)
			{
				foreach (ProgressFragment item in e.OldItems)
				{
					// Unsubscribe for changes on item
					//item.PropertyChanged -= OnWorkItemChanged;

					// Remove item from internal collection
					RemoveProgressFragment(item);
				}
			}


		}

		private async void AddProgressFragment(ProgressFragment item)
		{
			await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => 
			{
				var fragmentUI = new ProgressBarFragment(item);

				ProgressBarCanvas.Children.Add(fragmentUI);

				CalcProgressBarFragmentPosition(fragmentUI);
			});

		}

		private async void RemoveProgressFragment(ProgressFragment item)
		{
			await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
			{
				var removeTarget = ProgressBarCanvas.Children.Cast<ProgressBarFragment>()
					.SingleOrDefault(x => x.ProgressFragment == item);

				if (removeTarget != null)
				{
					removeTarget.Dispose();

					ProgressBarCanvas.Children.Remove(removeTarget);
				}
			});
		}



		private void CalcProgressBarFragmentPosition(ProgressBarFragment fragmentUI, double? width = null)
		{
			if (width == null)
			{
				width = ProgressBarCanvas.ActualWidth;
			}

			fragmentUI.ResetCanvasWidth(width.Value);

			Canvas.SetLeft(fragmentUI,
				fragmentUI.ProgressFragment
				.GetStartPositionInCanvas(width.Value)
				);
		}


		private void ResetProgressBarFragmentItemsPosition()
		{
			var canvasWidth = ProgressBarCanvas.ActualWidth;
			foreach (var item in ProgressBarCanvas.Children)
			{
				var fragmentUI = item as ProgressBarFragment;
				CalcProgressBarFragmentPosition(fragmentUI, canvasWidth);
			}
		}

		#endregion
	}
}
