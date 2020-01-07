using NicoPlayerHohoema.ViewModels.PlayerSidePaneContent;
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
using Prism.Ioc;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace NicoPlayerHohoema.Views
{
	public sealed partial class VideoSettingsSidePaneContent : UserControl
	{
		private SettingsSidePaneContentViewModel _viewModel { get; }
		public VideoSettingsSidePaneContent()
		{
			DataContext = _viewModel = App.Current.Container.Resolve<SettingsSidePaneContentViewModel>();

			this.InitializeComponent();
		}
	}
}
