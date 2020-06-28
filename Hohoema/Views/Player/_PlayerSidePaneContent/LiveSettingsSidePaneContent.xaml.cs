using Hohoema.ViewModels.PlayerSidePaneContent;
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
using Windows.UI;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Hohoema.Views
{
    public sealed partial class LiveSettingsSidePaneContent : UserControl
    {
        private SettingsSidePaneContentViewModel _viewModel { get; }
        public LiveSettingsSidePaneContent()
        {
            DataContext = _viewModel = App.Current.Container.Resolve<SettingsSidePaneContentViewModel>();

            this.InitializeComponent();
        }

        List<Color> CommentColorList = new[]
        {
            Colors.WhiteSmoke,
            Colors.Black
        }.ToList();

        List<Models.PlaylistEndAction> PlaylistEndActionList = new List<Models.PlaylistEndAction>()
        {
                Models.PlaylistEndAction.NothingDo,
                Models.PlaylistEndAction.ChangeIntoSplit,
                Models.PlaylistEndAction.CloseIfPlayWithCurrentWindow
        };

        List<double> VideoPlaybackRateList = new List<double>()
        {
            2.0,
            1.75,
            1.5,
            1.25,
            1.0,
            .75,
            .5,
            .25,
            .05
        };

        List<CommentNGScoreShareLevel> _NGScoreShareLevels = new[]
        {
            CommentNGScoreShareLevel.None,
            CommentNGScoreShareLevel.Weak,
            CommentNGScoreShareLevel.Middle,
            CommentNGScoreShareLevel.Strong,
            CommentNGScoreShareLevel.VeryStrong,
            CommentNGScoreShareLevel.UltraVeryStrong,
        }.ToList();
    }
}
