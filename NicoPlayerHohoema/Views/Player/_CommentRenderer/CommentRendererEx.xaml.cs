using NicoPlayerHohoema.Helpers;
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

namespace NicoPlayerHohoema.Views
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
            public bool IsSkipUpdate { get; set; }
            public float MoveSpeedPixelPerSec { get; set; }
            public double PrevHorizontalPosition { get; set; }

        }

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


        private Stack<CommentRenderInfo> CommentUICached = new Stack<CommentRenderInfo>();


        /// <summary>
        /// 描画待ちのコメントリスト
        /// 現在時間を過ぎたコメントをここから払い出していく
        /// </summary>
        private List<Comment> RenderPendingComments = new List<Comment>();

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

            Loaded += CommentRendererEx_Loaded;
            Unloaded += CommentRendererEx_Unloaded;
            Application.Current.EnteredBackground += Current_EnteredBackground;
            Application.Current.LeavingBackground += Current_LeavingBackground;

            this.SizeChanged += CommentRendererEx_SizeChanged;
        }

        private void CommentRendererEx_Loaded(object sender, RoutedEventArgs e)
        {
            ResetUpdateTimer();
        }

        private async void CommentRendererEx_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            using (var releaser = await _UpdateLock.LockAsync())
            {
                foreach (var renderComment in RenderComments)
                {
                    CommentUICached.Push(renderComment);
                }

                RenderComments.Clear();
                CommentCanvas.Children.Clear();

                PrevRenderCommentEachLine_Stream.Clear();
                PrevRenderCommentEachLine_Top.Clear();
                PrevRenderCommentEachLine_Bottom.Clear();

                Clip = new RectangleGeometry() { Rect = new Rect() { Width = ActualWidth, Height = ActualHeight } };
            }
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
            Debug.WriteLine("Comment Reset");

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
                RenderPendingComments.Add(c);
            }

            // あとは毎フレーム処理に任せる

            _RealVideoPosition = DateTime.Now;
        }


        


        TimeSpan _PreviousVideoPosition = TimeSpan.Zero;
        AsyncLock _UpdateLock = new AsyncLock();
        TimeSpan _PrevCommentRenderElapsedTime = TimeSpan.Zero;
        float CommentWeightPoint = 0;

        DateTime _RealVideoPosition = DateTime.Now;

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

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                {
                    // 更新済みの位置であれば処理をスキップ
                    var videoPosition = VideoPosition;

                    if (MediaPlayer == null)
                    {
                        _UpdateTimer?.Dispose();
                        _UpdateTimer = null;
                        return;
                    }

                    if (PlaybackState == MediaPlaybackState.Paused)
                    {
                        return;
                    }

                    if (_PlayerCanSeek)
                    {
                        if (_PreviousVideoPosition == videoPosition)
                        {
                            return;
                        }

                        if (_PreviousVideoPosition > videoPosition)
                        {
                            // 前方向にシークしていた場合
                            _IsNeedCommentRenderUpdated = true;
                        }

                        deltaVideoPosition = videoPosition - _PreviousVideoPosition;
                    }
                    else
                    {
                        deltaVideoPosition = DateTime.Now - _RealVideoPosition;
                    }

                    OnUpdate(deltaVideoPosition);

                    _PreviousVideoPosition = videoPosition;

                    updateInterval = UpdateInterval;
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
                _RealVideoPosition = DateTime.Now;
            }
        }


        private CommentRenderFrameData _RenderFrameData;
        private CommentRenderFrameData GetRenderFrameData()
        {
            var commentDisplayDurationVPos = GetCommentDisplayDurationVposUnit();

            if (_RenderFrameData == null)
            {
                _RenderFrameData = new CommentRenderFrameData();
                _RenderFrameData.CommentDisplayDuration = DefaultDisplayDuration;
                _RenderFrameData.CommentDisplayDurationSecondsDividedOne = (float)(1.0 / DefaultDisplayDuration.TotalSeconds);
            }

            _RenderFrameData.CommentDefaultColor = CommentDefaultColor;
            _RenderFrameData.CurrentVpos = (uint)Math.Floor(VideoPosition.TotalMilliseconds * 0.1);
            _RenderFrameData.CanvasWidth = (int)CommentCanvas.ActualWidth;
            _RenderFrameData.CanvasHeight = (uint)CommentCanvas.ActualHeight;
            _RenderFrameData.HalfCanvasWidth = CommentCanvas.ActualWidth * 0.5;
            _RenderFrameData.FontScale = (float)CommentSizeScale;
            _RenderFrameData.CommentDisplayDurationVPos = commentDisplayDurationVPos;
            _RenderFrameData.Visibility = Visibility;

            return _RenderFrameData;
        }

        private CommentRenderInfo MakeCommentUI(Comment comment, CommentRenderFrameData frame)
        {
            // フォントサイズの計算
            // 画面サイズの10分の１＊ベーススケール＊フォントスケール
            var baseSize = Math.Max(frame.CanvasHeight * BaseCommentSizeRatioByCanvasHeight, 24);
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



        List<Comment> _RenderCandidateComments = new List<Comment>(100);
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
                var tryComment = RenderPendingComments.First();
                if (tryComment.EndPosition < frame.CurrentVpos)
                {
                    RenderPendingComments.Remove(tryComment);
                }
                else
                {
                    break;
                }
            }
            

            // 表示が完了したコメントを削除
            // 表示区間をすぎたコメントを表示対象から削除
            // 現在位置より若いコメントはReset時にカットしているのでスルー
            while (RenderComments.Count > 0)
            {
                var commentInfo = RenderComments.First();

                if (frame.CurrentVpos < commentInfo.Comment.EndPosition)
                {
                    break;
                }

                var renderComment = commentInfo.CommentUI;

                RenderComments.Remove(commentInfo);
                CommentCanvas.Children.Remove(renderComment);
                renderComment.CommentData = null;

                CommentUICached.Push(commentInfo);

                // RenderCommentInfoとCommentUIのインスタンスを使いまわしているため
                // PrevRenderCommentEachLine_*のリストから不要要素を削除しておかないと
                // 将来の描画フレームにおいてインスタンスが再有効化された時に
                // 縦位置決定の処理で問題が発生するようになる
                var c = commentInfo.Comment;
                if (c.VAlign == null)
                {
                    var index = PrevRenderCommentEachLine_Stream.IndexOf(renderComment);
                    if (index >= 0)
                    {
                        PrevRenderCommentEachLine_Stream[index] = null;
                    }
                }
                else if (c.VAlign == VerticalAlignment.Top)
                {
                    var index = PrevRenderCommentEachLine_Top.IndexOf(renderComment);
                    if (index >= 0)
                    {
                        PrevRenderCommentEachLine_Top[index] = null;
                    }
                }
                else if (c.VAlign == VerticalAlignment.Bottom)
                {
                    var index = PrevRenderCommentEachLine_Bottom.IndexOf(renderComment);
                    if (index >= 0)
                    {
                        PrevRenderCommentEachLine_Bottom[index] = null;
                    }
                }
                else //if (c.VAlign == VerticalAlignment.Center)
                {
                    PrevRenderComment_Center = null;
                }
            }

            // RenderPendingCommentsから現在時間までのコメントを順次取り出してRenderCommentsに追加していく
            while (RenderPendingComments.Count > 0)
            {
                var c = RenderPendingComments.First();
                if (c.VideoPosition < frame.CurrentVpos)
                {
                    _RenderCandidateComments.Add(c);
                    
                    RenderPendingComments.Remove(c);
                }
                else
                {
                    break;
                }
            }

            bool isCanAddRenderComment_Stream = true;
            bool isCanAddRenderComment_Top = true;
            bool isCanAddRenderComment_Bottom = true;
            bool isCanAddRenderComment_Center = PrevRenderComment_Center?.IsEndDisplay(frame.CurrentVpos) ?? true;
            
            foreach (var comment in _RenderCandidateComments)
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
                    continue;
