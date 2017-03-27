using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;

namespace NicoPlayerHohoema.Views.Behaviors
{
    [ContentProperty(Name = "Actions")]
    public class KeyboardTrigger : DependencyObject, IBehavior
	{
		public DependencyObject AssociatedObject { get; private set; }

		public ActionCollection Actions
		{
			get
			{
				if (GetValue(ActionsProperty) == null)
				{
					this.Actions = new ActionCollection();
				}
				return (ActionCollection)GetValue(ActionsProperty);
			}
			set { SetValue(ActionsProperty, value); }
		}

		public static readonly DependencyProperty ActionsProperty =
			DependencyProperty.Register(
				nameof(Actions),
				typeof(ActionCollection),
				typeof(KeyboardTrigger),
				new PropertyMetadata(null));

		public bool ShiftKey
		{
			get { return (bool)GetValue(ShiftKeyProperty); }
			set { SetValue(ShiftKeyProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ShiftKey.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ShiftKeyProperty =
			DependencyProperty.Register("ShiftKey", typeof(bool), typeof(KeyboardTrigger), new PropertyMetadata(false));

		public bool CtrlKey
		{
			get { return (bool)GetValue(CtrlKeyProperty); }
			set { SetValue(CtrlKeyProperty, value); }
		}

		// Using a DependencyProperty as the backing store for CtrlKey.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CtrlKeyProperty =
			DependencyProperty.Register("CtrlKey", typeof(bool), typeof(KeyboardTrigger), new PropertyMetadata(false));

		public VirtualKey Key
		{
			get { return (VirtualKey)GetValue(KeyProperty); }
			set { SetValue(KeyProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Key.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty KeyProperty =
			DependencyProperty.Register("Key", typeof(VirtualKey), typeof(KeyboardTrigger), new PropertyMetadata(VirtualKey.None));


		public bool UseKeyUp
		{
			get { return (bool)GetValue(UseKeyUpProperty); }
			set { SetValue(UseKeyUpProperty, value); }
		}

		public static readonly DependencyProperty UseKeyUpProperty =
			DependencyProperty.Register("UseKeyUp", typeof(bool), typeof(KeyboardTrigger), new PropertyMetadata(false));



		public bool IsEnabled
		{
			get { return (bool)GetValue(IsEnabledProperty); }
			set { SetValue(IsEnabledProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Key.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsEnabledProperty =
			DependencyProperty.Register("IsEnabled", typeof(bool), typeof(KeyboardTrigger), new PropertyMetadata(true));

		public void Attach(DependencyObject associatedObject)
		{
			this.AssociatedObject = associatedObject;
			this.Register();
		}

		public void Detach()
		{
			this.Unregister();
			this.AssociatedObject = null;
		}

		private void Register()
		{
			var fe = this.AssociatedObject as FrameworkElement;
			if (fe == null) { return; }
			fe.Unloaded += this.Fe_Unloaded;
			if (!UseKeyUp)
			{
				Window.Current.CoreWindow.KeyDown += this.CoreWindow_KeyDown;
			}
			else
			{
				Window.Current.CoreWindow.KeyUp += this.CoreWindow_KeyDown;
			}
		}

		private void Unregister()
		{
			var fe = this.AssociatedObject as FrameworkElement;
			if (fe == null) { return; }
			fe.Unloaded -= this.Fe_Unloaded;

			if (!UseKeyUp)
			{
				Window.Current.CoreWindow.KeyDown -= this.CoreWindow_KeyDown;
			}
			else
			{
				Window.Current.CoreWindow.KeyUp -= this.CoreWindow_KeyDown;
			}
		}

		private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
		{
			if (!IsEnabled) { return; }
			if (args.Handled) { return; }

			if (this.ShiftKey && (Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift) & CoreVirtualKeyStates.Down) != CoreVirtualKeyStates.Down)
			{
				return;
			}

			if (this.CtrlKey && (Window.Current.CoreWindow.GetKeyState(VirtualKey.Control) & CoreVirtualKeyStates.Down) != CoreVirtualKeyStates.Down)
			{
				return;
			}

			if (this.Key == args.VirtualKey)
			{
                foreach (var action in this.Actions)
                {
                    (action as IAction)?.Execute(this, args);
                }

				args.Handled = true;
			}
		}

		private void Fe_Unloaded(object sender, RoutedEventArgs e)
		{
			this.Unregister();
		}
	}
}
