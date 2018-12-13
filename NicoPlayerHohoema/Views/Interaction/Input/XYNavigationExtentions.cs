using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace NicoPlayerHohoema.Views.Behaviors
{
    public class XYNavigationExtentions : DependencyObject
    {
        #region FocusAcceptOrientation


        public static readonly DependencyProperty FocusAcceptOrientationProperty =
            DependencyProperty.RegisterAttached(
                "FocusAcceptOrientation",
                typeof(Orientation),
                typeof(XYNavigationExtentions),
                new PropertyMetadata(null, OnFocusAcceptOrientatiRaisePropertyChanged)
        );

        public static void SetFocusAcceptOrientation(UIElement element, Orientation value)
        {
            element.SetValue(FocusAcceptOrientationProperty, value);
        }
        public static Orientation GetFocusAcceptOrientation(UIElement element)
        {
            return (Orientation)element.GetValue(FocusAcceptOrientationProperty);
        }


        public static void OnFocusAcceptOrientatiRaisePropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            // UIElement.GettingFocus is need UAC 4.0
            if (!ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4))
            {
                return;
            }

            if (sender is UIElement)
            {
                var fe = sender as UIElement;
                fe.GettingFocus -= OnTargetUIGettingFocus;
                fe.GettingFocus += OnTargetUIGettingFocus;
            }
        }

        private static void OnTargetUIGettingFocus(UIElement sender, GettingFocusEventArgs args)
        {
            // does not prevent if inside TargetUI focus movement.
            if (IsInsidePanel(args.OldFocusedElement, sender))
            {
                return;
            }
                
            
            var orientation = GetFocusAcceptOrientation(sender);
            switch (orientation)
            {
                case Orientation.Vertical:
                    if (args.Direction == FocusNavigationDirection.Right
                        || args.Direction == FocusNavigationDirection.Left)
                    {
                        args.Cancel = true;
                    }
                    break;
                case Orientation.Horizontal:
                    if (args.Direction == FocusNavigationDirection.Up 
                        || args.Direction == FocusNavigationDirection.Down)
                    {
                        args.Cancel = true;
                    }
                    break;
            }
        }

        private static bool IsInsidePanel(DependencyObject target, UIElement panel)
        {
            var parent = target;
            while (parent != null)
            {
                if (panel == parent)
                {
                    return true;
                }
                 
                parent = VisualTreeHelper.GetParent(parent);
            }

            return false;
        }

        #endregion

        #region KeepFocus

        public static readonly DependencyProperty FirstFocusProperty =
            DependencyProperty.RegisterAttached(
                "FirstFocus",
                typeof(Control),
                typeof(XYNavigationExtentions),
                new PropertyMetadata(null, OnFirstFocusPropertyChanged)
        );

        public static void SetFirstFocus(DependencyObject element, Control value)
        {
            element.SetValue(FirstFocusProperty, value);
        }
        public static Control GetFirstFocus(DependencyObject element)
        {
            return (Control)element.GetValue(FirstFocusProperty);
        }


        public static void OnFirstFocusPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            if (sender is UserControl && args.NewValue != null)
            {
                var control = sender as Control;
                var firstFocusElement = args.NewValue as Control;
                control.Loaded += (s, e) => 
                {
                    if (firstFocusElement == null)
                    {
                        control.Focus(FocusState.Programmatic);
                    }
                    else
                    {
                        firstFocusElement.Focus(FocusState.Programmatic);
                    }
                };
            }
        }

        private static void Control_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var focused = FocusManager.GetFocusedElement();
            var control = sender as Control;
            if ((bool)e.NewValue == false && control == focused)
            {
                FocusManager.TryMoveFocus(FocusNavigationDirection.Next);
            }
        }

        #endregion


    }
}
