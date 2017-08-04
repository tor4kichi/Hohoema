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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace NicoPlayerHohoema.Views.CommentRenderer
{
	public sealed partial class CommentUI : UserControl
	{
		public Comment CommentData { get; set; }

        public bool IsVerticalPositionCulcurated { get; set; }

        private float _TextHeight;
        public float TextHeight => _TextHeight;
        private float _TextWidth;
        public float TextWidth => _TextWidth;


        public CommentUI()
		{
			this.InitializeComponent();

            DataContextChanged += CommentUI_DataContextChanged;
            SizeChanged += CommentUI_SizeChanged;
		}

        private void CommentUI_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _TextHeight = (float)DesiredSize.Height;
            _TextWidth = (float)DesiredSize.Width;
        }

        private void CommentUI_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            CommentData = DataContext as Comment;
        }


        public bool IsInsideScreen { get; private set; }
		public int HorizontalPosition { get; private set; }


		public bool IsEndDisplay(uint currentVpos)
		{
			return CommentData.EndPosition <= currentVpos;
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
			var comment = CommentData;
			var width = _TextWidth;

			var distance = screenWidth + width;
			var displayTime = (comment.EndPosition - comment.VideoPosition);
			var localVpos = displayTime - (comment.EndPosition - currentVpos);
			var lerp = localVpos / (float)displayTime;

			// 理論的にlocalVposはdisplayTimeを越えることはない

			var result = (int)Math.Floor(distance * lerp);

			IsInsideScreen = result > width;

			HorizontalPosition = result;
		}

	}
}
