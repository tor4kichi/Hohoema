using Hohoema.ViewModels.Pages.Niconico.Search;
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
using Hohoema.Services.Navigations;
using CommunityToolkit.Mvvm.DependencyInjection;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Hohoema.Views.Pages.Niconico.Search
{
	public sealed partial class SearchResultKeywordPage : Page
	{
		public SearchResultKeywordPage()
		{
			this.InitializeComponent();
            DataContext = _vm = Ioc.Default.GetRequiredService<SearchResultKeywordPageViewModel>();
        }

        private readonly SearchResultKeywordPageViewModel _vm;
    }
}
