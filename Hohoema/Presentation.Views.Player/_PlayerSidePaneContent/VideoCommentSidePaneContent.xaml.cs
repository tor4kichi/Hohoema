using Hohoema.Presentation.ViewModels.Player.PlayerSidePaneContent;
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

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace Hohoema.Presentation.Views.Player
{
    public sealed partial class VideoCommentSidePaneContent : UserControl
    {
        VideoCommentSizePaneContentViewModel _viewModel;
        public VideoCommentSidePaneContent()
        {
            DataContext = _viewModel = App.Current.Container.Resolve<VideoCommentSizePaneContentViewModel>();
            this.InitializeComponent();
        }
    }
}
