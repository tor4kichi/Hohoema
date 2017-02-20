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
    public class FocusFirstItem : Behavior<ItemsControl>
    {

        #region IsEnabled

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                nameof(IsEnabled),
                typeof(bool),
                typeof(FocusFirstItem),
                new PropertyMetadata(default(bool)));

        public bool IsEnabled
        {
            get { return (bool)GetValue(IsEnabledProperty); }
            set { SetValue(IsEnabledProperty, value); }
        }

        #endregion




        private bool _IsEmptyItem = true;

        protected override void OnAttached()
        {
            base.OnAttached();

            this.AssociatedObject.Loaded += AssociatedObject_Loaded;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (this.AssociatedObject != null)
            {
                this.AssociatedObject.Items.VectorChanged -= Items_VectorChanged;
            }
        }


        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            this.AssociatedObject.Items.VectorChanged += Items_VectorChanged;
        }

        private void Items_VectorChanged(Windows.Foundation.Collections.IObservableVector<object> sender, Windows.Foundation.Collections.IVectorChangedEventArgs @event)
        {
            if (_IsEmptyItem)
            {
                if (this.AssociatedObject.Items.Count > 0)
                {
                    var first = this.AssociatedObject.Items.First();
                    var a = this.AssociatedObject.ContainerFromItem(first) as FrameworkElement;
                    if (a != null)
                    {
                        if (IsEnabled)
                        {
                            var control = a.FindFirstChild<Control>();
                            control?.Focus(FocusState.Programmatic);
                        }

                        _IsEmptyItem = false;
                    }
                }
            }
            else
            {
                _IsEmptyItem = this.AssociatedObject.Items.Count == 0;
            }
        }
    }
}
