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
using Windows.UI;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Hohoema.Presentation.Views.Player
{
    public sealed partial class LiveSettingsSidePaneContent : UserControl
    {
        private SettingsSidePaneContentViewModel _viewModel { get; }
        public LiveSettingsSidePaneContent()
        {
            DataContext = _viewModel = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<SettingsSidePaneContentViewModel>();

            this.InitializeComponent();
        }

        List<Color> CommentColorList = new[]
        {
            Colors.WhiteSmoke,
            Colors.Black
        }.ToList();

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


        public void IncreaseCommentDisplayTime()
        {
            CommentDisplayDurationNumberBox.Value += CommentDisplayDurationNumberBox.SmallChange;
        }

        public void DecreaseCommentDisplayTime()
        {
            CommentDisplayDurationNumberBox.Value -= CommentDisplayDurationNumberBox.SmallChange;
        }

        public void IncreaseCommentFontScale()
        {
            CommentFontScaleNumberBox.Value += CommentFontScaleNumberBox.SmallChange;
        }

        public void DecreaseCommentFontScale()
        {
            CommentFontScaleNumberBox.Value -= CommentFontScaleNumberBox.SmallChange;
        }
    }
}
