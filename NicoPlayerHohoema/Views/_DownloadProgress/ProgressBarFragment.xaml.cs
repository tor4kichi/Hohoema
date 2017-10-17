using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace NicoPlayerHohoema.Views.DownloadProgress
{
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
}
