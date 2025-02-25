﻿#nullable enable
using CommunityToolkit.Diagnostics;
using Hohoema.Models.Player.Comment;
using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Hohoema.Views.Player;

public sealed partial class CommentUI : UserControl
{
    public float TextHeight { get; private set; }
    public float TextWidth { get; private set; }


    public TimeSpan VideoPosition { get; set; }
    public TimeSpan EndPosition { get; set; }

    public IComment? Comment { get; set; }

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
        TextHeight = (float)DesiredSize.Height;
        TextWidth = (float)DesiredSize.Width;
        _MoveCommentWidthTimeInVPos = null;
    }


    public bool IsInsideScreen { get; private set; }
    public int HorizontalPosition { get; private set; }


    public bool IsEndDisplay(TimeSpan currentVpos)
    {
        return EndPosition <= currentVpos;
    }


    public TimeSpan CommentDisplayDuration { get; set; }
    public float InverseCommentDisplayDurationInMs { get; set; }


    private TimeSpan? _MoveCommentWidthTimeInVPos = null;
    private TimeSpan CalcMoveCommentWidthTimeInVPos(int canvasWidth)
    {
        if (_MoveCommentWidthTimeInVPos != null)
        {
            return _MoveCommentWidthTimeInVPos.Value;
        }

        var speed = MoveSpeedPer1MilliSeconds(canvasWidth);

#if DEBUG
        Guard.IsNotEqualTo(TextWidth, 0, nameof(TextWidth));
        Guard.IsFalse(float.IsNaN(speed), "comment speed is NaN.");
#endif
        // 時間 = 距離 ÷ 速さ
        var time = (TextWidth / speed);
        if (float.IsNaN(time))
        {
            return TimeSpan.Zero;
        }

        var timeToSecondCommentWidthMove = TimeSpan.FromMilliseconds(time);

        _MoveCommentWidthTimeInVPos = timeToSecondCommentWidthMove;
        return timeToSecondCommentWidthMove;
    }

    private float MoveSpeedPer1MilliSeconds(int canvasWidth)
    {
        // 1 Vposあたりのコメントの移動量
        return (canvasWidth + TextWidth) * InverseCommentDisplayDurationInMs;
    }


    public float GetPosition(int canvasWidth, TimeSpan currentVPos)
    {
        return (canvasWidth + TextWidth) * ((float)(EndPosition - currentVPos).TotalMilliseconds * InverseCommentDisplayDurationInMs) - TextWidth;
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
