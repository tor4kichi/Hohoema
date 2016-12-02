using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.Views.StateTrigger
{
    abstract public class UIIntractionTriggerBase : InvertibleStateTrigger
    {
        #region Target Property

        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register("Target"
                    , typeof(UIElement)
                    , typeof(UIIntractionTriggerBase)
                    , new PropertyMetadata(default(UIElement), OnTargetPropertyChanged)
                );

        public UIElement Target
        {
            get { return (UIElement)GetValue(TargetProperty); }
            set { SetValue(TargetProperty, value); }
        }


        public static void OnTargetPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            UIIntractionTriggerBase source = (UIIntractionTriggerBase)sender;

            if (args.OldValue != null)
            {
                var ui = args.OldValue as UIElement;
                source.OnDetachTargetHandler(ui);
            }

            if (args.NewValue != null)
            {
                var ui = args.NewValue as UIElement;
                source.OnAttachTargetHandler(ui);
                source.SetActiveInvertible(false);
            }
        }

        #endregion

        protected abstract void OnAttachTargetHandler(UIElement ui);
        protected abstract void OnDetachTargetHandler(UIElement ui);
    }
}
