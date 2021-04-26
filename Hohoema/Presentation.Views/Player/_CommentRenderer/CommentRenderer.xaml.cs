using Microsoft.Toolkit.Uwp.UI.Animations;
using Hohoema.Models.Domain.Helpers;
using Hohoema.Models.Domain.Niconico;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Unity;
using Hohoema.Models.Domain;
using Prism.Unity;
using System.Text.RegularExpressions;
using System.Reactive.Disposables;
using Hohoema.Models.Domain.Niconico.Video;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using Reactive.Bindings.Extensions;
using Hohoema.Models.Domain.Player;
using System.Numerics;
using Uno.Threading;
using Microsoft.Extensions.ObjectPool;
using System.Reactive;
using System.Reactive.Concurrency;
using Windows.System;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Hohoema.Presentation.Views
{
    sealed class CommentUIObjectPoolPolicy : IPooledObjectPolicy<CommentUI>
    {
        public CommentUI Create()
        {
            return new CommentUI();
        }

        public bool Return(CommentUI commentUI)
        {
            commentUI.Comment = null;
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
            
            _commentUIObjectPool = new DefaultObjectPool<CommentUI>(new CommentUIObjectPoolPolicy(), 500);

            _windowResizeTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            _windowResizeTimer.Interval = TimeSpan.FromSeconds(0.25);
            _windowResizeTimer.Tick += _windowResizeTimer_Tick;
            _windowResizeTimer.IsRepeating = false;
        }

        CompositeDisposable _disposables;

        DefaultObjectPool<CommentUI> _commentUIObjectPool;

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



        CancellationTokenSource _unloadedCts;
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


        private CancellationTokenSource _scrollCommentAnimationCts;

        private MediaPlaybackState? PlaybackState = null;
        private async void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (MediaPlayer == null || MediaPlayer.Source == null)
                {
                    PlaybackState = null;
                    return;
                }

                PlaybackState = sender?.PlaybackState ?? null;

                Debug.WriteLine("state changed " + PlaybackState);
                
                ResetScrollCommentsAnimation(GetRenderFrameData());
            });
        }

        void ResetScrollCommentsAnimation(in CommentRenderFrameData frame)
        {
            if (PlaybackState == MediaPlaybackState.Playing)
            {
                var ct = GetScrollCommentAnimationCancellationToken();
                foreach (var renderComment in CommentCanvas.Children.Cast<CommentUI>())
                {
                    if (renderComment.DisplayMode == CommentDisplayMode.Scrolling)
                    {
                        var comment = renderComment.Comment;
                        if (renderComment.EndPosition > frame.CurrentVpos)
                        {
                            
                            var ab = AnimationBuilder.Create()
                                .Translation(Axis.Y)
                                .NormalizedKeyFrames(b => b
                                    .KeyFrame(0.0, renderComment.VerticalPosition))
                                .Translation(Axis.X,
                                    from: renderComment.GetPosition(frame.CanvasWidth, frame.CurrentVpos),
                                    to: -renderComment.TextWidth,
                                    duration: (renderComment.EndPosition - frame.CurrentVpos) * frame.PlaybackRateInverse,
                                    easingType: EasingType.Linear
                                )
                                ;

                            _ = ab.StartAsync(renderComment, ct);
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
                        var ab = AnimationBuilder.Create()
                                .Translation(Axis.Y)
                                .NormalizedKeyFrames(b => b
                                    .KeyFrame(0.0, renderComment.VerticalPosition))
                                ;
                        ab.Start(renderComment);

                    }
                }
            }
        }

        IDisposable CommentItemsChangedSubscriber;
        private static void OnCommentsChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            CommentRenderer me = sender as CommentRenderer;

            me.CommentItemsChangedSubscriber?.Dispose();
            
            if (e.NewValue is INotifyCollectionChanged newNCC)
            {
                var dispatcher = me.Dispatcher;
                me.CommentItemsChangedSubscriber = newNCC.CollectionChangedAsObservable()
                    .Subscribe(args =>
                    {
                        _ = me.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => 
                        {
                            try
                            {
                                if (args.Action == NotifyCollectionChangedAction.Reset)
                                {
                                    Debug.WriteLine("Reset Comments");
                                    me.ResetComments();
                                }
                                else if (args.Action == NotifyCollectionChangedAction.Add)
                                {
                                    Debug.WriteLine("Add Comments");
                                    me.AddCommentToCanvas(args.NewItems.Cast<IComment>());
                                }
                                else if (args.Action == NotifyCollectionChangedAction.Remove)
                                {
                                    Debug.WriteLine("Remove Comments");
                                    me.RemoveCommentFromCanvas(args.OldItems.Cast<IComment>());
                                }
                            }
                            catch (Exception)
                            {
                            }

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
                AddCommentToCanvas(sender[(int)args.Index] as IComment);
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
            public MediaPlaybackState PlaybackState { get; set; }
            public double PlaybackRate { get; set; }
            public double PlaybackRateInverse { get; set; }
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

            _unloadedCts = new CancellationTokenSource();
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

        private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => 
            {
                if (_nowWindowSizeChanging) { return; }

                var frame = GetRenderFrameData();
                for (int i = _pendingRenderComments.Count - 1; i >= 0; i--)
                {
                    var comment = _pendingRenderComments[i];
                    if (comment.VideoPosition <= frame.CurrentVpos)
                    {
                        _pendingRenderComments.RemoveAt(i);
                        AddCommentToCanvas(comment, in frame);
                    }
                }
            });
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

        private void CommentRenderer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Clip = new RectangleGeometry() { Rect = new Rect() { Width = ActualWidth, Height = ActualHeight } };

            Debug.WriteLine("CommentCanvas SizeChanged"); 
            
            if (_nowWindowSizeChanging) { return; }

            ResetComments();
        }


        private void CommentRenderer_Unloaded(object sender, RoutedEventArgs e)
        {
            _disposables.Dispose();
            _disposables = null;
            Window.Current.SizeChanged -= WindowSizeChanged;
            this.SizeChanged -= CommentRenderer_SizeChanged;
            
            _windowResizeTimer.Stop();

            _unloadedCts.Cancel();
            _unloadedCts.Dispose();
            _unloadedCts = null;

            StopScrollCommentAnimation();

            CommentCanvas.Children.Clear();

            var mediaPlayer = MediaPlayer;
            if (mediaPlayer != null)
            {
                mediaPlayer.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
                mediaPlayer.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;
            }
        }


        static CommentRenderFrameData _frameData = new CommentRenderFrameData();
        private CommentRenderFrameData GetRenderFrameData()
        {
            _frameData.CommentDisplayDuration = DefaultDisplayDuration;
            _frameData.PlaybackState = MediaPlayer.PlaybackSession.PlaybackState;
            _frameData.CommentDefaultColor = CommentDefaultColor;
            _frameData.CurrentVpos = VideoPosition + VideoPositionOffset;
            _frameData.CanvasWidth = (int)CommentCanvas.ActualWidth;
            _frameData.CanvasHeight = (uint)CommentCanvas.ActualHeight;
            _frameData.HalfCanvasWidth = CommentCanvas.ActualWidth * 0.5;
            _frameData.FontScale = (float)CommentSizeScale;
            _frameData.Visibility = Visibility;
            _frameData.PlaybackRate = MediaPlayer.PlaybackSession.PlaybackRate;
            _frameData.PlaybackRateInverse = 1d / MediaPlayer.PlaybackSession.PlaybackRate;
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

            PrevRenderCommentEachLine_Stream.Clear();
            PrevRenderCommentEachLine_Top.Clear();
            PrevRenderCommentEachLine_Bottom.Clear();
        }

        private void ResetComments()
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
                        AddCommentToCanvas(Comments.Cast<IComment>().ToArray());
                    }
                }
            }
            finally
            {
                CommentCanvas.Visibility = Visibility.Visible;
            }
        }

        static double CulcCommentFontSize(IComment comment, in CommentRenderFrameData frame)
        {
            // フォントサイズの計算
            // 画面サイズの10分の１＊ベーススケール＊フォントスケール
            float commentFontScale = 1.0f;
            switch (comment.SizeMode)
            {
                case CommentSizeMode.Normal:
                    commentFontScale = 1.0f;
                    break;
                case CommentSizeMode.Big:
                    commentFontScale = 1.25f;
                    break;
                case CommentSizeMode.Small:
                    commentFontScale = 0.75f;
                    break;
                default:
                    break;
            }

            var baseSize = Math.Max(frame.CanvasHeight * BaseCommentSizeRatioByCanvasHeight, 24);
            const float PixelToPoint = 0.75f;
            return baseSize * frame.FontScale * commentFontScale * PixelToPoint;
        }

        private void AddCommentToCanvas(IComment comment, in CommentRenderFrameData frame)
        {
            CommentUI MakeCommentUI(IComment comment, in CommentRenderFrameData frame)
            {
                var commentFontSize = CulcCommentFontSize(comment, in frame);

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
                }

                return commentUI;
            }

            if (_commentToRenderCommentMap.ContainsKey(comment)) { return; }

            // 表示区間を過ぎたコメントは表示しない
            if (comment.VideoPosition + frame.CommentDisplayDuration <= frame.CurrentVpos)
            {
                return;
            }

            if (comment.IsInvisible) { return; }


            
            // 表示対象に登録
            var renderComment = MakeCommentUI(comment, in frame);

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
                        var leftEdge = prevComment.EndPosition < currentCommentReachLeftEdgeTime;
                        var rightEdge = prevComment.CalcTextShowRightEdgeTime(frame.CanvasWidth) < frame.CurrentVpos;

                        if (leftEdge && rightEdge)
                        {
                            // コリジョンしない
                            // 追加可能
                            insertPosition = i;
                            break;
                        }

                        // コリジョンする
                        // 追加できない
                        verticalPos += prevComment.TextHeight + prevComment.TextHeight * CommentVerticalMarginRatio;
                    }
                }

                // 画面下部に少しでも文字がはみ出るようなら範囲外
                isOutBoundComment = (verticalPos + renderComment.TextHeight) > frame.CanvasHeight;
                if (isOutBoundComment is false)
                {
                    // 最初は右端に配置
                    double initialCanvasLeft = renderComment.GetPosition(frame.CanvasWidth, frame.CurrentVpos) ?? frame.CanvasWidth;

                    renderComment.Opacity = 1.0;
                    renderComment.VerticalPosition = verticalPos;

                    if (frame.PlaybackState != MediaPlaybackState.Paused)
                    {
                        double displayDuration = Math.Min(renderComment.EndPosition.TotalMilliseconds - frame.CurrentVpos.TotalMilliseconds, frame.CommentDisplayDuration.TotalMilliseconds) * frame.PlaybackRateInverse;
                        var ab = AnimationBuilder.Create()
                            .Translation(Axis.Y)
                            .NormalizedKeyFrames(b => b
                                .KeyFrame(0.0, renderComment.VerticalPosition))
                            .Translation(Axis.X,
                                from: (float)initialCanvasLeft,
                                to: -renderComment.TextWidth,                            
                                duration: TimeSpan.FromMilliseconds(displayDuration),
                                easingType: EasingType.Linear
                                );

                        _ = ab.StartAsync(renderComment, frame.ScrollCommentAnimationCancelToken);
                    }
                    else
                    {
                        var ab = AnimationBuilder.Create()
                           .Translation(Axis.Y)
                           .NormalizedKeyFrames(b => b
                               .KeyFrame(0.0, renderComment.VerticalPosition))
                           .Translation(Axis.X)
                           .NormalizedKeyFrames(b => b
                               .KeyFrame(0.0, (float)initialCanvasLeft))
                            ;

                        ab.Start(renderComment);
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

        List<IComment> _pendingRenderComments = new List<IComment>();


        private void AddOrPushPending(IComment comment, in CommentRenderFrameData frame)
        {
            if (_nowWindowSizeChanging || comment.VideoPosition > frame.CurrentVpos)
            {
                _pendingRenderComments.Add(comment);
            }
            else
            {
                AddCommentToCanvas(comment, in frame);
            }
        }

        private void AddCommentToCanvas(IComment comment)
        {
            if (Visibility == Visibility.Collapsed) { return; }

            var frame = GetRenderFrameData();
            AddOrPushPending(comment, in frame);
        }

        private void AddCommentToCanvas(IEnumerable<IComment> comments)
        {
            if (Visibility ==Visibility.Collapsed) { return; }

            var frame = GetRenderFrameData();
            foreach (var comment in comments)
            {
                AddOrPushPending(comment, in frame);
                frame.CurrentVpos = VideoPosition + VideoPositionOffset;
            }
        }

        private void RemoveCommentFromCanvas(IEnumerable<IComment> comments)
        {
            foreach (var comment in comments)
            {
                RemoveCommentFromCanvas(comment);
            }
        }



        Dictionary<IComment, CommentUI> _commentToRenderCommentMap = new Dictionary<IComment, CommentUI>();
        private void RemoveCommentFromCanvas(IComment comment)
        {
            if (_commentToRenderCommentMap.Remove(comment, out var renderComment))
            {
                CommentCanvas.Children.Remove(renderComment);
                if (renderComment.DisplayMode == CommentDisplayMode.Scrolling)
                {
                    PrevRenderCommentEachLine_Stream.Remove(renderComment);
                }

                _commentUIObjectPool.Return(renderComment);
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

}
