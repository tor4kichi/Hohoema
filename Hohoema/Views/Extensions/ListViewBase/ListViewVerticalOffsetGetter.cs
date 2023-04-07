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

namespace Hohoema.Views.Behaviors
{
	public class ListViewVerticalOffsetGetter : Behavior<ListViewBase>, IAction
	{

		#region WithCursor Property 

		public static readonly DependencyProperty VerticalOffsetProperty =
			DependencyProperty.Register("VerticalOffset"
				, typeof(double)
				, typeof(ListViewVerticalOffsetGetter)
				, new PropertyMetadata(0.0)
			);

		public double VerticalOffset
		{
			get { return (double)GetValue(VerticalOffsetProperty); }
			set { SetValue(VerticalOffsetProperty, value); }
		}

		#endregion


		public ScrollViewer _ScrollViewer;

		protected override void OnAttached()
		{
			base.OnAttached();

			// Get scrollviewer

			this.AssociatedObject.Loaded += AssociatedObject_Loaded;
			
		}

        protected override void OnDetaching()
        {
            this.AssociatedObject.Loaded -= AssociatedObject_Loaded;
            base.OnDetaching();
        }

		private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
		{
            _ScrollViewer = AssociatedObject.FindFirstChild<ScrollViewer>();

            if (_ScrollViewer != null)
            {
                _ScrollViewer.ViewChanged += AssociatedObject_ViewChanged;
            }
        }


        bool _NowChangingInViewChanged = false;
		private void AssociatedObject_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
		{
            _NowChangingInViewChanged = true;
            try
            {
                VerticalOffset = _ScrollViewer.VerticalOffset;
            }
            finally
            {
                _NowChangingInViewChanged = false;
            }
        }


        object IAction.Execute(object sender, object parameter)
        {
            VerticalOffset = _ScrollViewer.VerticalOffset;
            return true;
        }
    }


}
