using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
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
		const int CommentUIReserveCount = 100;

		public SortedDictionary<uint, List<Comment>> TimeSequescailComments { get; private set; }
		public Dictionary<Comment, CommentUI> RenderComments { get; private set; }


		public List<CommentUI> NextVerticalPosition { get; private set; }
		public List<CommentUI> TopAlignNextVerticalPosition { get; private set; }
		public List<CommentUI> BottomAlignNextVerticalPosition { get; private set; }

		private Timer _UpdateTimingTimer;

		private List<CommentUI> _CommentUIReserve;


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

	

		private void CommentRenderer_Loaded(object sender, RoutedEventArgs e)
		{
//			_UpdateTimingTimer = new Timer(
//				async (state) =>
//				{
//					var me = (CommentRenderer)state;

//					await me.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => 
//					{
////						me.OnUpdate();
//					});
//				}
//				, this
//				, TimeSpan.FromSeconds(1)
//				, TimeSpan.FromMilliseconds(64)
//			);
		}

		
		private int CalcAndRegisterCommentVerticalPosition(CommentUI commentUI)
		{
			const int CommentVerticalMargin = 2;

			
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
			int totalHeight = CommentVerticalMargin;
			for (int i = 0; i< verticalAlignList.Count; ++i)
			{
				var next = verticalAlignList[i];
				if (next == null)
				{
					if (_valign.HasValue && _valign.Value == VerticalAlignment.Bottom)
					{
						commentVerticalPos = (int)this.ActualHeight - (int)commentUI.DesiredSize.Height - totalHeight;
						Debug.WriteLine("is bottom");
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
					totalHeight += (int)next.DesiredSize.Height + CommentVerticalMargin;
				}
			}


			if (!commentVerticalPos.HasValue)
			{
				if (_valign.HasValue && _valign.Value == VerticalAlignment.Bottom)
				{
					commentVerticalPos = (int)this.ActualHeight - (int)commentUI.DesiredSize.Height - totalHeight;
					Debug.WriteLine("is bottom");
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

					return x.IsCompleteInsideScreen((int)this.ActualWidth, Math.Max(currentVPos - 50, 0));
				})
				.ToArray();


			foreach (var remove in removeTargets)
			{
				var index = list.IndexOf(remove);
				list[index] = null;
			}
		}
		
		private void OnUpdate()
		{
			var currentVpos = (uint)Math.Floor(VideoPosition.TotalMilliseconds * 0.1);
			var canvasWidth = (int)CommentCanvas.ActualWidth;
			var halfCanvasWidth = canvasWidth / 2;

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

					renderComment.DataContext = comment;


					// 表示対象に登録
					RenderComments.Add(comment, renderComment);

					CommentCanvas.Children.Add(renderComment);
					renderComment.UpdateLayout();

					var verticalPos = CalcAndRegisterCommentVerticalPosition(renderComment);
					System.Diagnostics.Debug.WriteLine($"V={verticalPos}: [{renderComment.CommentData.CommentText}]");
					Canvas.SetTop(renderComment, verticalPos);

					if (comment.VAlign.HasValue)
					{
						switch (comment.VAlign.Value)
						{
							case VerticalAlignment.Top:
							case VerticalAlignment.Bottom:
								Canvas.SetLeft(renderComment, halfCanvasWidth - (int)(renderComment.DesiredSize.Width * 0.5));
								break;
							case VerticalAlignment.Center:
							case VerticalAlignment.Stretch:
							default:
								break;
						}
					}

				}
			}

			// 表示区間をすぎたコメントを表示対象から削除

			var removeRenderComments = RenderComments
				.Where(x => CommentIsEndDisplay(x.Key, currentVpos))
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


		private bool CommentIsEndDisplay(Comment comment, uint currentVpos)
		{
			return !(comment.VideoPosition <= currentVpos && currentVpos <= comment.EndPosition);
		}

		private void CommentRenderer_Unloaded(object sender, RoutedEventArgs e)
		{

			_UpdateTimingTimer?.Dispose();
			_UpdateTimingTimer = null;
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

			me.OnUpdate();
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
			CommentRendererWithWin2D me = sender as CommentRendererWithWin2D;

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
			return TimeSequescailComments.Keys.Where(x => x < currentVpos)
				.Select(x => TimeSequescailComments[x])
				.SelectMany(x => x.Where(y => currentVpos < y.EndPosition));
		}


		private IEnumerable<Comment> DisplayComments(IEnumerable<uint> vposList)
		{
			foreach (var vpos in vposList)
			{
				var list = TimeSequescailComments[vpos];
				foreach (var comment in list)
				{
					yield return comment;
				}
			}
		}
	}


}
