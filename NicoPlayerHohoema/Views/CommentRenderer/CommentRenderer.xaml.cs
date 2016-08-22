using NicoPlayerHohoema.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace NicoPlayerHohoema.Views.CommentRenderer
{
	public sealed partial class CommentRenderer : UserControl
	{
		/// <summary>
		/// コメントUIのView使いまわし
		/// </summary>
		const int CommentUIReserveCount = 200;

		/// <summary>
		/// 仮定の最大コメント表示時間 <br />
		/// 描画範囲から切り捨てるために利用する
		/// </summary>
		const int CommentDisplayMaxTime = 700; // 1 = 10ms

		/// <summary>
		/// 各コメントの縦方向に追加される余白
		/// </summary>
		const int CommentVerticalMargin = 4;

		/// <summary>
		/// shita コマンドのコメントの下に追加する余白
		/// </summary>
		const int BottomCommentMargin = 12;

		/// <summary>
		/// 表示時間で昇順にソートされたコメントVMのディクショナリ <br />
		/// </summary>
		public SortedDictionary<uint, List<Comment>> TimeSequescailComments { get; private set; }

		/// <summary>
		/// 現在表示中のコメント
		/// </summary>
		public Dictionary<Comment, CommentUI> RenderComments { get; private set; }


		public List<CommentUI> NextVerticalPosition { get; private set; }
		public List<CommentUI> TopAlignNextVerticalPosition { get; private set; }
		public List<CommentUI> BottomAlignNextVerticalPosition { get; private set; }

		/// <summary>
		/// 非アクティブなコメントUIをあとで使いまわすためのリザーブリスト
		/// </summary>
		private List<CommentUI> _CommentUIReserve;

		private uint _RenderingSkipCount;

		/// <summary>
		/// コメントの描画タイミングを生み出すタイマー
		/// </summary>
		private Timer _RenderingTimingTimer;

		private bool _NowUpdating;
		private TimeSpan _PreviousVideoPosition = TimeSpan.Zero;
		private SemaphoreSlim _UpdateLock = new SemaphoreSlim(1, 1);


		public CommentRenderer()
		{
			this.InitializeComponent();

			TimeSequescailComments = new SortedDictionary<uint, List<Comment>>();
			RenderComments = new Dictionary<Comment, CommentUI>();

			NextVerticalPosition = new List<CommentUI>();
			TopAlignNextVerticalPosition = new List<CommentUI>();
			BottomAlignNextVerticalPosition = new List<CommentUI>();

			_CommentUIReserve = new List<CommentUI>();

			for (var i = 0; i < 100; ++i)
			{
				_CommentUIReserve.Add(new CommentUI());
			}

			Loaded += CommentRenderer_Loaded;
			Unloaded += CommentRenderer_Unloaded;
		}


		#region Event Handler

		private async void CommentRenderer_Loaded(object sender, RoutedEventArgs e)
		{
			await ResetRenderingTimer();
		}



		private void CommentRenderer_Unloaded(object sender, RoutedEventArgs e)
		{
			_RenderingTimingTimer?.Dispose();
			_RenderingTimingTimer = null;
		}


		#endregion



		private async Task ResetRenderingTimer()
		{
			_RenderingTimingTimer?.Dispose();

			uint MaxCount = 10000;
			uint count = 0;
			while (_NowUpdating && MaxCount > count)
			{
				await Task.Delay(10);

				count++;
				Debug.WriteLine("コメント描画の終了を待機中 " + count.ToString());				
			}

			_RenderingTimingTimer = new Timer(TimerCallback, this, 100, (int)(1000 / RequestFPS));
		}

		private void TimerCallback(object state)
		{
			try
			{
				_UpdateLock.Wait();

				if (_NowUpdating)
				{
					_RenderingSkipCount++;
					return;
				}

				_NowUpdating = true;
			}
			finally
			{
				_UpdateLock.Release();
			}

			var task = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
			{
				// 更新済みの位置であれば処理をスキップ
				var videoPosition = VideoPosition;
				if (_PreviousVideoPosition == videoPosition)
				{
					return;
				}

				OnUpdate();

				_PreviousVideoPosition = videoPosition;
				
			})
			.AsTask();

			task.Wait();

			try
			{
				_UpdateLock.Wait();

				_NowUpdating = false;
				_RenderingSkipCount = 0;
			}
			finally
			{
				_UpdateLock.Release();
			}
		}



		private void OnUpdate()
		{
			var currentVpos = (uint)Math.Floor(VideoPosition.TotalMilliseconds * 0.1);
			var canvasWidth = (int)CommentCanvas.ActualWidth;
			var canvasHeight = (uint)CommentCanvas.ActualHeight;
			var halfCanvasWidth = canvasWidth / 2;
			var fontScale = (float)CommentSizeScale;
			var commentDefaultColor = CommentDefaultColor;

			// 非表示時は処理を行わない
			if (Visibility == Visibility.Collapsed)
			{
				if (RenderComments.Count > 0)
				{
					foreach (var renderComment in RenderComments.ToArray())
					{
						RenderComments.Remove(renderComment.Key);

						CommentCanvas.Children.Remove(renderComment.Value);

						renderComment.Value.DataContext = null;
					}
				}

				return;
			}





			UpdateCommentVerticalPositionList(currentVpos);


			// 表示すべきコメントを抽出して、表示対象として未登録のコメントを登録処理する

			var search = BinarySearch(currentVpos);

			foreach (var comment in search)
			{
				if (!RenderComments.ContainsKey(comment))
				{
					// リザーブからCommentUIを取得

					CommentUI renderComment = null;
					if (_CommentUIReserve.Count > 0)
					{
						renderComment = _CommentUIReserve.Last();
						_CommentUIReserve.Remove(renderComment);
					}
					else
					{
						renderComment = new CommentUI();
					}

					// フォントサイズの計算
					// 画面サイズの10分の１＊ベーススケール＊フォントスケール
					var baseSize = canvasHeight / 10;
					const float PixelToPoint = 0.75f;
					var scaledFontSize = baseSize * fontScale * comment.FontScale * PixelToPoint;
					comment.FontSize = (uint)Math.Ceiling(scaledFontSize);

					if (comment.Color == null)
					{
						comment.RealColor = commentDefaultColor;
					}
					else
					{
						comment.RealColor = comment.Color.Value;
					}

					// コメント背景の色を求める
					// 色を反転して輝度に変換
					var baseColor = comment.RealColor.ToInverted();
					var c = (byte)(0.299f * baseColor.R + 0.587f * baseColor.G + 0.114f * baseColor.B);

					comment.BackColor = new Color()
					{
						R = c,
						G = c,
						B = c,
						A = byte.MaxValue
					};


					renderComment.DataContext = comment;


					renderComment.Visibility = Visibility.Visible;

					// 表示対象に登録
					RenderComments.Add(comment, renderComment);

					CommentCanvas.Children.Add(renderComment);
					renderComment.UpdateLayout();

					var verticalPos = CalcAndRegisterCommentVerticalPosition(renderComment);

					if (verticalPos < 0 || verticalPos > canvasHeight)
					{
						renderComment.Visibility = Visibility.Collapsed;

//						Debug.WriteLine("hide comment : " + comment.CommentText);
					}
					else
					{

						Canvas.SetTop(renderComment, verticalPos);
						var left = halfCanvasWidth - (int)(renderComment.DesiredSize.Width * 0.5);
//						Debug.WriteLine($"V={verticalPos}: [{renderComment.CommentData.CommentText}] [{left}] [{comment.FontSize}]");

						if (comment.VAlign.HasValue)
						{
							switch (comment.VAlign.Value)
							{
								case VerticalAlignment.Top:
								case VerticalAlignment.Bottom:
									Canvas.SetLeft(renderComment, left);
									break;
								case VerticalAlignment.Center:
								case VerticalAlignment.Stretch:
								default:
									break;
							}
						}

					}




				}
			}

			// 表示区間をすぎたコメントを表示対象から削除
			var removeRenderComments = RenderComments
				.Where(x => CommentIsEndDisplay(x.Key, currentVpos) || x.Key.IsNGComment)
				.ToArray();
			foreach (var renderComment in removeRenderComments)
			{
				// 表示対象としての登録を解除
				RenderComments.Remove(renderComment.Key);

				CommentCanvas.Children.Remove(renderComment.Value);

				_CommentUIReserve.Add(renderComment.Value);

				renderComment.Value.DataContext = null;
			}

			// CommentUIの表示位置を更新
			foreach (var renderComment in RenderComments)
			{
				var ui = renderComment.Value;
				var comment = renderComment.Key;
				if (comment.VAlign != VerticalAlignment.Bottom && comment.VAlign != VerticalAlignment.Top)
				{
					Canvas.SetLeft(ui, canvasWidth - ui.GetHorizontalPosition(canvasWidth, currentVpos));
				}
			}
		}



		private int CalcAndRegisterCommentVerticalPosition(CommentUI commentUI)
		{	
			List<CommentUI> verticalAlignList;
			VerticalAlignment? _valign = (commentUI.DataContext as Comment).VAlign;
			if (_valign.HasValue)
			{
				if (_valign.Value == VerticalAlignment.Bottom)
				{
					verticalAlignList = BottomAlignNextVerticalPosition;
				}
				else if (_valign.Value == VerticalAlignment.Top)
				{
					verticalAlignList = TopAlignNextVerticalPosition;
				}
				else
				{
					verticalAlignList = NextVerticalPosition;
				}
			}
			else
			{
				verticalAlignList = NextVerticalPosition;
			}

			int? commentVerticalPos = null;
			int totalHeight = 0;

			// TrimFromBaselineを指定しているため、下コメントに余白を追加
			if (_valign.HasValue && _valign.Value == VerticalAlignment.Bottom)
			{
				totalHeight += BottomCommentMargin;
			}
			else
			{
				totalHeight += (int)(commentUI.DesiredSize.Height * 0.35);
			}

			for (int i = 0; i< verticalAlignList.Count; ++i)
			{
				var next = verticalAlignList[i];
				if (next == null)
				{
					if (_valign.HasValue && _valign.Value == VerticalAlignment.Bottom)
					{
						commentVerticalPos = (int)this.ActualHeight - (int)commentUI.DesiredSize.Height - totalHeight;
//						Debug.WriteLine("is bottom");
					}
					else
					{
						commentVerticalPos = totalHeight;
					}
					verticalAlignList[i] = commentUI;
					break;
				}
				else
				{
					totalHeight += (int)next.DesiredSize.Height + CommentVerticalMargin + (int)(next.DesiredSize.Height * 0.35);
				}
			}


			if (!commentVerticalPos.HasValue)
			{
				if (_valign.HasValue && _valign.Value == VerticalAlignment.Bottom)
				{
					commentVerticalPos = (int)this.ActualHeight - (int)commentUI.DesiredSize.Height - totalHeight;
//					Debug.WriteLine("is bottom");
				}
				else
				{
					commentVerticalPos = totalHeight;
				}
				verticalAlignList.Add(commentUI);
			}

			return commentVerticalPos.Value;
		}


		private void UpdateCommentVerticalPositionList(uint currentVPos)
		{
			_InnerUpdateCommentVertialPositonList(currentVPos, NextVerticalPosition);
			_InnerUpdateAlignedCommentVertialPositonList(currentVPos, TopAlignNextVerticalPosition);
			_InnerUpdateAlignedCommentVertialPositonList(currentVPos, BottomAlignNextVerticalPosition);
		}

		private void _InnerUpdateAlignedCommentVertialPositonList(uint currentVPos, List<CommentUI> list)
		{
			var removeTargets = list
				.Where(x =>
				{
					if (x == null) { return false; }

					return x.CommentData == null;
				})
				.ToArray();


			foreach (var remove in removeTargets)
			{
				var index = list.IndexOf(remove);
				list[index] = null;
			}
		}

		private void _InnerUpdateCommentVertialPositonList(uint currentVPos, List<CommentUI> list)
		{
			var removeTargets = list
				.Where(x =>
				{
					if (x == null) { return false; }

					var comment = x.CommentData;

					if (comment == null) { return true; }

					return x.IsCompleteInsideScreen((int)this.ActualWidth, Math.Max(currentVPos, 0));
				})
				.ToArray();


			foreach (var remove in removeTargets)
			{
				var index = list.IndexOf(remove);
				list[index] = null;
			}
		}
		


		private bool CommentIsEndDisplay(Comment comment, uint currentVpos)
		{
			return !(comment.VideoPosition <= currentVpos && currentVpos <= comment.EndPosition);
		}

		




		private void AddComment(Comment comment)
		{
			var vpos = comment.VideoPosition;
			List<Comment> list;
			if (TimeSequescailComments.ContainsKey(vpos))
			{
				list = TimeSequescailComments[vpos];
			}
			else
			{
				list = new List<Comment>();
				TimeSequescailComments.Add(vpos, list);
			}

			list.Add(comment);
		}


		private CommentUI MakeCommentUI(Comment comment)
		{
			CommentUI ui = new CommentUI()
			{
				DataContext = comment
			};

			return ui;
		}




		private IEnumerable<Comment> BinarySearch(uint currentVpos)
		{
			int skipVpos = (int)currentVpos - CommentDisplayMaxTime;
			return TimeSequescailComments.Keys
				.SkipWhile(x => x < skipVpos)
				.TakeWhile(x => x < currentVpos)
				.Select(x => TimeSequescailComments[x])
				.SelectMany(x => x.Where(y => currentVpos < y.EndPosition && !y.IsNGComment));
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


		public static readonly DependencyProperty VideoPositionProperty =
			DependencyProperty.Register("VideoPosition"
					, typeof(TimeSpan)
					, typeof(CommentRenderer)
					, new PropertyMetadata(default(TimeSpan), OnVideoPositionChanged)
				);

		public TimeSpan VideoPosition
		{
			get { return (TimeSpan)GetValue(VideoPositionProperty); }
			set { SetValue(VideoPositionProperty, value); }
		}


		private static void OnVideoPositionChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			CommentRenderer me = sender as CommentRenderer;

//			me.OnUpdate();
		}




		public static readonly DependencyProperty RequestFPSProperty =
			DependencyProperty.Register("RequestFPS"
				, typeof(int)
				, typeof(CommentRenderer)
				, new PropertyMetadata(24, OnRequestFPSChanged)
				);

		public int RequestFPS
		{
			get { return (int)GetValue(RequestFPSProperty); }
			set { SetValue(RequestFPSProperty, value); }
		}


		private static void OnRequestFPSChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			CommentRenderer me = sender as CommentRenderer;

			me.ResetRenderingTimer();
		}



		public static readonly DependencyProperty SelectedCommentIdProperty =
			DependencyProperty.Register("SelectedCommentId"
				, typeof(uint)
				, typeof(CommentRenderer)
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
				, typeof(CommentRenderer)
				, new PropertyMetadata(null, OnCommentsChanged)
				);

		private static void OnCommentsChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			CommentRenderer me = sender as CommentRenderer;

			var old = e.OldValue as ObservableCollection<Comment>;

			if (old != null)
				old.CollectionChanged -= me.OnCommentCollectionChanged;

			var n = e.NewValue as ObservableCollection<Comment>;

			if (n != null)
				n.CollectionChanged += me.OnCommentCollectionChanged;
		}

		private void OnCommentCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			
			if (e.Action == NotifyCollectionChangedAction.Reset)
			{
				// Clear and update entire collection
				CommentCanvas.Children.Clear();

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
