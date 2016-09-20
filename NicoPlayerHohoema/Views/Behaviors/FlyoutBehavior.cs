using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;

namespace NicoPlayerHohoema.Views.Behaviors
{
	public class FlyoutBehavior : DependencyObject, IBehavior
	{
		public DependencyObject AssociatedObject { get; private set; }

		public void Attach(Windows.UI.Xaml.DependencyObject associatedObject)
		{
			var flyout = associatedObject as FlyoutBase;

			if (flyout == null)
				throw new ArgumentException("FlyoutBehavior can be attached only to FlyoutBase");

			AssociatedObject = associatedObject;

			flyout.Opened += FlyoutOpened;
			flyout.Closed += FlyoutClosed;
		}

		public void Detach()
		{
			var flyout = AssociatedObject as FlyoutBase;

			if (flyout != null)
			{
				flyout.Opened -= FlyoutOpened;
				flyout.Closed -= FlyoutClosed;
			}
		}

		public static readonly DependencyProperty OpenActionsProperty =
			DependencyProperty.Register("OpenActions", typeof(ActionCollection), typeof(FlyoutBehavior), new PropertyMetadata(null));

		public ActionCollection OpenActions
		{
			get { return GetValue(OpenActionsProperty) as ActionCollection; }
			set { SetValue(OpenActionsProperty, value); }
		}

		public static readonly DependencyProperty CloseActionsProperty =
			DependencyProperty.Register("CloseActions", typeof(ActionCollection), typeof(FlyoutBehavior), new PropertyMetadata(null));

		public ActionCollection CloseActions
		{
			get { return GetValue(CloseActionsProperty) as ActionCollection; }
			set { SetValue(CloseActionsProperty, value); }
		}

		private void FlyoutOpened(object sender, object e)
		{
			foreach (IAction action in OpenActions)
			{
				action.Execute(AssociatedObject, null);
			}
		}

		private void FlyoutClosed(object sender, object e)
		{
			foreach (IAction action in CloseActions)
			{
				action.Execute(AssociatedObject, null);
			}
		}

		public FlyoutBehavior()
		{
			OpenActions = new ActionCollection();
			CloseActions = new ActionCollection();
		}
	}
}
