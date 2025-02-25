﻿#nullable enable
using Microsoft.Xaml.Interactivity;
using System.Linq;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;

namespace Hohoema.Views.Behaviors;

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


    public bool IsEnableUINavigationButtons
    {
        get { return (bool)GetValue(IsEnableUINavigationButtonsProperty); }
        set { SetValue(IsEnableUINavigationButtonsProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Key.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsEnableUINavigationButtonsProperty =
        DependencyProperty.Register("IsEnableUINavigationButtons", typeof(bool), typeof(KeyboardTrigger), new PropertyMetadata(false));




    bool _NowFocusing_OnlyWhenFocus = false;

    public FrameworkElement OnlyWhenFocus
    {
        get { return (FrameworkElement)GetValue(OnlyWhenFocusProperty); }
        set { SetValue(OnlyWhenFocusProperty, value); }
    }

    // trigger timing limitation when focused in 
    public static readonly DependencyProperty OnlyWhenFocusProperty =
        DependencyProperty.Register(nameof(OnlyWhenFocus), typeof(FrameworkElement), typeof(KeyboardTrigger), new PropertyMetadata(default(FrameworkElement), OnIsAutoHideEnabledPropertyChanged));

    public static void OnIsAutoHideEnabledPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
    {
        var source = (KeyboardTrigger)sender;

        source._NowFocusing_OnlyWhenFocus = false;

        if (args.OldValue is FrameworkElement oldControl)
        {
            oldControl.GotFocus -= source.OldControl_GotFocus;
            oldControl.LostFocus -= source.OldControl_LostFocus;
        }

        if (args.NewValue is FrameworkElement newControl)
        {
            newControl.GotFocus += source.OldControl_GotFocus;
            newControl.LostFocus += source.OldControl_LostFocus;
        }
    }

    private void OldControl_GotFocus(object sender, RoutedEventArgs e)
    {
        _NowFocusing_OnlyWhenFocus = true;
    }

    private void OldControl_LostFocus(object sender, RoutedEventArgs e)
    {
        _NowFocusing_OnlyWhenFocus = false;
    }





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
            fe.KeyDown += ProcessKeyAction;
			}
			else
			{
            fe.KeyUp += ProcessKeyAction;
			}
		}


    private void Unregister()
		{
			var fe = this.AssociatedObject as FrameworkElement;
			if (fe == null) { return; }
			fe.Unloaded -= this.Fe_Unloaded;

        fe.KeyDown -= ProcessKeyAction;
        fe.KeyUp -= ProcessKeyAction;
    }

    static readonly VirtualKey[] NavigationButtonVirtualKeyList = new[]
    {
        VirtualKey.NavigationAccept,
        VirtualKey.NavigationCancel,
        VirtualKey.NavigationDown,
        VirtualKey.NavigationLeft,
        VirtualKey.NavigationMenu,
        VirtualKey.NavigationRight,
        VirtualKey.NavigationUp,
        VirtualKey.NavigationView,
        VirtualKey.GamepadA,
        VirtualKey.GamepadB,
        VirtualKey.GamepadDPadDown,
        VirtualKey.GamepadDPadLeft,
        VirtualKey.GamepadDPadRight,
        VirtualKey.GamepadDPadUp,
        VirtualKey.GamepadLeftShoulder,
        VirtualKey.GamepadLeftThumbstickButton,
        VirtualKey.GamepadLeftThumbstickDown,
        VirtualKey.GamepadLeftThumbstickLeft,
        VirtualKey.GamepadLeftThumbstickRight,
        VirtualKey.GamepadLeftThumbstickUp,
        VirtualKey.GamepadLeftTrigger,
        VirtualKey.GamepadMenu,
        VirtualKey.GamepadRightShoulder,
        VirtualKey.GamepadRightThumbstickButton,
        VirtualKey.GamepadRightThumbstickDown,
        VirtualKey.GamepadRightThumbstickLeft,
        VirtualKey.GamepadRightThumbstickRight,
        VirtualKey.GamepadRightThumbstickUp,
        VirtualKey.GamepadRightTrigger,
        VirtualKey.GamepadView,
        VirtualKey.GamepadX,
        VirtualKey.GamepadY,
    };

    // https://msdn.microsoft.com/ja-jp/library/windows/desktop/dd375731(v=vs.85).aspx
    static readonly int[] IgnoreVirtualKeyList = new[] 
    {
        0xAD,  // Volume Mute key
        0xAE,  // Volume Down key
        0xAF,  // Volume Up key
        0xB0,  // Next Track key
        0xB1,  // Previous Track key
        0xB2,  // Stop Media key
        0xB3,  // Play/Pause Media key
    };

    private void ProcessKeyAction(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs args)
    {
        if (OnlyWhenFocus != null && !_NowFocusing_OnlyWhenFocus) { return; }
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

        if (!IsEnableUINavigationButtons)
        {
            if (NavigationButtonVirtualKeyList.Any(x => x == args.Key)) { return; }

            var keycode = (int)args.Key;
            if (IgnoreVirtualKeyList.Any(x => x == keycode)) { return; }
        }

        if (this.Key == VirtualKey.None || this.Key == args.Key)
        {
            foreach (var action in this.Actions.Cast<IAction>())
            {
                var result = action.Execute(this, null);
                if (result is bool)
                {
                    var isExecuted = (bool)result;
                    if (!isExecuted)
                    {
                        return;
                    }
                    args.Handled = true;
                }
                else
                {
                    args.Handled = true;
                }
            }
        }
    }


		private void Fe_Unloaded(object sender, RoutedEventArgs e)
		{
			this.Unregister();
		}
	}
