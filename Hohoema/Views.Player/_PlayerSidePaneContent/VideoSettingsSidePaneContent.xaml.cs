using Hohoema.ViewModels.Player.PlayerSidePaneContent;
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
using Hohoema.Models.Player;
using CommunityToolkit.Mvvm.DependencyInjection;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Hohoema.Views.Player
{
	public sealed partial class VideoSettingsSidePaneContent : UserControl
	{
		public VideoSettingsSidePaneContent()
		{
			this.InitializeComponent();
			DataContext = _vm = Ioc.Default.GetRequiredService<SettingsSidePaneContentViewModel>();
		}

		private readonly SettingsSidePaneContentViewModel _vm;

		List<Color> CommentColorList = new[]
		{
			Colors.WhiteSmoke,
			Colors.Black
		}.ToList();

		List<PlaylistEndAction> PlaylistEndActionList = new List<PlaylistEndAction>()
        {
				PlaylistEndAction.NothingDo,
				PlaylistEndAction.ChangeIntoSplit,
				PlaylistEndAction.CloseIfPlayWithCurrentWindow
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

	public enum CommentNGScoreShareLevel
	{
		/// <summary>
		/// 全て表示（NGScoreを無視）
		/// </summary>
		None,

		/// <summary>
		/// NGScore -10000 以上を表示
		/// </summary>
		Weak,
		/// <summary>
		/// NGScore -4800 以上を表示
		/// </summary>
		Middle,

		/// <summary>
		/// NGScore -1000 以上を表示
		/// </summary>
		Strong,

		/// <summary>
		/// NGScore -100 以上を表示
		/// </summary>
		VeryStrong,

		/// <summary>
		/// NGScore -10 以上を表示
		/// </summary>
		UltraVeryStrong
	}

	public class CommentNGScoreShareLevelConverter : IValueConverter
	{
		Dictionary<CommentNGScoreShareLevel, int> _levelToScoreMap = new Dictionary<CommentNGScoreShareLevel, int>() 
		{
			[CommentNGScoreShareLevel.None]       = -100000,
			[CommentNGScoreShareLevel.Weak]       = -10000,
			[CommentNGScoreShareLevel.Middle]     = -4800,
			[CommentNGScoreShareLevel.Strong]     = -1000,
			[CommentNGScoreShareLevel.VeryStrong] = -100,
			[CommentNGScoreShareLevel.UltraVeryStrong]  = -10,
		};


		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is int val)
			{
				foreach (var pair in _levelToScoreMap)
				{
					if (val == pair.Value || val < pair.Value)
					{
						return pair.Key;
					}
				}
			}

			return CommentNGScoreShareLevel.Weak;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			if (value is CommentNGScoreShareLevel level)
			{
				return _levelToScoreMap[level];
			}
			else
			{
				return _levelToScoreMap[CommentNGScoreShareLevel.Weak];
			}

		}
	}
}
