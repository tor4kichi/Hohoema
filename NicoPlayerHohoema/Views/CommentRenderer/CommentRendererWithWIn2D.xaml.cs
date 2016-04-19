using Microsoft.Graphics.Canvas.Text;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Graphics.Canvas.Brushes;
using Windows.UI;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas;
using System.Threading.Tasks;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace NicoPlayerHohoema.Views
{
	// 
	internal class AnimatedControlData
	{
		public AnimatedControlData()
		{
			TimeSequescailComments = new SortedDictionary<uint, List<Comment>>();
			RenderComments = new Dictionary<Comment, CommentRenderInfo>();
			SelectedCommentOutlineColor = Colors.White;
			CommentVerticalPosToNextVpos = new List<uint>();
		}

		public double UIWidth { get; set; }
		public double UIHeight { get; set; }

		public uint VideoPos { get; set; }
		public Color SelectedCommentOutlineColor { get; set; }
		public uint SelectedCommentId { get; set; }

		public SortedDictionary<uint, List<Comment>> TimeSequescailComments { get; private set; }
		public Dictionary<Comment, CommentRenderInfo> RenderComments { get; private set; }

		public List<uint> CommentVerticalPosToNextVpos { get; private set; }
		public List<uint> CommentVerticalUePosToNextVpos { get; private set; }
		public List<uint> CommentVerticalShitaPosToNextVpos { get; private set; }
		


		public int GetNextPosition(uint vpos, CommentRenderInfo renderInfo, VerticalAlignment align)
		{
			// コメントを流す位置を確定する
			switch (align)
			{
				case VerticalAlignment.Top:
					break;
				case VerticalAlignment.Center:
					break;
				case VerticalAlignment.Bottom:
					break;
				case VerticalAlignment.Stretch:
					break;
				default:
					break;
			}
			return 1;
		}
	}

	public sealed partial class CommentRendererWithWin2D : UserControl
	{
		public CommentRendererWithWin2D()
		{
			this.InitializeComponent();

			
		}


		private AnimatedControlData GameLoopThreadData { get; set; }




		public static readonly DependencyProperty SelectedCommentOutlineColorProperty =
			DependencyProperty.Register("SelectedCommentOutlineColor"
				, typeof(Color)
				, typeof(CommentRendererWithWin2D)
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
					, typeof(CommentRendererWithWin2D)
					, new PropertyMetadata(default(TimeSpan), OnVideoPositionChanged)
				);

		public TimeSpan VideoPosition
		{
			get { return (TimeSpan)GetValue(VideoPositionProperty); }
			set { SetValue(VideoPositionProperty, value); }
		}

		
		private static async void OnVideoPositionChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			CommentRendererWithWin2D me = sender as CommentRendererWithWin2D;

			var val = (TimeSpan)e.NewValue;
			try
			{
				await me.CanvasControl.RunOnGameLoopThreadAsync(() =>
				{
					me.GameLoopThreadData.VideoPos = (uint)Math.Floor(val.TotalMilliseconds * 0.1);
				});
			}
			catch (TaskCanceledException taskCancel)
			{
				System.Diagnostics.Debug.WriteLine(taskCancel.Message);
			}
		}







		public static readonly DependencyProperty SelectedCommentIdProperty =
			DependencyProperty.Register("SelectedCommentId", typeof(uint), typeof(CommentRendererWithWin2D), new PropertyMetadata(uint.MaxValue, OnSelectedCommentIdChanged));


		public uint SelectedCommentId
		{
			get { return (uint)GetValue(SelectedCommentIdProperty); }
			set { SetValue(SelectedCommentIdProperty, value); }
		}


		private static async void OnSelectedCommentIdChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			CommentRendererWithWin2D me = sender as CommentRendererWithWin2D;

			await me.CanvasControl.RunOnGameLoopThreadAsync(() =>
			{
				var newSelectCommandId = (uint)e.NewValue;

				me.GameLoopThreadData.SelectedCommentId = newSelectCommandId;
			});

		}







		public ObservableCollection<Comment> Comments
		{
			get { return (ObservableCollection<Comment>)GetValue(CommentsProperty); }
			set { SetValue(CommentsProperty, value); }
		}


		// Using a DependencyProperty as the backing store for WorkItems.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CommentsProperty =
			DependencyProperty.Register("Comments", typeof(ObservableCollection<Comment>), typeof(CommentRendererWithWin2D), new PropertyMetadata(null, OnCommentsChanged));

		private static void OnCommentsChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			CommentRendererWithWin2D me = sender as CommentRendererWithWin2D;

			var old = e.OldValue as ObservableCollection<Comment>;

			if (old != null)
				old.CollectionChanged -= me.OnCommentCollectionChanged;

			var n = e.NewValue as ObservableCollection<Comment>;

			if (n != null)
				n.CollectionChanged += me.OnCommentCollectionChanged;
		}

		private async void OnCommentCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			await CanvasControl.RunOnGameLoopThreadAsync(() =>
			{
				if (e.Action == NotifyCollectionChangedAction.Reset)
				{
					// Clear and update entire collection
					GameLoopThreadData.TimeSequescailComments.Clear();
					GameLoopThreadData.RenderComments.Clear();
				}

				if (e.NewItems != null)
				{
					foreach (Comment item in e.NewItems)
					{
						// Subscribe for changes on item
						var gameloopThreadComment = new Comment()
						{
							Color = item.Color,
							CommentId = item.CommentId,
							CommentText = item.CommentText,
							EndPosition = item.EndPosition,
							VideoPosition = item.VideoPosition,
							FontSize = item.FontSize,
							HAlign = item.HAlign,
							VAlign = item.VAlign,
							IsAnonimity = item.IsAnonimity,
							IsVisible = item.IsVisible,
							UserId = item.UserId,
							IsOwnerComment = item.IsOwnerComment
						};

						AddNewComment(gameloopThreadComment);

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
			});

		}


		private void AddNewComment(Comment comment)
		{
			var vpos = comment.VideoPosition;
			List<Comment> list;
			if (GameLoopThreadData.TimeSequescailComments.ContainsKey(vpos))
			{
				list = GameLoopThreadData.TimeSequescailComments[vpos];
			}
			else
			{
				list = new List<Comment>();
				GameLoopThreadData.TimeSequescailComments.Add(vpos, list);
			}

			list.Add(comment);
		}





		private void CanvasControl_CreateResources(Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
		{

		}


		private void CanvasControl_Update(Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedUpdateEventArgs args)
		{
			// コメントの表示時間
			const uint CommentDisplayTime = 300; // 3秒

			var currentVpos = GameLoopThreadData.VideoPos;

			// 表示すべきコメントを抽出する
			var displayCommentVposEnumerable = BinarySearch(currentVpos, currentVpos + CommentDisplayTime).ToList();

			if (displayCommentVposEnumerable.Count == 0)
			{
				return;
			}

			var displayComments = DisplayComments(displayCommentVposEnumerable);


			foreach (var displayComment in displayComments)
			{
				if (!GameLoopThreadData.RenderComments.ContainsKey(displayComment))
				{
					var renderComment = new CommentRenderInfo(displayComment);
					GameLoopThreadData.RenderComments.Add(displayComment, renderComment);
					renderComment.Width = renderComment.Comment.CommentText.Length * renderComment.Comment.FontSize;
					renderComment.Height = 0;
				}
			}


			// 表示区間をすぎたコメントを表示対象から削除

			var removeRenderComments = GameLoopThreadData.RenderComments.Where(x => x.Value.IsEndDisplay(currentVpos)).ToArray();
			foreach (var renderComment in removeRenderComments)
			{
				GameLoopThreadData.RenderComments.Remove(renderComment.Key);
			}

			displayCommentVposEnumerable.Clear();
			displayCommentVposEnumerable = null;


		}


		private void CanvasControl_Draw(Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedDrawEventArgs args)
		{


			var currentVpos = GameLoopThreadData.VideoPos;
			var selectedCommentId = GameLoopThreadData.SelectedCommentId;





			// 表示対象コメントを描画

			Vector2 commentLocation = new Vector2();
			using (var textFormat = new CanvasTextFormat { FontSize = 14.0f, WordWrapping = CanvasWordWrapping.NoWrap })
			using (var session = args.DrawingSession)
			{
				foreach (var renderComment in GameLoopThreadData.RenderComments.Values)
				{
					if (renderComment.IsFirstRendering)
					{
						// 最初に描画するときだけ、描画後のテキストサイズを計算する
						// http://stackoverflow.com/questions/30696838/how-to-calculate-the-size-of-a-piece-of-text-in-win2d

						/*
						using (var textLayout = new CanvasTextLayout(session, renderComment.Comment.CommentText, textFormat, 0.0f, 0.0f))
						{
							renderComment.Width = textLayout.DrawBounds.Width;
							renderComment.Height = textLayout.DrawBounds.Height;
						}
						*/
						renderComment.IsFirstRendering = false;
					}

					var uiWidth = GameLoopThreadData.UIWidth;
					commentLocation.X = (float)uiWidth - renderComment.GetHorizontalPosition((int)uiWidth, currentVpos);

					// TODO: コメントの高さを求める
					commentLocation.Y = 0.0f;
					
					var isSelectedComment = renderComment.Comment.CommentId == selectedCommentId;
					if (isSelectedComment)
					{
						var textDrawRect = new Rect(
							commentLocation.X
							, commentLocation.Y
							, renderComment.Width
							, renderComment.Height
							);

						session.DrawRectangle(textDrawRect, SelectedCommentOutlineColor, 2.0f);
					}

					session.DrawText(renderComment.Comment.CommentText, commentLocation, renderComment.Comment.Color);

				}
			}



		}


		private IEnumerable<uint> BinarySearch(uint start, uint end)
		{
			var list = GameLoopThreadData.TimeSequescailComments.Keys;
			return list.SkipWhile((x => x > start))
				.TakeWhile(x => x < end);
		}


		private IEnumerable<Comment> DisplayComments(IEnumerable<uint> vposList)
		{
			foreach (var vpos in vposList)
			{
				var list = GameLoopThreadData.TimeSequescailComments[vpos];
				foreach (var comment in list)
				{
					yield return comment;
				}
			}
		}


		public uint OnMouseComment { get; private set; }

		private void CanvasControl_GameLoopStarting(Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl sender, object args)
		{
			GameLoopThreadData = new AnimatedControlData();

			

		}

		private async void CanvasControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			var width = ActualWidth;
			var height = ActualHeight;
			
			await CanvasControl.RunOnGameLoopThreadAsync( () =>
			{
				GameLoopThreadData.UIWidth = width;
				GameLoopThreadData.UIHeight = height;
			});
		}

		private async void CanvasControl_Loaded(object sender, RoutedEventArgs e)
		{
			var width = ActualWidth;
			var height = ActualHeight;

			await CanvasControl.RunOnGameLoopThreadAsync(() =>
			{
				GameLoopThreadData.UIWidth = width;
				GameLoopThreadData.UIHeight = height;
			});

			CanvasControl.SizeChanged += CanvasControl_SizeChanged;
		}
	}




	public class CommentRenderInfo : IDisposable
	{
		public CommentRenderInfo(Comment comment)
		{
			Comment = comment;
			
			IsFirstRendering = true;
		}

		public CanvasTextLayout CanvasTextLayout { get; private set; }

		public bool IsFirstRendering { get; set; }

		public double Width { get; set; }
		public double Height { get; set; }

		public bool IsEndDisplay(uint currentVpos)
		{
			return Comment.EndPosition <= currentVpos;
		}

		public int GetHorizontalPosition(int screenWidth, uint currentVpos)
		{
			// (Comment.EndPositioin - Comment.VideoPosition) の長さまでにコメント全体を表示しなければいけない
			// コメントの移動距離＝ screenWidth + Width

			//                                        コメント
			// ------------|--------------------------|-----------

			//                    コメント
			// ------------|--------------------------|-----------

			//      コメント
			// ------------|--------------------------|-----------
			
			// distance
			//      |---------------------------------|

			//

			

			var distance = screenWidth + Width;
			var displayTime = (Comment.EndPosition - Comment.VideoPosition);
			var localVpos = displayTime - (Comment.EndPosition - currentVpos);
			var lerp = localVpos / (float)displayTime;

			// 理論的にlocalVposはdisplayTimeを越えることはない

			return (int) Math.Floor(distance * lerp);
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public Comment Comment { get; private set; }
	}

	public class CommentApperance
	{
		public int? FontSize { get; private set; }
		public uint? Color { get; private set; }
	}

}
