using System;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Hohoema.Views.Player.DownloadProgress;

public sealed partial class ProgressBarFragment : UserControl, IDisposable
	{
		public ProgressFragment ProgressFragment { get; private set; }

		public double CanvasWidth { get; private set; }

		public ProgressBarFragment(ProgressFragment fragment)
		{
			ProgressFragment = fragment;

			DataContext = ProgressFragment;

			ProgressFragment.PropertyChanged += ProgressFragment_PropertyChanged;

			this.InitializeComponent();
		}

		public void Dispose()
		{
			ProgressFragment.PropertyChanged -= ProgressFragment_PropertyChanged;
		}

		private void ProgressFragment_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			UpdateWidth();
		}


		public async void UpdateWidth()
		{
			await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => 
			{
				Bar.Width = ProgressFragment.GetWidthInCanvas(CanvasWidth);
			});
		}


		public void ResetCanvasWidth(double canvasWidth)
		{
			CanvasWidth = canvasWidth;

			UpdateWidth();
		}
	}
