using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Views
{
    public static class CommentRendererHelper
    {
        public static bool IsStreamCommentColide(CommentUI first, CommentUI second, double canvasWidth)
        {
            return first.EndPosition > CalcStreamCommentReachToScreenLeftEdge(second, canvasWidth);
        }

        public static uint CalcStreamCommentReachToScreenLeftEdge(CommentUI second, double canvasWidth)
        {
            var secondDisplayTime = second.EndPosition - second.VideoPosition;

            // 1 Vposあたりの secondコメントの移動量
            var secondSpeed = (canvasWidth + second.TextWidth) / (float)secondDisplayTime;

            // 時間 = 距離 ÷ 速さ
            var timeToSecondCommentWidthMove = (uint)(second.TextWidth / secondSpeed);

            return timeToSecondCommentWidthMove;
        }
    }
}
