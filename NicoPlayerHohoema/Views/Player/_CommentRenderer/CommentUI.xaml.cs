using NicoPlayerHohoema.Models.Niconico;
using System;
using System.Collections.Generic;
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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace NicoPlayerHohoema.Views
{
	public sealed partial class CommentUI : UserControl
	{
        public bool IsVerticalPositionCulcurated { get; set; }

        private float _TextHeight;
        public float TextHeight => _TextHeight;
        private float _TextWidth;
        public float TextWidth => _TextWidth;


        public long VideoPosition { get; set; }
        public long EndPosition { get; set; }

        public string CommentText { get; set; }

        public Color BackTextColor { get; set; } 
        public Color TextColor { get; set; }

        public double CommentFontSize { get; set; }

        public bool IsVisible { get; set; }

        public double TextBGOffsetX { get; set; }
        public double TextBGOffsetY { get; set; }

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


		public bool IsEndDisplay(uint currentVpos)
		{
			return EndPosition <= currentVpos;
		}

		public void Update(int screenWidth, uint currentVpos)
		{
			// (Comment.EndPositioin - Comment.VideoPosition) の長さまでにコメント全体を表示しなければいけない
			// コメントの移動距離＝ screenWidth + Width

			//                                        コメント
			// ------------|--------------------------|-----------

			//                    コメント
			// ------------|--------------------------|-----------

			//      コメント
			// ------------|--------------------------|-----------

			// distance
			//      |---------------------------------|

			//

            if (_TextWidth == 0)
            {
                HorizontalPosition = 0;
                return;
            }
			var width = _TextWidth;

			var distance = screenWidth + width;
			var displayTime = (EndPosition - VideoPosition);
			var localVpos = displayTime - (EndPosition - currentVpos);
			var lerp = localVpos / (float)displayTime;

			// 理論的にlocalVposはdisplayTimeを越えることはない

			var result = (int)Math.Floor(distance * lerp);

			IsInsideScreen = result > width;

			HorizontalPosition = result;
		}


        public long CommentDisplayDuration => EndPosition - VideoPosition;


        private long? _MoveCommentWidthTimeInVPos = null;
        private long CalcMoveCommentWidthTimeInVPos(int canvasWidth)
        {
            if (_MoveCommentWidthTimeInVPos != null)
            {
                return _MoveCommentWidthTimeInVPos.Value;
            }

            var speed = MoveSpeedPer1VPos(canvasWidth);

            // 時間 = 距離 ÷ 速さ
            var timeToSecondCommentWidthMove = (uint)(TextWidth / speed);

            _MoveCommentWidthTimeInVPos = timeToSecondCommentWidthMove;
            return timeToSecondCommentWidthMove;
        }

        private float MoveSpeedPer1VPos(int canvasWidth)
        {
            // 1 Vposあたりのコメントの移動量
            return (canvasWidth + TextWidth) / (float)CommentDisplayDuration;
        }


        public double? GetPosition(int canvasWidth, long currentVPos)
        {
            if (VideoPosition > currentVPos) { return null; }
            if (EndPosition < currentVPos) { return null; }

            var speed = MoveSpeedPer1VPos(canvasWidth);
            var delta = currentVPos - VideoPosition;
            return (canvasWidth) - (double)(speed * delta);
        }

        public long CalcTextShowRightEdgeTime(int canvasWidth)
        {
            return VideoPosition + CalcMoveCommentWidthTimeInVPos(canvasWidth);
        }

        public long CalcReachLeftEdge(int canvasWidth)
        {
            return EndPosition - CalcMoveCommentWidthTimeInVPos(canvasWidth);
        }
	}
}
