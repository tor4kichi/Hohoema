using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.ViewModels.Niconico.Video.Commands;
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

namespace Hohoema.Views.Flyouts
{
    public sealed partial class MylistItemFlyout : MenuFlyout
    {
        public MylistItemFlyout()
        {
            this.InitializeComponent();

            PlayAllItem.Command = Ioc.Default.GetService<PlaylistPlayAllCommand>();
        }
    }
}
