using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace NicoPlayerHohoema.Views
{
	public sealed partial class CommandStreaming : UserControl
	{
		public CommandStreaming()
		{
			this.InitializeComponent();
		}




		public ObservableCollection<string> Comments
		{
			get { return (ObservableCollection<string>)GetValue(CommentsProperty); }
			set { SetValue(CommentsProperty, value); }
		}

		// Using a DependencyProperty as the backing store for WorkItems.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CommentsProperty =
			DependencyProperty.Register("Comments", typeof(ObservableCollection<string>), typeof(CommandStreaming), new PropertyMetadata(null, OnCommentsChanged));

		private static void OnCommentsChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			CommandStreaming me = sender as CommandStreaming;

			var old = e.OldValue as ObservableCollection<string>;

			if (old != null)
				old.CollectionChanged -= me.OnCommentCollectionChanged;

			var n = e.NewValue as ObservableCollection<string>;

			if (n != null)
				n.CollectionChanged += me.OnCommentCollectionChanged;
		}

		private void OnCommentCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Reset)
			{
				// Clear and update entire collection
			}

			if (e.NewItems != null)
			{
				foreach (string item in e.NewItems)
				{
					// Subscribe for changes on item
					AddNewComment(item);

					// Add item to internal collection
				}
			}

			if (e.OldItems != null)
			{
				foreach (string item in e.OldItems)
				{
					// Unsubscribe for changes on item
					//item.PropertyChanged -= OnWorkItemChanged;

					// Remove item from internal collection
				}
			}
		}

		private void AddNewComment(string comment)
		{

		}

		private void CanvasControl_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
		{
			
		}
	}




	public class CommentRenderInfo
	{
		public CommentRenderInfo(string comment, int verticalPos, CommentApperance baseApperance, CommentApperance overrideApperance = null, float streamSpeedScale = 1.0f)
		{
			Comment = comment;
			VerticalPosition = verticalPos;
			StreamSpeedScale = streamSpeedScale;

			var actualFontSize = (OverrideApperance?.FontSize ?? BaseApperance?.FontSize).Value;
			StreamSpeedPerSec = CalcStreamSpeed(Comment, StreamSpeedScale, actualFontSize);
		}

		public static float CalcStreamSpeed(string comment, float streamSpeedScale, int fontSize)
		{
			// 半角文字と全角文字で別々にカウントする
			// 半角文字を1、全角文字を2 としてスピード係数に与える
			// fontSizeは minSize から maxSizeまででスピードを補間


			return 0.0f;
		}

		private static float CommentSpeedFromFontSize(int fontSize)
		{
			var roundedFontSize = Math.Min(Math.Max(fontSize, MinFontSize), MaxFontSize);

			return 0.0f;
		}

		const int MaxFontSize = 40;
		const int MinFontSize = 10;
		const float MaxFontSpeedScale = 0.25f;
		const float MinFontSpeedScale = 1.0f;



		public string Comment { get; private set; }
		public int VerticalPosition { get; private set; }
		public float StreamSpeedPerSec { get; set; }
		public float StreamSpeedScale { get; private set; }

		public CommentApperance BaseApperance { get; set; }
		public CommentApperance OverrideApperance { get; private set; }
	}

	public class CommentApperance
	{
		public int? FontSize { get; private set; }
		public uint? Color { get; private set; }
	}

}
