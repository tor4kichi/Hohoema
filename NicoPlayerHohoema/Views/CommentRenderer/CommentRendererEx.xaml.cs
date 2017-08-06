using NicoPlayerHohoema.Util;
using System;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace NicoPlayerHohoema.Views.CommentRenderer
{
    public sealed partial class CommentRendererEx : UserControl
    {

        class CommentRenderFrameData
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
            public float CommentDisplayDurationSecondsDividedOne { get; internal set; }
        }


        class CommentRenderInfo
        {
            public Comment Comment { get; set; }
            public CommentUI CommentUI { get; set; }
            public float MoveSpeedPixelPerSec { get; set; }
            public bool IsSkipUpdate { get; set; }
            public double PrevHorizontalPosition { get; set; }

        }

       
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


        private Stack<CommentRenderInfo> CommentUICached = new Stack<CommentRenderInfo>();


        /// <summary>
        /// 描画待ちのコメントリスト
        /// 現在時間を過ぎたコメントをここから払い出していく
        /// </summary>
        private Queue<Comment> RenderPendingComments = new Queue<Comment>();

        /// <summary>
        /// 現在表示中のコメント
        /// </summary>
        private List<CommentRenderInfo> RenderComments = new List<CommentRenderInfo>();


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




        public CommentRendererEx()
        {
            this.InitializeComponent();

            Unloaded += CommentRendererEx_Unloaded;
            Application.Current.EnteredBackground += Current_EnteredBackground;
            Application.Current.LeavingBackground += Current_LeavingBackground;

        }

        private void Current_LeavingBackground(object sender, Windows.ApplicationModel.LeavingBackgroundEventArgs e)
        {
            ResetUpdateTimer();
        }

        private void Current_EnteredBackground(object sender, Windows.ApplicationModel.EnteredBackgroundEventArgs e)
        {
            _UpdateTimer?.Dispose();
            _UpdateTimer = null;
        }

        private void CommentRendererEx_Unloaded(object sender, RoutedEventArgs e)
        {
            Application.Current.EnteredBackground -= Current_EnteredBackground;
            Application.Current.LeavingBackground -= Current_LeavingBackground;
            _UpdateTimer?.Dispose();
        }

        private bool _IsNeedCommentRenderUpdated = false;
        private void ResetComments(CommentRenderFrameData frame)
        {
            foreach (var renderComment in RenderComments)
            {
                CommentUICached.Push(renderComment);
            }

            RenderPendingComments.Clear();
            RenderComments.Clear();
            CommentCanvas.Children.Clear();

            PrevRenderCommentEachLine_Stream.Clear();
            PrevRenderCommentEachLine_Top.Clear();
            PrevRenderCommentEachLine_Bottom.Clear();

            // 現在時間-コメント表示時間から始まるコメントを描画待機コメントとして再配置
            var currentVideoPos = frame.CurrentVpos;
            var comments = new List<Comment>(Comments);
            comments.Sort((x, y) => (int)(x.VideoPosition - y.VideoPosition));

            foreach (var c in comments)
            {
                RenderPendingComments.Enqueue(c);
            }

            // あとは毎フレーム処理に任せる
        }


        


        TimeSpan _PreviousVideoPosition = TimeSpan.Zero;
        AsyncLock _UpdateLock = new AsyncLock();
        TimeSpan _PrevCommentRenderElapsedTime = TimeSpan.Zero;
        float CommentWeightPoint = 0;

        private async Task UpdateCommentDisplay()
        {
            using (var releaser = await _UpdateLock.LockAsync())
            {
                if (CommentWeightPoint >= 0.5f)
                {
                    CommentWeightPoint -= 1.0f;
                    return;
                }

                var watch = Stopwatch.StartNew();

                TimeSpan deltaVideoPosition = TimeSpan.Zero;
                TimeSpan updateInterval;
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    // 更新済みの位置であれば処理をスキップ
                    var videoPosition = VideoPosition;

                    deltaVideoPosition = videoPosition - _PreviousVideoPosition;

                    if (_PreviousVideoPosition == videoPosition)
                    {
                        return;
                    }

                    if (_PreviousVideoPosition > videoPosition)
                    {
                        // 前方向にシークしていた場合
                        _IsNeedCommentRenderUpdated = true;
                    }

                    OnUpdate(deltaVideoPosition);

                    _PreviousVideoPosition = videoPosition;

                    updateInterval = UpdateInterval;

                    await Task.Delay(1);
                });

                watch.Stop();

                // コメント更新間隔よりコメント描画時間が長かったら
                // 描画スキップを設定する
                var renderTime = watch.Elapsed.TotalMilliseconds;
                var requireRenderTime = updateInterval.TotalMilliseconds;
                // 要求描画時間を上回った時間を計算して
                // 要求描画時間何回分をスキップすればいいのかを算出
                var overRenderTime = renderTime - requireRenderTime;
                CommentWeightPoint += (float)overRenderTime;
                CommentWeightPoint = Math.Min(2.0f, Math.Max(-2.0f, CommentWeightPoint));

                _PrevCommentRenderElapsedTime = watch.Elapsed;
            }
        }


        private CommentRenderFrameData _RenderFrameData = new CommentRenderFrameData();
        private CommentRenderFrameData GetRenderFrameData()
        {
            var commentDisplayDurationVPos = GetCommentDisplayDurationVposUnit();

            _RenderFrameData.CurrentVpos = (uint)Math.Floor(VideoPosition.TotalMilliseconds * 0.1);
            _RenderFrameData.CanvasWidth = (int)CommentCanvas.ActualWidth;
            _RenderFrameData.CanvasHeight = (uint)CommentCanvas.ActualHeight;
            _RenderFrameData.HalfCanvasWidth = CommentCanvas.ActualWidth * 0.5;
            _RenderFrameData.FontScale = (float)CommentSizeScale;
            _RenderFrameData.CommentDefaultColor = CommentDefaultColor;
            _RenderFrameData.CommentDisplayDurationVPos = commentDisplayDurationVPos;
            _RenderFrameData.CommentDisplayDuration = DefaultDisplayDuration;
            _RenderFrameData.CommentDisplayDurationSecondsDividedOne = (float)(1.0 / DefaultDisplayDuration.TotalSeconds);
            _RenderFrameData.Visibility = Visibility;

            return _RenderFrameData;
        }

        private CommentRenderInfo MakeCommentUI(Comment comment, CommentRenderFrameData frame)
        {
            // フォントサイズの計算
            // 画面サイズの10分の１＊ベーススケール＊フォントスケール
            var baseSize = frame.CanvasHeight * 0.1;
            const float PixelToPoint = 0.75f;
            var scaledFontSize = baseSize * frame.FontScale * comment.FontScale * PixelToPoint;
            comment.FontSize = (uint)Math.Ceiling(scaledFontSize);

            // フォントの影のオフセット量
            comment.TextBGOffset = Math.Floor(FontSize * TextBGOffsetBias);

            // コメントの終了位置を更新
            comment.EndPosition = comment.VideoPosition + frame.CommentDisplayDurationVPos;

            // コメントカラー
            if (comment.Color == null)
            {
                comment.RealColor = frame.CommentDefaultColor;
            }
            else
            {
                comment.RealColor = comment.Color.Value;
            }

            // コメント背景の色を求める
            comment.BackColor = GetShadowColor(comment.RealColor);


            if (CommentUICached.Count > 0)
            {
                var info = CommentUICached.Pop();

                info.Comment = comment;
                info.CommentUI.DataContext = comment;

                return info;
            }
            else
            {
                var commentUI = new CommentUI()
                {
                    DataContext = comment
                };
                return new CommentRenderInfo()
                {
                    Comment = comment,
                    CommentUI = commentUI,
                };

            }
        }


        private void OnUpdate(TimeSpan elapsedTime)
        {
            var frame = GetRenderFrameData();

            // 非表示時は処理を行わない
            if (frame.Visibility == Visibility.Collapsed)
            {
                _IsNeedCommentRenderUpdated = true;

                if (RenderComments.Any())
                {
                    foreach (var renderComment in RenderComments)
                    {
                        renderComment.CommentUI.DataContext = null;
                        CommentUICached.Push(renderComment);
                    }

                    CommentCanvas.Children.Clear();
                    RenderComments.Clear();
                }

                return;
            }

            if (_IsNeedCommentRenderUpdated)
            {
                ResetComments(frame);
                _IsNeedCommentRenderUpdated = false;
            }

            // RenderPendingCommentsからコメント表示時間より古いコメントを排除
            while (RenderPendingComments.Count != 0)
            {
                var tryComment = RenderPendingComments.Peek();
                if (tryComment.EndPosition < frame.CurrentVpos)
                {
                    RenderPendingComments.Dequeue();
                }
                else
                {
                    break;
                }
            }
            

            // 表示が完了したコメントを削除
            // 表示区間をすぎたコメントを表示対象から削除
            // 現在位置より若いコメントはReset時にカットしているのでスルー
            var removeTargets = RenderComments
                .TakeWhile(x => frame.CurrentVpos > x.Comment.EndPosition)
                .ToArray();

            foreach (var commentInfo in removeTargets)
            {
                var renderComment = commentInfo.CommentUI;

                RenderComments.Remove(commentInfo);
                CommentCanvas.Children.Remove(renderComment);
                renderComment.CommentData = null;

                CommentUICached.Push(commentInfo);
            }

            // RenderPendingCommentsから現在時間までのコメントを順次取り出してRenderCommentsに追加していく
            bool isCanAddRenderComment_Stream = true;
            bool isCanAddRenderComment_Top = true;
            bool isCanAddRenderComment_Bottom = true;
            bool isCanAddRenderComment_Center = PrevRenderComment_Center?.IsEndDisplay(frame.CurrentVpos) ?? true;


            var renderCandidateComments = new List<Comment>();

            while (RenderPendingComments.Count > 0)
            {
                var c = RenderPendingComments.Peek();
                if (c.VideoPosition < frame.CurrentVpos)
                {
                    renderCandidateComments.Add(RenderPendingComments.Dequeue());
                }
                else
                {
                    break;
                }
            }

//            var renderCandidateComments = RenderPendingComments.TakeWhile(x => x.VideoPosition < frame.CurrentVpos).ToArray();
            foreach (var comment in renderCandidateComments)
            {
                // 現フレームでは既に追加不可となっている場合はスキップ
                if (comment.VAlign == null)
                {
                    if (!isCanAddRenderComment_Stream) { continue; }
                }
                else if (comment.VAlign == VerticalAlignment.Top)
                {
                    if (!isCanAddRenderComment_Top) { continue; }
                }
                else if (comment.VAlign == VerticalAlignment.Bottom)
                {
                    if (!isCanAddRenderComment_Bottom) { continue; }
                }
                else if (comment.VAlign == VerticalAlignment.Center)
                {
                    if (!isCanAddRenderComment_Center) { continue; }
                }
                
                
                if (comment.IsNGComment || !comment.IsVisible)
                {
                    continue;
                }

                // 表示対象に登録
                var renderInfo = MakeCommentUI(comment, frame);
                renderInfo.MoveSpeedPixelPerSec = 1.0f;

                var renderComment = renderInfo.CommentUI;
                RenderComments.Add(renderInfo);
                CommentCanvas.Children.Add(renderComment);
                renderComment.UpdateLayout();


                // 初期の縦・横位置を計算
                // 縦位置を計算して表示範囲外の場合はそれぞれの表示縦位置での追加をこのフレーム内で打ち切る
                bool isOutBoundComment = false; 
                if (comment.VAlign == null)
                {
                    // 流れるコメントの縦位置を決定

                    // 前に流れているコメントを走査して挿入可能な高さを判定していく
                    // 前後のコメントが重複なく流せるかを求める
                    int insertPosition = -1;
                    double verticalPos = 8;
                    var currentCommentReachEdgeTime = renderComment.CalcReachLeftEdge(frame.CanvasWidth);
                    foreach (var prev in PrevRenderCommentEachLine_Stream.Select((x, i) => new { comment=x, index=i }))
                    {
                        // 先行コメントのテキストが画面内に完全に収まっている場合
                        // かつ
                        // 追加したいコメントが画面左端に到達した時間が
                        // 先行しているコメントの表示終了時間を超える場合
                        // コリジョンしない

                        var prevComment = prev.comment;

                        if (prevComment.CommentData == null ||
                            (prevComment.CalcTextShowRightEdgeTime(frame.CanvasWidth) < frame.CurrentVpos 
                            && prevComment.CommentData.EndPosition < currentCommentReachEdgeTime )
                            )
                        {
                            // コリジョンしない
                            // 追加可能
                            insertPosition = prev.index;
                            break;
                        }
                        else
                        {
                            // コリジョンする
                            // 追加できない
                            verticalPos += prevComment.TextHeight + prevComment.TextHeight * CommentVerticalMarginRatio;
                        }
                    }

                    // 画面下部に少しでも文字がはみ出るようなら範囲外
                    isOutBoundComment = (verticalPos + renderComment.TextHeight) > frame.CanvasHeight;
                    if (isOutBoundComment)
                    {
                        isCanAddRenderComment_Stream = false;
                    }
                    else
                    {
                        // コメントの流速を計算
                        renderInfo.MoveSpeedPixelPerSec = (frame.CanvasWidth + renderComment.TextWidth) * frame.CommentDisplayDurationSecondsDividedOne;

                        // 最初は右端に配置
                        var initialVPos = renderComment.GetPosition(frame.CanvasWidth, frame.CurrentVpos);
                        if (initialVPos != null)
                        {
                            initialVPos = frame.CanvasWidth;
                        }

                        Canvas.SetLeft(renderComment, initialVPos.Value);
                        renderInfo.PrevHorizontalPosition = initialVPos.Value;
                        renderInfo.IsSkipUpdate = true;

                        // 
                        Canvas.SetTop(renderComment, verticalPos);

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

                        isCanAddRenderComment_Stream = (verticalPos + (renderComment.TextHeight + renderComment.TextHeight * CommentVerticalMarginRatio) + renderComment.TextHeight) < frame.CanvasHeight;
                    }
                }
                else
                {                    
                    if (comment.VAlign == VerticalAlignment.Top)
                    {
                        // 上に位置する場合の縦位置の決定
                        int insertPosition = -1;
                        double verticalPos = 8;
                        foreach (var prev in PrevRenderCommentEachLine_Top.Select((x, i) => new { comment = x, index = i }))
                        {
                            var prevComment = prev.comment;
                            if (prevComment.CommentData == null 
                                || prevComment.CommentData.EndPosition < frame.CurrentVpos)
                            {
                                insertPosition = prev.index;
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
                            isCanAddRenderComment_Top = false;
                        }
                        else
                        {
                            Canvas.SetTop(renderComment, verticalPos);

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

                            isCanAddRenderComment_Top = (verticalPos + (renderComment.TextHeight + renderComment.TextHeight * CommentVerticalMarginRatio) + renderComment.TextHeight) < frame.CanvasHeight;
                        }
                    }
                    else if (comment.VAlign == VerticalAlignment.Bottom)
                    {
                        // 下に位置する場合の縦位置の決定
                        int insertPosition = -1;
                        double verticalPos = frame.CanvasHeight - renderComment.TextHeight - BottomCommentMargin;
                        foreach (var prev in PrevRenderCommentEachLine_Bottom.Select((x, i) => new { comment = x, index = i }))
                        {
                            var prevComment = prev.comment;
                            if (prevComment.CommentData == null
                                || prevComment.CommentData.EndPosition < frame.CurrentVpos)
                            {
                                insertPosition = prev.index;
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
                            isCanAddRenderComment_Bottom = false;
                        }
                        else
                        {
                            Canvas.SetTop(renderComment, verticalPos);

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

                            isCanAddRenderComment_Bottom = (verticalPos - (renderComment.TextHeight - renderComment.TextHeight * CommentVerticalMarginRatio) - renderComment.TextHeight) > 0;
                        }
                    }
                    else //if (comment.VAlign == VerticalAlignment.Center)
                    {
                        Canvas.SetTop(renderComment, frame.CanvasHeight * 0.5f - renderComment.TextHeight * 0.5f);
                        PrevRenderComment_Center = renderComment;
                        isCanAddRenderComment_Center = false;
                    }

                    if (!isOutBoundComment)
                    {
                        var left = frame.HalfCanvasWidth - (int)(renderComment.TextWidth * 0.5f);
                        Canvas.SetLeft(renderComment, left);
                    }
                }

                if (isOutBoundComment)
                {
                    // 追加してしまったRenderComments等からは削除しておく
                    RenderComments.Remove(renderInfo);
                    CommentCanvas.Children.Remove(renderComment);
                    renderComment.DataContext = null;
                    CommentUICached.Push(renderInfo);
                }
            }



            // コメントの表示位置更新
            var elapsedTime_Single = (float)elapsedTime.TotalSeconds;
            var streamRenderComments = RenderComments.Where(x => x.Comment.VAlign == null);
            foreach (var renderCommentInfo in streamRenderComments)
            {
                // コメントの初期化表示位置がElapsedTime分ズレてしまうことを防ぐため
                // このフレーム中にコメント表示リストのリセットが行われた場合には
                // コメント表示位置の更新をしない
                if (renderCommentInfo.IsSkipUpdate)
                {
                    renderCommentInfo.IsSkipUpdate = false;
                    continue;
                }

                var renderComment = renderCommentInfo.CommentUI;
                var moveSpeed = renderCommentInfo.MoveSpeedPixelPerSec * elapsedTime_Single;

                var nextHorizontalPos = renderCommentInfo.PrevHorizontalPosition - moveSpeed;
                Canvas.SetLeft(renderComment, nextHorizontalPos);
                renderCommentInfo.PrevHorizontalPosition = nextHorizontalPos;
            }
        }






        private void AddComment(Comment comment)
        {
            _IsNeedCommentRenderUpdated = true;
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





        #region Dependency Properties

        public static readonly DependencyProperty CommentDefaultColorProperty =
            DependencyProperty.Register("CommentDefaultColor"
                , typeof(Color)
                , typeof(CommentRendererEx)
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
                , typeof(CommentRendererEx)
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
                , typeof(CommentRendererEx)
                , new PropertyMetadata(1.0)
                );


        public double CommentSizeScale
        {
            get { return (double)GetValue(CommentSizeScaleProperty); }
            set { SetValue(CommentSizeScaleProperty, value); }
        }


        public static readonly DependencyProperty UpdateIntervalProperty =
            DependencyProperty.Register("UpdateInterval"
                    , typeof(TimeSpan)
                    , typeof(CommentRendererEx)
                    , new PropertyMetadata(TimeSpan.FromMilliseconds(32), OnUpdateIntervalChanged)
                );

        public TimeSpan UpdateInterval
        {
            get { return (TimeSpan)GetValue(UpdateIntervalProperty); }
            set { SetValue(UpdateIntervalProperty, value); }
        }

        private static void OnUpdateIntervalChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            CommentRendererEx me = sender as CommentRendererEx;

            me.ResetUpdateTimer();
        }


        AsyncLock _TimerGenerateLock = new AsyncLock();
        Timer _UpdateTimer;

        private async void ResetUpdateTimer()
        {
            using (var releaser = await _TimerGenerateLock.LockAsync())
            {
                if (_UpdateTimer == null)
                {
                    _UpdateTimer = new Timer(
                        async (_) =>
                        {
                            await UpdateCommentDisplay();
                        },
                        null,
                        TimeSpan.Zero,
                        this.UpdateInterval
                        );
                }
                else
                {
                    _UpdateTimer.Change(TimeSpan.Zero, this.UpdateInterval);
                }
            }
        }

        public static readonly DependencyProperty MediaPlayerProperty =
            DependencyProperty.Register("MediaPlayer"
                    , typeof(MediaPlayer)
                    , typeof(CommentRendererEx)
                    , new PropertyMetadata(default(TimeSpan))
                );


        public TimeSpan VideoPosition => MediaPlayer.PlaybackSession.Position;


        public MediaPlayer MediaPlayer
        {
            get { return (MediaPlayer)GetValue(MediaPlayerProperty); }
            set { SetValue(MediaPlayerProperty, value); }
        }

        public static readonly DependencyProperty DefaultDisplayDurationProperty =
            DependencyProperty.Register("DefaultDisplayDuration"
                , typeof(TimeSpan)
                , typeof(CommentRendererEx)
                , new PropertyMetadata(TimeSpan.FromSeconds(4))
                );

        public TimeSpan DefaultDisplayDuration
        {
            get { return (TimeSpan)GetValue(DefaultDisplayDurationProperty); }
            set { SetValue(DefaultDisplayDurationProperty, value); }
        }


        public static readonly DependencyProperty SelectedCommentIdProperty =
            DependencyProperty.Register("SelectedCommentId"
                , typeof(uint)
                , typeof(CommentRendererEx)
                , new PropertyMetadata(uint.MaxValue, OnSelectedCommentIdChanged)
                );


        public uint SelectedCommentId
        {
            get { return (uint)GetValue(SelectedCommentIdProperty); }
            set { SetValue(SelectedCommentIdProperty, value); }
        }




        private static void OnSelectedCommentIdChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

        }


        public ObservableCollection<Comment> Comments
        {
            get { return (ObservableCollection<Comment>)GetValue(CommentsProperty); }
            set { SetValue(CommentsProperty, value); }
        }


        // Using a DependencyProperty as the backing store for WorkItems.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommentsProperty =
            DependencyProperty.Register("Comments"
                , typeof(ObservableCollection<Comment>)
                , typeof(CommentRendererEx)
                , new PropertyMetadata(null, OnCommentsChanged)
                );

        private static void OnCommentsChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            CommentRendererEx me = sender as CommentRendererEx;

            var old = e.OldValue as INotifyCollectionChanged;

            if (old != null)
                old.CollectionChanged -= me.OnCommentCollectionChanged;

            var n = e.NewValue as INotifyCollectionChanged;

            if (n != null)
                n.CollectionChanged += me.OnCommentCollectionChanged;
        }

        private void OnCommentCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                // Clear and update entire collection
                CommentCanvas.Children.Clear();

                _IsNeedCommentRenderUpdated = true;
            }

            if (e.NewItems != null)
            {
                foreach (Comment item in e.NewItems)
                {
                    // Subscribe for changes on item

                    AddComment(item);

                    // Add item to internal collection
                }
            }

            if (e.OldItems != null)
            {
                foreach (Comment item in e.OldItems)
                {
                    // Unsubscribe for changes on item
                    //item.PropertyChanged -= OnWorkItemChanged;

                    // Remove item from internal collection
                }
            }


        }


        #endregion

    }

}
