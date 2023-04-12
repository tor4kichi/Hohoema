#nullable enable
using Hohoema.Models.Player.Comment;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Toolkit.Uwp.UI.Animations;
using Reactive.Bindings.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Hohoema.Views.Player;

sealed class CommentUIObjectPoolPolicy : IPooledObjectPolicy<CommentUI>
{
    public CommentUI Create()
    {
        return new CommentUI();
    }

    public bool Return(CommentUI commentUI)
    {
        commentUI.Comment = null;
        commentUI.Opacity = 1.0;
        return true;
    }
}

public sealed partial class CommentRenderer : UserControl
{
    public CommentRenderer()
    {
        this.InitializeComponent();

        Loaded += CommentRenderer_Loaded;
        Unloaded += CommentRenderer_Unloaded;
        
        _commentUIObjectPool = new DefaultObjectPool<CommentUI>(new CommentUIObjectPoolPolicy(), 200);

        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _windowResizeTimer = _dispatcherQueue.CreateTimer();
        _windowResizeTimer.Interval = TimeSpan.FromSeconds(0.25);
        _windowResizeTimer.Tick += _windowResizeTimer_Tick;
        _windowResizeTimer.IsRepeating = false;
    }

    CompositeDisposable _disposables;

    private readonly DefaultObjectPool<CommentUI> _commentUIObjectPool;
    private readonly DispatcherQueue _dispatcherQueue;

    #region Dependency Properties

    public static readonly DependencyProperty CommentDefaultColorProperty =
        DependencyProperty.Register("CommentDefaultColor"
            , typeof(Color)
            , typeof(CommentRenderer)
            , new PropertyMetadata(Windows.UI.Colors.WhiteSmoke)
            );


    public Color CommentDefaultColor
    {
        get { return (Color)GetValue(CommentDefaultColorProperty); }
        set { SetValue(CommentDefaultColorProperty, value); }
    }





    public static readonly DependencyProperty SelectedCommentOutlineColorProperty =
        DependencyProperty.Register("SelectedCommentOutlineColor"
            , typeof(Color)
            , typeof(CommentRenderer)
            , new PropertyMetadata(Windows.UI.Colors.LightGray)
            );


    public Color SelectedCommentOutlineColor
    {
        get { return (Color)GetValue(SelectedCommentOutlineColorProperty); }
        set { SetValue(SelectedCommentOutlineColorProperty, value); }
    }


    public static readonly DependencyProperty CommentSizeScaleProperty =
        DependencyProperty.Register("CommentSizeScale"
            , typeof(double)
            , typeof(CommentRenderer)
            , new PropertyMetadata(1.0)
            );


    public double CommentSizeScale
    {
        get { return (double)GetValue(CommentSizeScaleProperty); }
        set { SetValue(CommentSizeScaleProperty, value); }
    }



    public static readonly DependencyProperty MediaPlayerProperty =
        DependencyProperty.Register("MediaPlayer"
                , typeof(MediaPlayer)
                , typeof(CommentRenderer)
                , new PropertyMetadata(default(MediaPlayer), OnMediaPlayerChanged)
            );

   

    public TimeSpan VideoPosition
    {
        get
        {
            try
            {
                return MediaPlayer?.PlaybackSession.Position ?? TimeSpan.Zero;
            }
            catch
            {
                return TimeSpan.Zero;
            }
        }
    }

    public MediaPlayer MediaPlayer
    {
        get { return (MediaPlayer)GetValue(MediaPlayerProperty); }
        set { SetValue(MediaPlayerProperty, value); }
    }

    public static readonly DependencyProperty DefaultDisplayDurationProperty =
        DependencyProperty.Register("DefaultDisplayDuration"
            , typeof(TimeSpan)
            , typeof(CommentRenderer)
            , new PropertyMetadata(TimeSpan.FromSeconds(4), OnDefaultDisplayDurationChanged)
            );

    public TimeSpan DefaultDisplayDuration
    {
        get { return (TimeSpan)GetValue(DefaultDisplayDurationProperty); }
        set { SetValue(DefaultDisplayDurationProperty, value); }
    }



    public IEnumerable Comments
    {
        get { return (IEnumerable)GetValue(CommentsProperty); }
        set { SetValue(CommentsProperty, value); }
    }


    // Using a DependencyProperty as the backing store for WorkItems.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty CommentsProperty =
        DependencyProperty.Register("Comments"
            , typeof(IEnumerable)
            , typeof(CommentRenderer)
            , new PropertyMetadata(null, OnCommentsChanged)
            );

    






