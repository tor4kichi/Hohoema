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

namespace NicoPlayerHohoema.Views.SettingsPageContent
{
	public sealed partial class PlayerSettingsPageContent : UserControl
	{
        // 0.25 ~ 2.0
        public List<double> PlaybackRateItems { get; private set; } =
            Enumerable.Range(-3, 8).Select(x => 1.0 + (x * 0.25)).ToList();


		public PlayerSettingsPageContent()
		{
			this.InitializeComponent();
		}
	}
}
