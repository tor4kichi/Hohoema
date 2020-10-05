using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Toolkit.Uwp.UI.Animations.Behaviors;


namespace Hohoema.Presentation.Views.Controls
{
    /// <summary>
    /// Scroll header control to be used with ListViews or GridViews
    /// </summary>
    public class ScrollHeader : ContentControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScrollHeader"/> class.
        /// </summary>
        public ScrollHeader()
        {
            DefaultStyleKey = typeof(ScrollHeader);
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
        }

        /// <summary>
        /// Identifies the <see cref="Mode"/> property.
        /// </summary>
        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register(nameof(Mode), typeof(ScrollHeaderMode), typeof(ScrollHeader), new PropertyMetadata(ScrollHeaderMode.None, OnModeChanged));

        /// <summary>
        /// Gets or sets a value indicating whether the current mode.
        /// Default is none.
        /// </summary>
        public ScrollHeaderMode Mode
        {
            get { return (ScrollHeaderMode)GetValue(ModeProperty); }
            set { SetValue(ModeProperty, value); }
        }

        /// <summary>
        /// Invoked whenever application code or internal processes (such as a rebuilding layout pass) call <see cref="Control.ApplyTemplate"/>.
        /// </summary>
        protected override void OnApplyTemplate()
        {
            UpdateScrollHeaderBehavior();
        }

        private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as ScrollHeader)?.UpdateScrollHeaderBehavior();
        }

        private static void OnTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as ScrollHeader)?.OnApplyTemplate();
        }

        private void UpdateScrollHeaderBehavior()
        {
            var targetListViewBase = this.FindAscendant<Windows.UI.Xaml.Controls.ListViewBase>();

            if (targetListViewBase == null)
            {
                return;
            }

            // Remove previous behaviors
            foreach (var behavior in Microsoft.Xaml.Interactivity.Interaction.GetBehaviors(targetListViewBase))
            {
                if (behavior is FadeHeaderBehavior || behavior is QuickReturnHeaderBehavior || behavior is StickyHeaderBehavior)
                {
                    Microsoft.Xaml.Interactivity.Interaction.GetBehaviors(targetListViewBase).Remove(behavior);
                }
            }

            switch (Mode)
            {
                case ScrollHeaderMode.None:
                    break;
                case ScrollHeaderMode.QuickReturn:
                    Microsoft.Xaml.Interactivity.Interaction.GetBehaviors(targetListViewBase).Add(new Hohoema.Presentation.Views.Controls.QuickReturnHeaderBehavior());
                    break;
                case ScrollHeaderMode.Sticky:
                    Microsoft.Xaml.Interactivity.Interaction.GetBehaviors(targetListViewBase).Add(new Hohoema.Presentation.Views.Controls.StickyHeaderBehavior());
                    break;
                case ScrollHeaderMode.Fade:
                    Microsoft.Xaml.Interactivity.Interaction.GetBehaviors(targetListViewBase).Add(new FadeHeaderBehavior());
                    break;
            }
        }
    }
}
