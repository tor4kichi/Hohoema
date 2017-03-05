using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace NicoPlayerHohoema.Views.Behaviors
{
	public class ListViewVerticalOffsetGetter : Behavior<ListView>
	{

		#region WithCursor Property 

		public static readonly DependencyProperty VerticalOffsetProperty =
			DependencyProperty.Register("VerticalOffset"
				, typeof(double)
				, typeof(ListViewVerticalOffsetGetter)
				, new PropertyMetadata(0.0, OnVerticalOffsetPropertyChanged)
			);

		public double VerticalOffset
		{
			get { return (double)GetValue(VerticalOffsetProperty); }
			set { SetValue(VerticalOffsetProperty, value); }
		}

		public static void OnVerticalOffsetPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
		{
			var source = (ListViewVerticalOffsetGetter)sender;

			source.ResetVerticalOffset();
		}


		#endregion


		public ScrollViewer _ScrollViewer;

		protected override void OnAttached()
		{
			base.OnAttached();

			// Get scrollviewer

			this.AssociatedObject.Loaded += AssociatedObject_Loaded;
			
		}


		private void ResetVerticalOffset()
		{
			if (_ScrollViewer != null)
			{
				_ScrollViewer.ChangeView(null, VerticalOffset, null, false);
			}
		}

		private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
		{
            _ScrollViewer = AssociatedObject.FindFirstChild<ScrollViewer>();

            if (_ScrollViewer != null)
            {
                _ScrollViewer.ViewChanged += AssociatedObject_ViewChanged;

                ResetVerticalOffset();
            }
        }



		private void AssociatedObject_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
		{
			VerticalOffset = _ScrollViewer.VerticalOffset;
		}
		
	}


}
