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
using Hohoema.ViewModels.Pages.Hohoema.LocalMylist;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Hohoema.Views.Pages.Hohoema.LocalMylist
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class LocalPlaylistPage : Page
    {
        public LocalPlaylistPage()
        {
            this.InitializeComponent();
            DataContext = _vm = Ioc.Default.GetRequiredService<LocalPlaylistPageViewModel>();
        }

        private readonly LocalPlaylistPageViewModel _vm;
    }
}
