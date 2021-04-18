using NiconicoLiveToolkit.Live.WatchSession;
using Hohoema.Models.Domain.Niconico;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Hohoema.Models.Domain.Player;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Hohoema.Presentation.Views
{
	public sealed partial class CommentUI : UserControl
	{
        private float _TextHeight;
        public float TextHeight => _TextHeight;
        private float _TextWidth;
        public float TextWidth => _TextWidth;


        public TimeSpan VideoPosition { get; set; }
        public TimeSpan EndPosition { get; set; }

        public IComment Comment { get; set; }

        public string CommentText
        {
            get { return (string)GetValue(CommentTextProperty); }
            set { SetValue(CommentTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CommentText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommentTextProperty =
            DependencyProperty.Register("CommentText", typeof(string), typeof(CommentUI), new PropertyMetadata(string.Empty));



        public Color BackTextColor
        {
            get { return (Color)GetValue(BackTextColorProperty); }
            set { SetValue(BackTextColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BackTextColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BackTextColorProperty =
            DependencyProperty.Register("BackTextColor", typeof(Color), typeof(CommentUI), new PropertyMetadata(Colors.Transparent));




        public Color TextColor
        {
            get { return (Color)GetValue(TextColorProperty); }
            set { SetValue(TextColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TextColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextColorProperty =
            DependencyProperty.Register("TextColor", typeof(Color), typeof(CommentUI), new PropertyMetadata(Colors.Transparent));




        public double CommentFontSize
        {
            get { return (double)GetValue(CommentFontSizeProperty); }
            set { SetValue(CommentFontSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CommentFontSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommentFontSizeProperty =
            DependencyProperty.Register("CommentFontSize", typeof(double), typeof(CommentUI), new PropertyMetadata(14));





        public bool IsVisible
        {
            get { return (bool)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsVisible.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.Register("IsVisible", typeof(bool), typeof(CommentUI), new PropertyMetadata(true));




        public double TextBGOffsetX
        {
            get { return (double)GetValue(TextBGOffsetXProperty); }
            set { SetValue(TextBGOffsetXProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TextBGOffsetX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextBGOffsetXProperty =
            DependencyProperty.Register("TextBGOffsetX", typeof(double), typeof(CommentUI), new PropertyMetadata(0));




        public double TextBGOffsetY
        {
            get { return (double)GetValue(TextBGOffsetYProperty); }
            set { SetValue(TextBGOffsetYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TextBGOffsetY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextBGOffsetYProperty =
            DependencyProperty.Register("TextBGOffsetY", typeof(double), typeof(CommentUI), new PropertyMetadata(0));


        public CommentDisplayMode DisplayMode { get; set; }

        public double VerticalPosition { get; set; }

        public CommentUI()
		{
			this.InitializeComponent();

            SizeChanged += CommentUI_SizeChanged;
		}

        private void CommentUI_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _TextHeight = (float)DesiredSize.Height;
            _TextWidth = (float)DesiredSize.Width;
            _MoveCommentWidthTimeInVPos = null;
        }


        public bool IsInsideScreen { get; private set; }
		public int HorizontalPosition { get; private set; }


		public bool IsEndDisplay(TimeSpan currentVpos)
		{
			return EndPosition <= currentVpos;
		}

		
        public TimeSpan CommentDisplayDuration => EndPosition - VideoPosition;


        private TimeSpan? _MoveCommentWidthTimeInVPos = null;
        private TimeSpan CalcMoveCommentWidthTimeInVPos(int canvasWidth)
        {
            if (_MoveCommentWidthTimeInVPos != null)
            {
                return _MoveCommentWidthTimeInVPos.Value;
            }

            var speed = MoveSpeedPer1MilliSeconds(canvasWidth);

            // 時間 = 距離 ÷ 速さ
            var timeToSecondCommentWidthMove = TimeSpan.FromMilliseconds((int)(TextWidth / speed));
            
            _MoveCommentWidthTimeInVPos = timeToSecondCommentWidthMove;
            return timeToSecondCommentWidthMove;
        }

        private float MoveSpeedPer1MilliSeconds(int canvasWidth)
        {
            // 1 Vposあたりのコメントの移動量
            return (canvasWidth + TextWidth) / (float)CommentDisplayDuration.TotalMilliseconds;
        }


        public double? GetPosition(int canvasWidth, TimeSpan currentVPos)
        {
            if (VideoPosition > currentVPos) { return null; }
            if (EndPosition < currentVPos) { return null; }

            var speed = MoveSpeedPer1MilliSeconds(canvasWidth);
            var delta = currentVPos - VideoPosition;
            return (canvasWidth) - (double)(speed * delta.TotalMilliseconds);
        }

        public TimeSpan CalcTextShowRightEdgeTime(int canvasWidth)
        {
            return VideoPosition + CalcMoveCommentWidthTimeInVPos(canvasWidth);
        }

        public TimeSpan CalcReachLeftEdge(int canvasWidth)
        {
            return EndPosition - CalcMoveCommentWidthTimeInVPos(canvasWidth);
        }
	}
}
