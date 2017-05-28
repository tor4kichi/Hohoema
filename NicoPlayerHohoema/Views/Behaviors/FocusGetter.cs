using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;


namespace NicoPlayerHohoema.Views.Behaviors
{
    public class FocusGetter : Behavior<UIElement>
    {
        public static readonly DependencyProperty IsFocusProperty =
            DependencyProperty.Register(nameof(IsFocus)
                    , typeof(bool)
                    , typeof(FocusGetter)
                    , new PropertyMetadata(default(bool))
                );

        public bool IsFocus
        {
            get { return (bool)GetValue(IsFocusProperty); }
            set { SetValue(IsFocusProperty, value); }
        }

        protected override void OnAttached()
        {
            this.AssociatedObject.GotFocus += AssociatedObject_GotFocus;
            this.AssociatedObject.LostFocus += AssociatedObject_LostFocus;
            base.OnAttached();
        }

        private void AssociatedObject_LostFocus(object sender, RoutedEventArgs e)
        {
            IsFocus = false;
        }

        private void AssociatedObject_GotFocus(object sender, RoutedEventArgs e)
        {
            IsFocus = true;
        }
    }
}
