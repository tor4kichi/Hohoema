﻿using Hohoema.Presentation.ViewModels.Pages.Niconico.Search;
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

namespace Hohoema.Presentation.Views.Pages.Niconico.Search
{
	public sealed partial class SearchResultKeywordPage : Page
	{
		public SearchResultKeywordPage()
		{
			this.InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var oldViewModel = _vm;
            _vm = args.NewValue as SearchResultKeywordPageViewModel;
            if (args.NewValue != null && args.NewValue != oldViewModel)
            {
                this.Bindings.Update();
            }
        }

        private SearchResultKeywordPageViewModel _vm { get; set; }
    }
}
