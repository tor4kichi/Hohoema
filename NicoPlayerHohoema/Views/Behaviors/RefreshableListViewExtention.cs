using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.Views.Behaviors
{
	public class RefreshableListViewRefreshableGetter : Behavior<RefreshableListView.RefreshableListView>
	{
		public static readonly DependencyProperty IsRefreshableProperty =
		DependencyProperty.RegisterAttached(
			nameof(IsRefreshable),
			typeof(bool),
			typeof(RefreshableListViewRefreshableGetter),
			new PropertyMetadata(default(bool)));

		// プログラムからアクセスするための添付プロパティのラッパー
		public bool IsRefreshable
		{
			get { return (bool)GetValue(IsRefreshableProperty); }
			set { SetValue(IsRefreshableProperty, value); }
		}


		protected override void OnAttached()
		{
			base.OnAttached();

			this.AssociatedObject.PullProgressChanged += AssociatedObject_PullProgressChanged;
		}

		protected override void OnDetaching()
		{
			base.OnDetaching();
			this.AssociatedObject.PullProgressChanged -= AssociatedObject_PullProgressChanged;
		}


		private void AssociatedObject_PullProgressChanged(object sender, RefreshableListView.RefreshProgressEventArgs e)
		{
			IsRefreshable = e.IsRefreshable;
		}


	}
}
