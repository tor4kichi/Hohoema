using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;

namespace Hohoema.Presentation.Views.StateTrigger
{
    public sealed class FlyoutIsOpenStateTrigger : InvertibleStateTrigger, IDisposable
    {
        public FlyoutBase TargetFlyout
        {
            get { return (FlyoutBase)GetValue(TargetFlyoutProperty); }
            set { SetValue(TargetFlyoutProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TargetFlyoutProperty =
            DependencyProperty.Register("TargetFlyout", typeof(FlyoutBase), typeof(FlyoutIsOpenStateTrigger), new PropertyMetadata(null, OnFlyoutChanged));

        private static void OnFlyoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var _this = (FlyoutIsOpenStateTrigger)d;
            if (e.NewValue is FlyoutBase newflyout)
            {
                newflyout.Opening += _this.Newflyout_Opening;
                newflyout.Closing += _this.Newflyout_Closing;
            }
        }

        private void Newflyout_Opening(object sender, object e)
        {
            SetActiveInvertible(true);
        }

        private void Newflyout_Closing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
        {
            SetActiveInvertible(false);
        }

        public void Dispose()
        {
            TargetFlyout.Opening -= Newflyout_Opening;
            TargetFlyout.Closing -= Newflyout_Closing;
        }
    }
}
