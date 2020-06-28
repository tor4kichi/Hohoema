using Microsoft.Toolkit.Uwp.UI.Animations.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Hohoema.Views.Interaction.Behaviors
{
    public sealed class ListViewScrollFollowsToItemBehavior : BehaviorBase<ListViewBase>
    {


        public object ScrollTargetItem
        {
            get { return (object)GetValue(ScrollTargetItemProperty); }
            set { SetValue(ScrollTargetItemProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ScrollTargetItem.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ScrollTargetItemProperty =
            DependencyProperty.Register("ScrollTargetItem", typeof(object), typeof(ListViewScrollFollowsToItemBehavior), new PropertyMetadata(null, OnScrollTargetItemChanged));




        public bool IsEnabled
        {
            get { return (bool)GetValue(IsEnabledProperty); }
            set { SetValue(IsEnabledProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsEnabled.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.Register("IsEnabled", typeof(bool), typeof(ListViewScrollFollowsToItemBehavior), new PropertyMetadata(true));



        private static void OnScrollTargetItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var _this = (ListViewScrollFollowsToItemBehavior)d;
            _this.ScrollTo();
        }

        private void ScrollTo()
        {
            if (AssociatedObject == null) { return; }
            if (!IsEnabled) { return; }

            var target = ScrollTargetItem;
            if (target != null)
            {
                AssociatedObject.ScrollIntoView(target, ScrollIntoViewAlignment.Default);
            }
        }

        protected override void OnAssociatedObjectLoaded()
        {
            base.OnAssociatedObjectLoaded();
        }
    }
}
