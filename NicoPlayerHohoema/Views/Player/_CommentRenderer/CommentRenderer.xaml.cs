using Microsoft.Toolkit.Uwp.UI.Animations;
using NicoPlayerHohoema.Models.Helpers;
using NicoPlayerHohoema.Models.Niconico;
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
using NicoPlayerHohoema.Models;
using Prism.Unity;
using System.Text.RegularExpressions;
using System.Reactive.Disposables;
using NicoPlayerHohoema.Models.Niconico.Video;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using Reactive.Bindings.Extensions;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace NicoPlayerHohoema.Views
{
    public sealed partial class CommentRenderer : UserControl
    {
        public CommentRenderer()
        {
            this.InitializeComponent();

            Loaded += CommentRenderer_Loaded;
            Unloaded += CommentRenderer_Unloaded;
            this.ObserveDependencyProperty(VisibilityProperty)
                .Subscribe(x => 
                {
                    _ = ResetCommentsAsyncSafe();
                });
        }

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
                    oldMediaPlayer.SourceChanged -= me.MediaPlayer_SourceChanged;
                }
            }
            else
            {
                me.MediaPlayer.PlaybackSession.PlaybackStateChanged += me.PlaybackSession_PlaybackStateChanged;
                me.MediaPlayer.SourceChanged += me.MediaPlayer_SourceChanged;
            }
        }

        bool _PlayerCanSeek = false;
        private async void MediaPlayer_SourceChanged(MediaPlayer sender, object args)
        {
            using (var releaser = await _UpdateLock.LockAsync())
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    if (MediaPlayer == null || MediaPlayer.Source == null)
                    {
                        _PlayerCanSeek = false;
                        return;
                    }

                    _PlayerCanSeek = sender.PlaybackSession.CanSeek;
                });
            }
        }

        private MediaPlaybackState? PlaybackState = null;
        private async void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                using (var releaser = await _UpdateLock.LockAsync())
                {
                    if (MediaPlayer == null || MediaPlayer.Source == null)
                    {
                        PlaybackState = null;
                        return;
                    }

                    PlaybackState = sender?.PlaybackState ?? null;

                    if (PlaybackState == MediaPlaybackState.Playing)
                    {
                        var frame = GetRenderFrameData();
                        foreach (var renderComment in CommentCanvas.Children.Cast<CommentUI>())
                        {
                            if (renderComment.DisplayMode == CommentDisplayMode.Scrolling)
                            {
                                var comment = renderComment.DataContext as IComment;
                                if (_animationSetMap.TryGetValue(renderComment.DataContext as IComment, out var anim))
                                {
                                    anim.SetDuration((renderComment.EndPosition - frame.CurrentVpos) * 10 * frame.PlaybackRateInverse);
                                    anim.Start();
                                }
                                else
                                {
                                    anim = renderComment.Offset(
                                        -renderComment.TextWidth,
                                        (float)renderComment.VerticalPosition,
                                        duration: (renderComment.EndPosition - frame.CurrentVpos) * 10 * frame.PlaybackRateInverse,
                                        easingType: EasingType.Linear
                                        );
                                    anim.Start();
                                    _animationSetMap.Add(comment, anim);
                                }
                            }
                        }
                    }
                    else
                    {
                        var frame = GetRenderFrameData();
                        foreach (var renderComment in CommentCanvas.Children.Cast<CommentUI>())
                        {
                            if (renderComment.DisplayMode == CommentDisplayMode.Scrolling)
                            {
                                // 現在時間での横位置を求める
                                // lerp 現在時間における位置の比率
                                //var val = renderComment.GetPosition(frame.CanvasWidth, frame.CurrentVpos);
                                //if (val.HasValue)
                                //{
                                //    renderComment.Offset((float)val.Value, duration: 0).Start();
                                //}

                                if (_animationSetMap.TryGetValue(renderComment.DataContext as IComment, out var anim))
                                {
                                    anim.Stop();
                                }
                            }
                        }
                    }
                }
            });
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
                    .Subscribe(async args =>
                    {
                        await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => 
                        {
                            if (args.Action == NotifyCollectionChangedAction.Reset)
                            {
                                await me.ResetCommentsAsyncSafe();
                            }
                            else if (args.Action == NotifyCollectionChangedAction.Add)
                            {
                                await me.AddCommentToCanvasAsyncSafe(args.NewItems.Cast<IComment>());
                            }
                            else if (args.Action == NotifyCollectionChangedAction.Remove)
                            {
                                await me.RemoveCommentFromCanvasAsyncSafe(args.OldItems.Cast<IComment>());
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

        private async void Ov_VectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs args)
        {
            if (args.CollectionChange == CollectionChange.Reset)
            {
                await ResetCommentsAsyncSafe();
            }
            else if (args.CollectionChange == CollectionChange.ItemInserted)
            {
                await AddCommentToCanvasAsyncSafe(sender[(int)args.Index] as IComment);
            }
            else if (args.CollectionChange == CollectionChange.ItemRemoved)
            {
                await RemoveCommentFromCanvasAsyncSafe(sender[(int)args.Index] as IComment);
            }

        }

        private static void OnDefaultDisplayDurationChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var me = (CommentRenderer)sender;
        }



        #endregion




        struct CommentRenderFrameData
        {
            public uint CurrentVpos { get; set; }// (uint)Math.Floor(VideoPosition.TotalMilliseconds * 0.1);
            public int CanvasWidth { get; set; }// (int)CommentCanvas.ActualWidth;
            public uint CanvasHeight { get; set; } //= (uint)CommentCanvas.ActualHeight;
            public double HalfCanvasWidth { get; set; } //= canvasWidth / 2;
            public float FontScale { get; set; } //= (float)CommentSizeScale;
            public Color CommentDefaultColor { get; set; } //= CommentDefaultColor;
            public Visibility Visibility { get; set; }
            public uint CommentDisplayDurationVPos { get; internal set; }
            public TimeSpan CommentDisplayDuration { get; internal set; }
            public MediaPlaybackState PlaybackState { get; set; }
            public double PlaybackRate { get; set; }
            public double PlaybackRateInverse { get; set; }
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

        CommentUI PrevRenderComment_Center;


        Dictionary<IComment, AnimationSet> _animationSetMap = new Dictionary<IComment, AnimationSet>();

        private async void CommentRenderer_Loaded(object sender, RoutedEventArgs e)
        {
            using (var releaser = await _UpdateLock.LockAsync())
            {
                if (MediaPlayer != null)
                {
                    MediaPlayer.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
                    MediaPlayer.SourceChanged -= MediaPlayer_SourceChanged;
                    MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
                    MediaPlayer.SourceChanged += MediaPlayer_SourceChanged;
                }

                Clip = new RectangleGeometry() { Rect = new Rect() { Width = ActualWidth, Height = ActualHeight } };
                this.SizeChanged += CommentRenderer_SizeChanged;                
            }
        }

        private async void CommentRenderer_SizeChanged(object sender, SizeChangedEventArgs e)
        {

            using (var releaser = await _UpdateLock.LockAsync())
            {
                Clip = new RectangleGeometry() { Rect = new Rect() { Width = ActualWidth, Height = ActualHeight } };
            }

            await ResetCommentsAsyncSafe();
        }


        private async void CommentRenderer_Unloaded(object sender, RoutedEventArgs e)
        {
            this.SizeChanged -= CommentRenderer_SizeChanged;

            using (await _UpdateLock.LockAsync())
            {
                foreach (var renderComment in CommentCanvas.Children.Cast<CommentUI>())
                {
                    renderComment.Offset(0).Fade(0).SetDurationForAll(0).Start();
                }

                CommentCanvas.Children.Clear();
            }

            var mediaPlayer = MediaPlayer;
            if (mediaPlayer != null)
            {
                mediaPlayer.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
                mediaPlayer.SourceChanged -= MediaPlayer_SourceChanged;
            }
        }



        TimeSpan _PreviousVideoPosition = TimeSpan.Zero;
        AsyncLock _UpdateLock = new AsyncLock();
        TimeSpan _PrevCommentRenderElapsedTime = TimeSpan.Zero;

        DateTime _RealVideoPosition = DateTime.Now;

        private CommentRenderFrameData GetRenderFrameData()
        {
            return new CommentRenderFrameData()
            {
                CommentDisplayDuration = DefaultDisplayDuration
                , PlaybackState = MediaPlayer.PlaybackSession.PlaybackState
                , CommentDefaultColor = CommentDefaultColor
                , CurrentVpos = (uint)Math.Floor((VideoPosition + VideoPositionOffset).TotalMilliseconds * 0.1)
                , CanvasWidth = (int)CommentCanvas.ActualWidth
                , CanvasHeight = (uint)CommentCanvas.ActualHeight
                , HalfCanvasWidth = CommentCanvas.ActualWidth * 0.5
                , FontScale = (float)CommentSizeScale
                , CommentDisplayDurationVPos = GetCommentDisplayDurationVposUnit()
                , Visibility = Visibility
                , PlaybackRate = MediaPlayer.PlaybackSession.PlaybackRate
                , PlaybackRateInverse = 1d / MediaPlayer.PlaybackSession.PlaybackRate
            };
        }




        private async Task ResetCommentsAsyncSafe()
        {
            try
            {
                CommentCanvas.Visibility = Visibility.Collapsed;

                using (await _UpdateLock.LockAsync())
                {
                    Debug.WriteLine("Comment Reset");

                    foreach (var anim in _animationSetMap.Values)
                    {
                        anim.Stop();
                        anim.Dispose();
                    }

                    _animationSetMap.Clear();
                    CommentCanvas.Children.Clear();
                    _commentToRenderCommentMap.Clear();

                    PrevRenderCommentEachLine_Stream.Clear();
                    PrevRenderCommentEachLine_Top.Clear();
                    PrevRenderCommentEachLine_Bottom.Clear();

                    _RealVideoPosition = DateTime.Now;
                }

                CommentCanvas.Visibility = Visibility.Visible;
                if (Visibility == Visibility.Visible)
                {
                    if (Comments != null)
                    {
                        await Task.Delay(10);

                        await AddCommentToCanvasAsyncSafe(Comments.Cast<IComment>().ToArray());
                    }
                }

            }
            finally
            {
                CommentCanvas.Visibility = Visibility.Visible;
            }
        }



        private void AddCommentToCanvas(IComment comment, in CommentRenderFrameData frame)
        {
            CommentUI MakeCommentUI(IComment comment, in CommentRenderFrameData frame)
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
                var scaledFontSize = baseSize * frame.FontScale * commentFontScale * PixelToPoint;
                var commentFontSize = (uint)Math.Ceiling(scaledFontSize);

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

                var commentUI = new CommentUI()
                {
                    DataContext = comment,
                    CommentText = comment.CommentText_Transformed,
                    TextColor = commentColor,
                    BackTextColor = GetShadowColor(commentColor),
                    VideoPosition = comment.VideoPosition,
                    EndPosition = comment.VideoPosition + frame.CommentDisplayDurationVPos,
                    TextBGOffsetX = textBGOffset,
                    TextBGOffsetY = textBGOffset,
                    CommentFontSize = commentFontSize,
                    IsVisible = !comment.IsInvisible,
                    DisplayMode = comment.DisplayMode
                };

                return commentUI;
            }

            if (_commentToRenderCommentMap.ContainsKey(comment)) { return; }

            // 表示区間を過ぎたコメントは表示しない
            if (comment.VideoPosition + frame.CommentDisplayDurationVPos < frame.CurrentVpos)
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

                    if (prevComment == null
                        || prevComment.DataContext == null
                        )
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
                    }

                    // コリジョンする
                    // 追加できない
                    verticalPos += prevComment.TextHeight + prevComment.TextHeight * CommentVerticalMarginRatio;
                }

                // 画面下部に少しでも文字がはみ出るようなら範囲外
                isOutBoundComment = (verticalPos + renderComment.TextHeight) > frame.CanvasHeight;
                if (isOutBoundComment)
                {
//                    isCanAddRenderComment_Stream = false;
                }
                else
                {
                    // 最初は右端に配置
                    double initialCanvasLeft = renderComment.GetPosition(frame.CanvasWidth, frame.CurrentVpos) ?? frame.CanvasWidth;

                    renderComment.Opacity = 1.0;
                    renderComment.VerticalPosition = verticalPos;

                    if (frame.PlaybackState != MediaPlaybackState.Paused)
                    {
                        double displayDuration = Math.Min(renderComment.EndPosition - frame.CurrentVpos, frame.CommentDisplayDurationVPos) * 10u * frame.PlaybackRateInverse;
                        //double delay = Math.Max((renderComment.EndPosition - frame.CurrentVpos - frame.CommentDisplayDurationVPos) * 10u * frame.PlaybackRateInverse, 0);
                        Debug.WriteLine($"{comment.CommentId}: left: {initialCanvasLeft}, width: {renderComment.ActualWidth}, top: {verticalPos}");

                        var anim = renderComment
                            .Offset((float)initialCanvasLeft, (float)verticalPos, duration: 0)
                            .Then()
                            .Offset(-(float)renderComment.TextWidth, (float)verticalPos, duration: displayDuration, easingType: EasingType.Linear);

                        anim.Start();

                        _animationSetMap.Add(comment, anim);
                    }
                    else
                    {
                        renderComment
                            .Offset((float)initialCanvasLeft, (float)verticalPos, duration: 0)
                            .Start();
                    }

                    //Canvas.SetTop(renderComment, verticalPos);

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

//                    isCanAddRenderComment_Stream = (verticalPos + (renderComment.TextHeight + renderComment.TextHeight * CommentVerticalMarginRatio)) < frame.CanvasHeight;
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
                        if (prevComment == null
                            || prevComment.EndPosition < frame.CurrentVpos)
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
                    if (isOutBoundComment)
                    {
//                        isCanAddRenderComment_Top = false;
                    }
                    else
                    {
                        renderComment.VerticalPosition = verticalPos;

                        //Canvas.SetTop(renderComment, verticalPos);
                        renderComment
                            .Offset((float)frame.HalfCanvasWidth - renderComment.TextWidth * 0.5f, (float)verticalPos, duration: 0)
                            .Start();

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

//                        isCanAddRenderComment_Top = (verticalPos + (renderComment.TextHeight + renderComment.TextHeight * CommentVerticalMarginRatio)) < frame.CanvasHeight;
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
                        if (prevComment == null
                            || prevComment.EndPosition < frame.CurrentVpos)
                        {
                            insertPosition = i;
                            break;
                        }
                        else
                        {
                            verticalPos -= (prevComment.TextHeight + prevComment.TextHeight * CommentVerticalMarginRatio);
                        }
                    }

                    // 下コメが画面上部からはみ出す場合には範囲外
                    isOutBoundComment = verticalPos < 0;
                    if (isOutBoundComment)
                    {
//                        isCanAddRenderComment_Bottom = false;
                    }
                    else
                    {
                        renderComment.VerticalPosition = verticalPos;

                        //Canvas.SetTop(renderComment, verticalPos);
                        renderComment
                            .Offset((float)frame.HalfCanvasWidth - renderComment.TextWidth * 0.5f, (float)verticalPos, duration: 0)
                            .Start();

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

//                        isCanAddRenderComment_Bottom = (verticalPos - (renderComment.TextHeight + renderComment.TextHeight * CommentVerticalMarginRatio)) > 0;
                    }
                }
                else //if (comment.VAlign == VerticalAlignment.Center)
                {
                    renderComment
                            .Offset((float)frame.HalfCanvasWidth - renderComment.TextWidth * 0.5f, frame.CanvasHeight * 0.5f - renderComment.TextHeight * 0.5f, duration: 0)
                            .Start();
//                    Canvas.SetTop(renderComment, frame.CanvasHeight * 0.5f - renderComment.TextHeight * 0.5f);
                    PrevRenderComment_Center = renderComment;
//                    isCanAddRenderComment_Center = false;
                }

                // オーナーコメントの場合は優先して表示されるように
                if (comment.IsOwnerComment)
                {
                    Canvas.SetZIndex(renderComment, OWNER_COMMENT_Z_INDEX);
                }

                if (!isOutBoundComment)
                {
                    //var left = (float)frame.HalfCanvasWidth - (int)(renderComment.TextWidth * 0.5f);
                    //renderComment.Offset(offsetX: left, duration: 0).Start();
                }
            }

            if (isOutBoundComment)
            {
                // 追加してしまったRenderComments等からは削除しておく
                RemoveCommentFromCanvas(comment);
            }
        }



        private async Task AddCommentToCanvasAsyncSafe(IComment comment)
        {
            using (await _UpdateLock.LockAsync())
            {
                if (Visibility == Visibility.Collapsed) { return; }

                var frame = GetRenderFrameData();
                AddCommentToCanvas(comment, in frame);
            }
        }

        private async Task AddCommentToCanvasAsyncSafe(IEnumerable<IComment> comments)
        {
            using (await _UpdateLock.LockAsync())
            {
                if (Visibility ==Visibility.Collapsed) { return; }

                var frame = GetRenderFrameData();
                foreach (var comment in comments)
                {
                    AddCommentToCanvas(comment, in frame);
                    frame.CurrentVpos = (uint)Math.Floor((VideoPositionOffset).TotalMilliseconds * 0.1);
                }
            }
        }

        private async Task RemoveCommentFromCanvasAsyncSafe(IComment comment)
        {
            using (await _UpdateLock.LockAsync())
            {
                RemoveCommentFromCanvas(comment);
            }
        }

        private async Task RemoveCommentFromCanvasAsyncSafe(IEnumerable<IComment> comments)
        {
            using (await _UpdateLock.LockAsync())
            {
                foreach (var comment in comments)
                {
                    RemoveCommentFromCanvas(comment);
                }
            }
        }



        Dictionary<IComment, CommentUI> _commentToRenderCommentMap = new Dictionary<IComment, CommentUI>();
        private void RemoveCommentFromCanvas(IComment comment)
        {
            if (_animationSetMap.Remove(comment, out var anim))
            {
                anim.Stop();
                anim.Dispose();
            }

            if (_commentToRenderCommentMap.Remove(comment, out var renderComment))
            {
                CommentCanvas.Children.Remove(renderComment);
                renderComment.DataContext = null;
                PrevRenderCommentEachLine_Stream.Remove(renderComment);
            }
        }


        public uint GetCommentDisplayDurationVposUnit()
        {
            return (uint)(DefaultDisplayDuration.TotalSeconds * 100);
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
