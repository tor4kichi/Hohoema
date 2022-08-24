using CommunityToolkit.Mvvm.Input;
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
using Windows.Media.Playback;
using Windows.UI.Core;
using System.Windows.Input;
using Reactive.Bindings.Extensions;
using Windows.UI;
using Microsoft.Toolkit.Uwp.Helpers;
using Hohoema.Models.Domain;
using Hohoema.Models.UseCase.Niconico.Player;
using Hohoema.Models.Domain.Application;
using System.Diagnostics;
using Hohoema.Models.Domain.Player;
using Windows.System;
using Hohoema.Presentation.ViewModels.Player;
using System.Reactive.Disposables;
using Hohoema.Presentation.Navigations;
using CommunityToolkit.Mvvm.DependencyInjection;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Hohoema.Presentation.Views.Player
{
    public sealed partial class VideoPlayerPage : Page
    {
        public VideoPlayerPage()
        {
            this.InitializeComponent();

            DataContext = _vm = Ioc.Default.GetRequiredService<VideoPlayerPageViewModel>();
        }

        private readonly VideoPlayerPageViewModel _vm;        
    }
}