    /// <summary>
    /// コメントの動画再生位置に対するオフセット時間 </br>
    /// MediaPlayer.Positionがソース設定時に 0 にリセットされる特性に対応する必要がある場合に指定します（特にニコニコ生放送）
    /// </summary>
    public TimeSpan VideoPositionOffset
    {
        get { return (TimeSpan)GetValue(VideoPositionOffsetProperty); }
        set { SetValue(VideoPositionOffsetProperty, value); }
    }

    public static readonly DependencyProperty VideoPositionOffsetProperty =
        DependencyProperty.Register(nameof(VideoPositionOffset)
            , typeof(TimeSpan)
            , typeof(CommentRenderer)
            , new PropertyMetadata(TimeSpan.Zero)
            );

    #endregion


    #region Event Handling

    private static void OnMediaPlayerChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var me = (CommentRenderer)sender;

        if (me.MediaPlayer == null)
        {
            var oldMediaPlayer = (e.OldValue as MediaPlayer);
            if (oldMediaPlayer != null)
            {
                oldMediaPlayer.PlaybackSession.PlaybackStateChanged -= me.PlaybackSession_PlaybackStateChanged;
            }
        }
        else
        {
            me.MediaPlayer.PlaybackSession.PlaybackStateChanged += me.PlaybackSession_PlaybackStateChanged;
        }
    }


    IDisposable CommentItemsChangedSubscriber;
    private static void OnCommentsChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        CommentRenderer me = sender as CommentRenderer;

        me.CommentItemsChangedSubscriber?.Dispose();
        
        if (e.NewValue is INotifyCollectionChanged newNCC)
        {
            me.CommentItemsChangedSubscriber = newNCC.CollectionChangedAsObservable()
                .Subscribe(args =>
                {                    
                    me._dispatcherQueue.TryEnqueue(() => 
                    {
                        try
                        {
                            if (args.Action == NotifyCollectionChangedAction.Reset)
                            {
                                me.ResetComments();
                            }
                            else if (args.Action == NotifyCollectionChangedAction.Add)
                            {
                                foreach (var comment in args.NewItems)
                                {
                                    me.AddOrPushPending(comment as IComment);
                                }
                            }
                            else if (args.Action == NotifyCollectionChangedAction.Remove)
                            {
                                me.RemoveCommentFromCanvas(args.OldItems.Cast<IComment>());
                            }
                        }
                        catch (Exception) { }
                    });
                });
        }
        else if (e.NewValue is IObservableVector<object> ov)
        {
            ov.VectorChanged += me.Ov_VectorChanged;
        }

        if (e.OldValue is IObservableVector<object> oldOV)
        {
            oldOV.VectorChanged -= me.Ov_VectorChanged;
        }
    }

    private void Ov_VectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs args)
    {
        if (args.CollectionChange == CollectionChange.Reset)
        {
            ResetComments();
        }
        else if (args.CollectionChange == CollectionChange.ItemInserted)
        {
            AddOrPushPending(sender[(int)args.Index] as IComment);
        }
        else if (args.CollectionChange == CollectionChange.ItemRemoved)
        {
            RemoveCommentFromCanvas(sender[(int)args.Index] as IComment);
        }

    }

    private static void OnDefaultDisplayDurationChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var me = (CommentRenderer)sender;
    }



    #endregion

    private CancellationTokenSource _scrollCommentAnimationCts;
    private CancellationToken GetScrollCommentAnimationCancellationToken()
    {
        _scrollCommentAnimationCts ??= new CancellationTokenSource();
        return _scrollCommentAnimationCts.Token;
    }
    private void StopScrollCommentAnimation()
    {
        if (_scrollCommentAnimationCts != null)
        {
            try
            {
                _scrollCommentAnimationCts.Cancel();
                _scrollCommentAnimationCts.Dispose();
            }
            catch (ObjectDisposedException)
            {

            }
            finally
            {
                _scrollCommentAnimationCts = null;
            }
        }
    }



    class CommentRenderFrameData
    {
        public TimeSpan CurrentVpos { get; set; }// (uint)Math.Floor(VideoPosition.TotalMilliseconds * 0.1);
        public int CanvasWidth { get; set; }// (int)CommentCanvas.ActualWidth;
        public uint CanvasHeight { get; set; } //= (uint)CommentCanvas.ActualHeight;
        public double HalfCanvasWidth { get; set; } //= canvasWidth / 2;
        public float FontScale { get; set; } //= (float)CommentSizeScale;
        public Color CommentDefaultColor { get; set; } //= CommentDefaultColor;
        public Visibility Visibility { get; set; }
        public TimeSpan CommentDisplayDuration { get; internal set; }
        public float InverseCommentDisplayDurationInMs { get; internal set; }
        public MediaPlaybackState PlaybackState { get; set; }
        public float PlaybackRate { get; set; }
        public float PlaybackRateInverse { get; set; }
        public CancellationToken ScrollCommentAnimationCancelToken { get; set; }
    }

    const int OWNER_COMMENT_Z_INDEX = 1;

    const float BaseCommentSizeRatioByCanvasHeight = 1.0f / 15.0f;


    /// <summary>
    /// 各コメントの縦方向に追加される余白
    /// </summary>
    const float CommentVerticalMarginRatio = 0.55f;

    /// <summary>
    /// shita コマンドのコメントの下に追加する余白
    /// </summary>
    const int BottomCommentMargin = 16;

    /// <summary>
    /// テキストの影をどれだけずらすかの量
    /// 実際に使われるのはフォントサイズにTextBGOffsetBiasを乗算した値
    /// </summary>
    const double TextBGOffsetBias = 0.15;



    /// <summary>
    /// 流れるコメントの前コメを表示段ごとに管理するリスト
    /// </summary>
    private List<CommentUI> PrevRenderCommentEachLine_Stream = new List<CommentUI>();
    /// <summary>
    /// 上揃いコメントの前コメを表示段ごとに管理するリスト
    /// </summary>
    private List<CommentUI> PrevRenderCommentEachLine_Top = new List<CommentUI>();

    /// <summary>
    /// 下揃いコメントの前コメを表示段ごとに管理するリスト
    /// </summary>
    private List<CommentUI> PrevRenderCommentEachLine_Bottom = new List<CommentUI>();

    private void CommentRenderer_Loaded(object sender, RoutedEventArgs e)
    {
        _disposables = new CompositeDisposable();
        this.ObserveDependencyProperty(VisibilityProperty)
            .Subscribe(x =>
            {
                ResetComments();
            })
            .AddTo(_disposables);

        if (MediaPlayer != null)
        {
            MediaPlayer.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
            MediaPlayer.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;
            MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            MediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
        }

        _prevWindowSize = new Size(Window.Current.Bounds.Width, Window.Current.Bounds.Height);
        Window.Current.SizeChanged += WindowSizeChanged;

        Clip = new RectangleGeometry() { Rect = new Rect() { Width = ActualWidth, Height = ActualHeight } };
        this.SizeChanged += CommentRenderer_SizeChanged;                
    }

    private void CommentRenderer_Unloaded(object sender, RoutedEventArgs e)
    {
        Window.Current.SizeChanged -= WindowSizeChanged;
        this.SizeChanged -= CommentRenderer_SizeChanged;

        var mediaPlayer = MediaPlayer;
        if (mediaPlayer != null)
        {
            mediaPlayer.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
            mediaPlayer.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;
        }

        _disposables.Dispose();
        _disposables = null;
        _windowResizeTimer.Stop();
        StopScrollCommentAnimation();
        ClearComments();
    }

    bool _nowWindowSizeChanging;
    DispatcherQueueTimer _windowResizeTimer;
    Size _prevWindowSize;
    private void WindowSizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
    {
        if (_prevWindowSize == e.Size) { return; }

        _prevWindowSize = e.Size;

        Debug.WriteLine("Window SizeChanged");

        if (_nowWindowSizeChanging == false)
        {
            ClearComments();
        }

        _nowWindowSizeChanging = true;
        _windowResizeTimer.Start();
    }

    private void _windowResizeTimer_Tick(object sender, object e)
    {
        Debug.WriteLine("_windowResizeTimer_Tick");
        _nowWindowSizeChanging = false;

        ResetComments();
    }

    private TimeSpan CurrentVPos => VideoPosition + VideoPositionOffset;

    private void CommentRenderer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        Clip = new RectangleGeometry() { Rect = new Rect() { Width = ActualWidth, Height = ActualHeight } };

        Debug.WriteLine("CommentCanvas SizeChanged"); 
        
        if (_nowWindowSizeChanging) { return; }

        ResetComments();
    }

    private TimeSpan _prevPosition;
    private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (_nowWindowSizeChanging) { return; }

            var prev = PlaybackState;
            PlaybackState = sender?.PlaybackState ?? null;
            if (prev != PlaybackState)
            {
                Debug.WriteLine("state changed " + PlaybackState);
                ResetScrollCommentsAnimation(GetRenderFrameData());
            }

            TimeSpan currentVpos = CurrentVPos;
            if (Math.Abs((float)(currentVpos - _prevPosition).TotalSeconds) > 1.0f)
            {
                Debug.WriteLine("seeked! position changed");
                ResetComments();
            }
            else
            {
                // 表示期間を過ぎたコメントを削除
                for (int i = 0; i < CommentCanvas.Children.Count; i++)
                {
                    var comment = CommentCanvas.Children[i] as CommentUI;
                    if (comment.EndPosition <= currentVpos)
                    {
                        RemoveCommentFromCanvas(comment.Comment);
                        --i;
                    }
                    else
                    {
                        break;
                    }
                }

                // 追加待機中のコメントをチェック
                var now = DateTime.UtcNow;
                if (_nextTickCommentTiming < now)
                {
                    _nextTickCommentTiming = now + TimeSpan.FromMilliseconds(500);
                    int count = 0;
                    CommentRenderFrameData frame = null;
                    for (int i = 0; i < _pendingRenderComments.Count; i++)
                    {
                        if (count >= 25) { break; }
                        count++;

                        var comment = _pendingRenderComments[i];
                        if (comment.VideoPosition <= (currentVpos + TimeSpan.FromSeconds(1)))
                        {
                            frame ??= GetRenderFrameData();
                            _pendingRenderComments.RemoveAt(i);
                            --i;
                            AddCommentToCanvas(comment, frame);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            // コメントの再描画完了時間を前回位置として記録する
            // currentVposを使うとシーク判定の時間以上にコメント描画の処理に時間が掛かった場合に
            // コメント再描画のループが発生してしまう。
            _prevPosition = CurrentVPos;
        });
    }

    DateTime _nextTickCommentTiming = DateTime.MinValue;

    private MediaPlaybackState? PlaybackState = null;
    private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (MediaPlayer == null || MediaPlayer.Source == null)
            {
                PlaybackState = null;
                return;
            }

            var prev = PlaybackState;
            PlaybackState = sender?.PlaybackState ?? null;

            if (prev != PlaybackState)
            {
                Debug.WriteLine("state changed " + PlaybackState);
                ResetScrollCommentsAnimation(GetRenderFrameData());
            }
        });
    }

    void ResetScrollCommentsAnimation(in CommentRenderFrameData frame)
    {
        if (frame.PlaybackState == MediaPlaybackState.Playing)
        {
            StopScrollCommentAnimation();

            var ct = GetScrollCommentAnimationCancellationToken();
            foreach (var renderComment in CommentCanvas.Children.Cast<CommentUI>())
            {
                if (renderComment.DisplayMode == CommentDisplayMode.Scrolling)
                {
                    var comment = renderComment.Comment;
                    if (renderComment.EndPosition > frame.CurrentVpos)
                    {
                        var duration = (renderComment.EndPosition - frame.CurrentVpos) * frame.PlaybackRateInverse;
                        if (duration <= TimeSpan.Zero)
                        {
                            renderComment.Opacity = 0.0;
                            continue;
                        }

                        var ab = AnimationBuilder.Create()
                            .Translation(Axis.Y)
                            .NormalizedKeyFrames(b => b
                                .KeyFrame(0.0, renderComment.VerticalPosition))
                            .Translation(Axis.X,
                                from: renderComment.GetPosition(frame.CanvasWidth, frame.CurrentVpos),
                                to: -renderComment.TextWidth,
                                duration: duration,
                                easingType: EasingType.Linear
                            )
                            ;

                        ab.StartAsync(renderComment, ct);
                    }
                    else
                    {
                        renderComment.Opacity = 0.0;
                    }
                }
            }
        }
        else
        {
            StopScrollCommentAnimation();

            foreach (var renderComment in CommentCanvas.Children.Cast<CommentUI>())
            {
                if (renderComment.DisplayMode == CommentDisplayMode.Scrolling)
                {
                    // 現在時間での横位置を求める
                    // lerp 現在時間における位置の比率
                    //var val = renderComment.GetPosition(frame.CanvasWidth, frame.CurrentVpos);
                    //if (val.HasValue)
                    //{
                    //    renderComment.Translation((float)val.Value, duration: 0).Start();
                    //}
                    var posX = renderComment.GetPosition(frame.CanvasWidth, frame.CurrentVpos);
                    var ab = AnimationBuilder.Create()
                            .Translation(Axis.Y)
                            .NormalizedKeyFrames(b => b
                                .KeyFrame(0.0, renderComment.VerticalPosition))
                            .Translation(Axis.X)
                            .NormalizedKeyFrames(b => b
                                .KeyFrame(0.0, posX))
                            ;
                    ab.Start(renderComment);

                }
            }
        }
    }



    static CommentRenderFrameData _frameData = new CommentRenderFrameData();
    private CommentRenderFrameData GetRenderFrameData()
    {
        _frameData.CommentDisplayDuration = DefaultDisplayDuration;
        _frameData.InverseCommentDisplayDurationInMs = 1.0f / (float)DefaultDisplayDuration.TotalMilliseconds;
        _frameData.PlaybackState = MediaPlayer.PlaybackSession.PlaybackState;
        _frameData.CommentDefaultColor = CommentDefaultColor;
        _frameData.CurrentVpos = VideoPosition + VideoPositionOffset;
        _frameData.CanvasWidth = (int)CommentCanvas.ActualWidth;
        _frameData.CanvasHeight = (uint)CommentCanvas.ActualHeight;
        _frameData.HalfCanvasWidth = CommentCanvas.ActualWidth * 0.5;
        _frameData.FontScale = (float)CommentSizeScale;
        _frameData.Visibility = Visibility;
        _frameData.PlaybackRate = (float)MediaPlayer.PlaybackSession.PlaybackRate;
        _frameData.PlaybackRateInverse = 1f / (float)MediaPlayer.PlaybackSession.PlaybackRate;
        _frameData.ScrollCommentAnimationCancelToken = GetScrollCommentAnimationCancellationToken();
        
        return _frameData;
    }


    void ClearComments()
    {
        StopScrollCommentAnimation();

        foreach (var commentUI in CommentCanvas.Children.Cast<CommentUI>())
        {
            _commentUIObjectPool.Return(commentUI);
        }

        CommentCanvas.Children.Clear();
        _commentToRenderCommentMap.Clear();
        _pendingRenderComments.Clear();

        PrevRenderCommentEachLine_Stream.Clear();
        PrevRenderCommentEachLine_Top.Clear();
        PrevRenderCommentEachLine_Bottom.Clear();
    }

    private void ResetComments(CommentRenderFrameData data = null)
    {
        try
        {
            CommentCanvas.Visibility = Visibility.Collapsed;
            ClearComments();
            CommentCanvas.Visibility = Visibility.Visible;
            if (Visibility == Visibility.Visible)
            {
                if (Comments != null)
                {
                    var frame = data ?? GetRenderFrameData();
                    foreach (var comment in Comments)
                    {
                        AddOrPushPending(comment as IComment, frame);
                    }
                    _prevPosition = CurrentVPos;
                }
            }
        }
        finally
        {
            CommentCanvas.Visibility = Visibility.Visible;
        }
    }

    static double CulcCommentFontSize(IComment comment, CommentRenderFrameData frame)
    {
        // フォントサイズの計算
        // 画面サイズの10分の１＊ベーススケール＊フォントスケール
        const float PixelToPoint = 0.75f;
        const float SmallFontScaleWithPtP = 0.75f * PixelToPoint;
        const float BigFontScaleWithPtP = 1.25f * PixelToPoint;
        float commentFontScale;
        switch (comment.SizeMode)
        {
            case CommentSizeMode.Big:
                commentFontScale = BigFontScaleWithPtP;
                break;
            case CommentSizeMode.Small:
                commentFontScale = SmallFontScaleWithPtP;
                break;
            default:
                commentFontScale = PixelToPoint;
                break;
        }

        var baseSize = Math.Max(frame.CanvasHeight * BaseCommentSizeRatioByCanvasHeight, 24);
        return baseSize * frame.FontScale * commentFontScale;
    }

    
    private void AddCommentToCanvas(IComment comment, CommentRenderFrameData frame)
    {
        CommentUI MakeCommentUI(IComment comment, CommentRenderFrameData frame)
        {
            var commentFontSize = CulcCommentFontSize(comment, frame);

            // コメントカラー
            Color commentColor = default(Color);
            if (comment.Color == null)
            {
                commentColor = frame.CommentDefaultColor;
            }
            else
            {
                commentColor = comment.Color.Value;
            }

            var textBGOffset = Math.Floor(FontSize * TextBGOffsetBias);

            var commentUI = _commentUIObjectPool.Get();
            {
                commentUI.Comment = comment;
                commentUI.CommentText = comment.CommentText_Transformed;
                commentUI.TextColor = commentColor;
                commentUI.BackTextColor = GetShadowColor(commentColor);
                commentUI.VideoPosition = comment.VideoPosition;
                commentUI.EndPosition = comment.VideoPosition + frame.CommentDisplayDuration;
                commentUI.TextBGOffsetX = textBGOffset;
                commentUI.TextBGOffsetY = textBGOffset;
                commentUI.CommentFontSize = commentFontSize;
                commentUI.IsVisible = !comment.IsInvisible;
                commentUI.DisplayMode = comment.DisplayMode;
                commentUI.CommentDisplayDuration = frame.CommentDisplayDuration;
                commentUI.InverseCommentDisplayDurationInMs= frame.InverseCommentDisplayDurationInMs;
            }

            return commentUI;
        }

        if (_commentToRenderCommentMap.ContainsKey(comment)) { return; }

        // 表示区間を過ぎたコメントは表示しない
        if (comment.VideoPosition + frame.CommentDisplayDuration <= frame.CurrentVpos)
        {
            return;
        }

        comment.ApplyCommands();

        if (comment.IsInvisible) { return; }


        
        // 表示対象に登録
        var renderComment = MakeCommentUI(comment, frame);

        CommentCanvas.Children.Add(renderComment);
        _commentToRenderCommentMap.Add(comment, renderComment);
        renderComment.UpdateLayout();

        // 初期の縦・横位置を計算
        // 縦位置を計算して表示範囲外の場合はそれぞれの表示縦位置での追加をこのフレーム内で打ち切る
        bool isOutBoundComment = false;
        if (comment.DisplayMode == CommentDisplayMode.Scrolling)
        {
            // 流れるコメントの縦位置を決定

            // 前に流れているコメントを走査して挿入可能な高さを判定していく
            // 前後のコメントが重複なく流せるかを求める
            int insertPosition = -1;
            double verticalPos = 8;
            var currentCommentReachLeftEdgeTime = renderComment.CalcReachLeftEdge(frame.CanvasWidth);
            for (var i = 0; i < PrevRenderCommentEachLine_Stream.Count; i++)
            {
                var prevComment = PrevRenderCommentEachLine_Stream[i];
                // 先行コメントのテキストが画面内に完全に収まっている場合
                // かつ
                // 追加したいコメントが画面左端に到達した時間が
                // 先行しているコメントの表示終了時間を超える場合
                // コリジョンしない

                if (prevComment?.Comment == null)
                {
                    // コリジョンしない
                    // 追加可能
                    insertPosition = i;
                    break;
                }
                else
                {
                    bool isCollisionLeftEdge = prevComment.EndPosition > currentCommentReachLeftEdgeTime;
                    bool isCollisionRightEdge = prevComment.CalcTextShowRightEdgeTime(frame.CanvasWidth) > comment.VideoPosition;
                    if (isCollisionLeftEdge is false && isCollisionRightEdge is false)
                    {
                        // コリジョンしない
                        // 追加可能
                        insertPosition = i;
                        break;
                    }
                    else
                    {
                        // コリジョンする
                        // 追加できない
                        verticalPos += prevComment.TextHeight + prevComment.TextHeight * CommentVerticalMarginRatio;
                    }
                }
            }

            // 画面下部に少しでも文字がはみ出るようなら範囲外
            isOutBoundComment = (verticalPos + renderComment.TextHeight) > frame.CanvasHeight;
            if (isOutBoundComment is false)
            {
                // 最初は右端に配置
                float initialCanvasLeft = renderComment.GetPosition(frame.CanvasWidth, frame.CurrentVpos);

                renderComment.Opacity = 1.0;
                renderComment.VerticalPosition = verticalPos;

                float posToMovingRatio = initialCanvasLeft / (frame.CanvasWidth + renderComment.TextWidth);
                TimeSpan displayDuration = (renderComment.EndPosition - frame.CurrentVpos) * frame.PlaybackRateInverse;
                if (displayDuration > TimeSpan.FromMilliseconds(1))
                {
                    if (frame.PlaybackState == MediaPlaybackState.Playing)
                    {
                        var ab = AnimationBuilder.Create()
                            .Translation(Axis.Y)
                            .NormalizedKeyFrames(b => b
                                .KeyFrame(0.0, renderComment.VerticalPosition))
                            .Translation(Axis.X,
                                from: (float)initialCanvasLeft,
                                to: -renderComment.TextWidth,
                                duration: displayDuration,
                                easingType: EasingType.Linear
                                );

                        ab.Start(renderComment, frame.ScrollCommentAnimationCancelToken);
                    }
                    else
                    {
                        var ab = AnimationBuilder.Create()
                           .Translation(Axis.Y)
                           .NormalizedKeyFrames(b => b
                               .KeyFrame(0.0, renderComment.VerticalPosition)
                               , duration: displayDuration
                               )
                           .Translation(Axis.X)
                           .NormalizedKeyFrames(b => b
                               .KeyFrame(0.0, (float)initialCanvasLeft)
                               , duration: displayDuration
                               )
                            ;

                        ab.Start(renderComment);
                    }
                }

                if (insertPosition == -1)
                {
                    // 最後尾に追加
                    PrevRenderCommentEachLine_Stream.Add(renderComment);
                }
                else
                {
                    // 指定の位置に追加
                    PrevRenderCommentEachLine_Stream[insertPosition] = renderComment;
                }
            }
        }
        else
        {
            if (comment.DisplayMode == CommentDisplayMode.Top)
            {
                // 上に位置する場合の縦位置の決定
                int insertPosition = -1;
                double verticalPos = 8;
                for (var i = 0; i < PrevRenderCommentEachLine_Top.Count; i++)
                {
                    var prevComment = PrevRenderCommentEachLine_Top[i];
                    if (prevComment?.Comment == null)
                    {
                        insertPosition = i;
                        break;
                    }
                    else
                    {
                        verticalPos += prevComment.TextHeight + prevComment.TextHeight * CommentVerticalMarginRatio;
                    }
                }

                // 上コメが画面下部からはみ出す場合には範囲外
                isOutBoundComment = (verticalPos + renderComment.TextHeight) > frame.CanvasHeight;
                if (isOutBoundComment is false)
                {
                    renderComment.VerticalPosition = verticalPos;
                    var left = (float)frame.HalfCanvasWidth - renderComment.TextWidth * 0.5f;
                    AnimationBuilder.Create()
                       .Translation(Axis.Y)
                       .NormalizedKeyFrames(b => b
                           .KeyFrame(0.0, renderComment.VerticalPosition))
                       .Translation(Axis.X)
                       .NormalizedKeyFrames(b => b
                           .KeyFrame(0.0, left))
                       .Start(renderComment);

                    if (insertPosition == -1)
                    {
                        // 最後尾に追加
                        PrevRenderCommentEachLine_Top.Add(renderComment);
                    }
                    else
                    {
                        // 指定の位置に追加
                        PrevRenderCommentEachLine_Top[insertPosition] = renderComment;
                    }
                }
            }
            else if (comment.DisplayMode == CommentDisplayMode.Bottom)
            {
                // 下に位置する場合の縦位置の決定
                int insertPosition = -1;
                double verticalPos = frame.CanvasHeight - renderComment.TextHeight - BottomCommentMargin;
                for (var i = 0; i < PrevRenderCommentEachLine_Bottom.Count; i++)
                {
                    var prevComment = PrevRenderCommentEachLine_Bottom[i];
                    if (prevComment?.Comment == null)
                    {
                        insertPosition = i;
                        break;
                    }
                    else
                    {
                        verticalPos -= prevComment.TextHeight + prevComment.TextHeight * CommentVerticalMarginRatio;
                    }
                }

                // 下コメが画面上部からはみ出す場合には範囲外
                isOutBoundComment = verticalPos < 0;
                if (isOutBoundComment is false)
                {
                    renderComment.VerticalPosition = verticalPos;
                    var left = frame.HalfCanvasWidth - renderComment.TextWidth * 0.5f;
                    AnimationBuilder.Create()
                       .Translation(Axis.Y)
                       .NormalizedKeyFrames(b => b
                           .KeyFrame(0.0, renderComment.VerticalPosition))
                       .Translation(Axis.X)
                       .NormalizedKeyFrames(b => b
                           .KeyFrame(0.0, left))
                       .Start(renderComment);

                    if (insertPosition == -1)
                    {
                        // 最後尾に追加
                        PrevRenderCommentEachLine_Bottom.Add(renderComment);
                    }
                    else
                    {
                        // 指定の位置に追加
                        PrevRenderCommentEachLine_Bottom[insertPosition] = renderComment;
                    }
                }
            }
            else //if (comment.VAlign == VerticalAlignment.Center)
            {
                renderComment.Translation = new((float)frame.HalfCanvasWidth - renderComment.TextWidth * 0.5f, frame.CanvasHeight * 0.5f - renderComment.TextHeight * 0.5f, 0);
            }

            // オーナーコメントの場合は優先して表示されるように
            if (comment.IsOwnerComment)
            {
                Canvas.SetZIndex(renderComment, OWNER_COMMENT_Z_INDEX);
            }
        }

        if (isOutBoundComment)
        {
            // 追加してしまったRenderComments等からは削除しておく
            RemoveCommentFromCanvas(comment);
        }
    }

    private readonly List<IComment> _pendingRenderComments = new ();
    private readonly Dictionary<IComment, CommentUI> _commentToRenderCommentMap = new Dictionary<IComment, CommentUI>();


    private void AddOrPushPending(IComment comment, CommentRenderFrameData frame = null)
    {
        if (_nowWindowSizeChanging || comment.VideoPosition > VideoPosition + VideoPositionOffset)
        {
            if (_pendingRenderComments.Any())
            {
                int i = 0;
                bool isAdded = false;
                foreach (var item in _pendingRenderComments)
                {
                    if (item.VideoPosition >= comment.VideoPosition)
                    {
                        _pendingRenderComments.Insert(i, comment);
                        isAdded = true;
                        break;
                    }
                    i++;
                }

                if (isAdded is false)
                {
                    _pendingRenderComments.Add(comment);
                }
            }
            else
            {
                _pendingRenderComments.Add(comment);
            }
        }
        else
        {
            AddCommentToCanvas(comment, frame ?? GetRenderFrameData());
        }
    }

    private void AddCommentToCanvas(IEnumerable<IComment> comments, CommentRenderFrameData frame = null)
    {
        if (Visibility ==Visibility.Collapsed) { return; }
        if (comments.Any() is false) { return; }

        var frameData = frame ?? GetRenderFrameData();
        foreach (var comment in comments)
        {
            AddOrPushPending(comment, frameData);
            frameData.CurrentVpos = VideoPosition + VideoPositionOffset;
        }
    }

    private void RemoveCommentFromCanvas(IEnumerable<IComment> comments)
    {
        foreach (var comment in comments)
        {
            RemoveCommentFromCanvas(comment);
        }
    }



    private void RemoveCommentFromCanvas(IComment comment)
    {
        if (_commentToRenderCommentMap.Remove(comment, out var renderComment))
        {
            CommentCanvas.Children.Remove(renderComment);
            _commentUIObjectPool.Return(renderComment);

            for (int i = 0; i < PrevRenderCommentEachLine_Stream.Count; i++)
            {
                if (PrevRenderCommentEachLine_Stream[i] == renderComment)
                {
                    PrevRenderCommentEachLine_Stream[i] = null;
                }
            }
        }

        _pendingRenderComments.Remove(comment);
    }


    private Dictionary<Color, Color> _FontShadowColorMap = new Dictionary<Color, Color>();

    /// <summary>
    /// 色から輝度を求めて輝度を反転させて影色とする
    /// </summary>
    /// <param name="sourceColor"></param>
    /// <returns></returns>
    private Color GetShadowColor(Color sourceColor)
    {
        if (_FontShadowColorMap.ContainsKey(sourceColor))
        {
            return _FontShadowColorMap[sourceColor];
        }
        else
        {
            var baseColor = sourceColor;
            byte c = (byte)(byte.MaxValue - (byte)(0.299f * baseColor.R + 0.587f * baseColor.G + 0.114f * baseColor.B));

            // 赤や黄色など多少再度が高い色でも黒側に寄せるよう
            // 127ではなく196をしきい値に利用
            c = c > 196 ? byte.MaxValue : byte.MinValue;

            var shadowColor = new Color()
            {
                R = c,
                G = c,
                B = c,
                A = byte.MaxValue
            };

            _FontShadowColorMap.Add(sourceColor, shadowColor);
            return shadowColor;
        }
    }





   

}
