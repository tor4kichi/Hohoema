using System;

namespace Hohoema.Views.Player;

public static class CommentRendererHelper
{
    public static bool IsStreamCommentColide(CommentUI first, CommentUI second, double canvasWidth)
    {
        return first.EndPosition > CalcStreamCommentReachToScreenLeftEdge(second, canvasWidth);
    }

    public static TimeSpan CalcStreamCommentReachToScreenLeftEdge(CommentUI second, double canvasWidth)
    {
        var secondDisplayTime = second.EndPosition - second.VideoPosition;

        // 1msあたりの secondコメントの移動量
        var secondSpeed = ((float)canvasWidth + second.TextWidth) / (float)secondDisplayTime.TotalMilliseconds;

        // 時間 = 距離 ÷ 速さ
        var timeToSecondCommentWidthMove = TimeSpan.FromMilliseconds(second.TextWidth / secondSpeed);

        return timeToSecondCommentWidthMove;
    }
}
