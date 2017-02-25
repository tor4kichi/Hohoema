using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Input;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.Views.Behaviors
{
	public class MouseWheelTrigger : Behavior<FrameworkElement>
    {
		public ActionCollection UpActions 
		{
			get
			{
				if (GetValue(UpActionsProperty) == null)
				{
					this.UpActions = new ActionCollection();
				}
				return (ActionCollection)GetValue(UpActionsProperty);
			}
			set { SetValue(UpActionsProperty, value); }
		}

		public static readonly DependencyProperty UpActionsProperty =
			DependencyProperty.Register(
				nameof(UpActions),
				typeof(ActionCollection),
				typeof(MouseWheelTrigger),
				new PropertyMetadata(null));



		public ActionCollection DownActions
		{
			get
			{
				if (GetValue(DownActionsProperty) == null)
				{
					this.DownActions = new ActionCollection();
				}
				return (ActionCollection)GetValue(DownActionsProperty);
			}
			set { SetValue(DownActionsProperty, value); }
		}

		public static readonly DependencyProperty DownActionsProperty =
			DependencyProperty.Register(
				nameof(DownActions),
				typeof(ActionCollection),
				typeof(MouseWheelTrigger),
				new PropertyMetadata(null));

        protected override void OnAttached()
        {
            this.Register();
        }

        protected override void OnDetaching()
        {
            this.Unregister();
        }


		private void Register()
		{
			var fe = this.AssociatedObject as FrameworkElement;
			if (fe == null) { return; }
			fe.Unloaded += this.Fe_Unloaded;


			if (AssociatedObject is UIElement)
			{
				var ui = AssociatedObject as UIElement;
				ui.PointerWheelChanged += Ui_PointerWheelChanged; ;
			}
			else
			{
				Window.Current.CoreWindow.PointerWheelChanged += CoreWindow_PointerWheelChanged;
			}
		}

		private void Ui_PointerWheelChanged(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs args)
		{
			var pointer = args.GetCurrentPoint(null);
			
			if (pointer.Properties.MouseWheelDelta > 0)
			{
				Interaction.ExecuteActions(this, this.UpActions, args);
				args.Handled = true;
			}
			else if (pointer.Properties.MouseWheelDelta < 0)
			{
				Interaction.ExecuteActions(this, this.DownActions, args);
				args.Handled = true;
			}

		}

		private void CoreWindow_PointerWheelChanged(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.PointerEventArgs args)
		{
			var pointer = args.CurrentPoint;

			
			if (pointer.Properties.MouseWheelDelta > 0)
			{
				Interaction.ExecuteActions(this, this.UpActions, args);
				args.Handled = true;
			}
			else if (pointer.Properties.MouseWheelDelta < 0)
			{
				Interaction.ExecuteActions(this, this.DownActions, args);
				args.Handled = true;
			}
		}


		private void Process(PointerPoint pp)
		{

		}

		private void Unregister()
		{
			var fe = this.AssociatedObject as FrameworkElement;
			if (fe == null) { return; }
			fe.Unloaded -= this.Fe_Unloaded;

			Window.Current.CoreWindow.PointerWheelChanged -= CoreWindow_PointerWheelChanged;

		}


		

		private void Fe_Unloaded(object sender, RoutedEventArgs e)
		{
			this.Unregister();
		}
	}
}