//                    if (!isCanAddRenderComment_Center) { continue; }
                }
                
                
                if (comment.CheckIsNGComment())
                {
                    Debug.WriteLine("NG: " + comment.CommentText);
                    continue;
                }

                if (!comment.IsVisible) { continue; }
                

                // 表示対象に登録
                var renderInfo = MakeCommentUI(comment, frame);
                renderInfo.IsSkipUpdate = false;

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
                    var currentCommentReachLeftEdgeTime = renderComment.CalcReachLeftEdge(frame.CanvasWidth);
                    for (var i = 0; i < PrevRenderCommentEachLine_Stream.Count; i++)
                    {
                        var prevComment = PrevRenderCommentEachLine_Stream[i];
                        // 先行コメントのテキストが画面内に完全に収まっている場合
                        // かつ
                        // 追加したいコメントが画面左端に到達した時間が
                        // 先行しているコメントの表示終了時間を超える場合
                        // コリジョンしない
                        if (prevComment?.CommentData == null ||
                            (prevComment.CalcTextShowRightEdgeTime(frame.CanvasWidth) < frame.CurrentVpos 
                            && prevComment.CommentData.EndPosition < currentCommentReachLeftEdgeTime )
                            )
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
                        if (initialVPos == null)
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

                        isCanAddRenderComment_Stream = (verticalPos + (renderComment.TextHeight + renderComment.TextHeight * CommentVerticalMarginRatio)) < frame.CanvasHeight;
                    }
                }
                else
                {                    
                    if (comment.VAlign == VerticalAlignment.Top)
                    {
                        // 上に位置する場合の縦位置の決定
                        int insertPosition = -1;
                        double verticalPos = 8;
                        for (var i = 0; i < PrevRenderCommentEachLine_Top.Count; i++)
                        {
                            var prevComment = PrevRenderCommentEachLine_Top[i];
                            if (prevComment?.CommentData == null 
                                || prevComment.CommentData.EndPosition < frame.CurrentVpos)
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

                            isCanAddRenderComment_Top = (verticalPos + (renderComment.TextHeight + renderComment.TextHeight * CommentVerticalMarginRatio)) < frame.CanvasHeight;
                        }
                    }
                    else if (comment.VAlign == VerticalAlignment.Bottom)
                    {
                        // 下に位置する場合の縦位置の決定
                        int insertPosition = -1;
                        double verticalPos = frame.CanvasHeight - renderComment.TextHeight - BottomCommentMargin;
                        for (var i = 0; i < PrevRenderCommentEachLine_Bottom.Count; i++)
                        {
                            var prevComment = PrevRenderCommentEachLine_Bottom[i];
                            if (prevComment?.CommentData == null
                                || prevComment.CommentData.EndPosition < frame.CurrentVpos)
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

                            isCanAddRenderComment_Bottom = (verticalPos - (renderComment.TextHeight + renderComment.TextHeight * CommentVerticalMarginRatio)) > 0;
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

            _RenderCandidateComments.Clear();

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

                // 常に絶対位置で計算する
                // 内部で除算を使っているためやや重い
                /*
                var renderComment = renderCommentInfo.CommentUI;
                var pos = renderComment.GetPosition(frame.CanvasWidth, frame.CurrentVpos);
                if (pos != null)
                {
                    Canvas.SetLeft(renderComment, pos.Value);
                }
                */
            }
        }






        private void AddComment(Comment comment)
        {
            if (_IsNeedCommentRenderUpdated) { return; }

            if (comment.EndPosition < (VideoPosition.TotalSeconds * 100))
            {
                // もう表示することはないので何もしない
            }
            else
            {
                // 
                var insertPos = RenderPendingComments.FindIndex(x => x.VideoPosition > comment.VideoPosition);
                if (insertPos >= 0)
                {
                    RenderPendingComments.Insert(insertPos, comment);
                }
                else
                {
                    RenderPendingComments.Add(comment);
                }

            }
            //_IsNeedCommentRenderUpdated = true;
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
                            try
                            {
                                await UpdateCommentDisplay();
                            }
                            catch
                            {
                                _UpdateTimer?.Dispose();
                            }
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
                    , new PropertyMetadata(default(MediaPlayer), OnMediaPlayerChanged)
                );
        private static void OnMediaPlayerChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var me = (CommentRendererEx)sender;
            
            if (me.MediaPlayer == null)
            {
                var oldMediaPlayer = (e.OldValue as MediaPlayer);
                if (oldMediaPlayer != null)
                {
                    oldMediaPlayer.PlaybackSession.PlaybackStateChanged += me.PlaybackSession_PlaybackStateChanged;
                    oldMediaPlayer.SourceChanged += me.MediaPlayer_SourceChanged;
                }
                me._UpdateTimer?.Dispose();
                me._UpdateTimer = null;
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
            using (var releaser = await _UpdateLock.LockAsync())
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    if (MediaPlayer == null || MediaPlayer.Source == null)
                    {
                        PlaybackState = null;
                        return;
                    }

                    PlaybackState = sender?.PlaybackState ?? null;
                });
            }
        }

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
                , typeof(CommentRendererEx)
                , new PropertyMetadata(TimeSpan.FromSeconds(4), OnDefaultDisplayDurationChanged)
                );

        public TimeSpan DefaultDisplayDuration
        {
            get { return (TimeSpan)GetValue(DefaultDisplayDurationProperty); }
            set { SetValue(DefaultDisplayDurationProperty, value); }
        }


        private static void OnDefaultDisplayDurationChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var me = (CommentRendererEx)sender;
            me._RenderFrameData = null;
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


        public ICollection<Comment> Comments
        {
            get { return (ICollection<Comment>)GetValue(CommentsProperty); }
            set { SetValue(CommentsProperty, value); }
        }


        // Using a DependencyProperty as the backing store for WorkItems.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommentsProperty =
            DependencyProperty.Register("Comments"
                , typeof(ICollection<Comment>)
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
