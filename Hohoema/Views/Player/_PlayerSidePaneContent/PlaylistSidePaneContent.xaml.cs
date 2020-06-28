﻿using System;
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
using Hohoema.ViewModels.PlayerSidePaneContent;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Hohoema.Views
{
    public sealed partial class PlaylistSidePaneContent : UserControl
    {
        private PlaylistSidePaneContentViewModel _viewModel { get; }
        public PlaylistSidePaneContent()
        {
            DataContext = _viewModel = App.Current.Container.Resolve<PlaylistSidePaneContentViewModel>();
            this.InitializeComponent();
        }
    }
}
