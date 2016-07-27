using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NicoPlayerHohoema.Views.Behaviors
{
	public class SplitViewClosePane : Behavior<DependencyObject>, IAction
	{
		#region IsEnable Property

		public static readonly DependencyProperty SplitViewProperty =
			DependencyProperty.Register(
					nameof(SplitView)
					, typeof(SplitView)
					, typeof(SplitViewClosePane)
					, new PropertyMetadata(default(SplitView))
				);

		public SplitView SplitView
		{
			get { return (SplitView)GetValue(SplitViewProperty); }
			set { SetValue(SplitViewProperty, value); }
		}

		#endregion


		#region IsEnable Property

		public static readonly DependencyProperty IsEnableProperty =
			DependencyProperty.Register(
					nameof(IsEnable)
					, typeof(bool)
					, typeof(SplitViewClosePane)
					, new PropertyMetadata(true)
				);

		public bool IsEnable
		{
			get { return (bool)GetValue(IsEnableProperty); }
			set { SetValue(IsEnableProperty, value); }
		}

		#endregion



		protected override void OnAttached()
		{
			base.OnAttached();

		}

		

		protected override void OnDetaching()
		{
			base.OnDetaching();

		}

		public object Execute(object sender, object parameter)
		{
			if (SplitView != null && IsEnable)
			{
				if (SplitView.DisplayMode != SplitViewDisplayMode.Inline)
				{
					SplitView.IsPaneOpen = false;
				}
			}

			return true;
		}
	}
}
