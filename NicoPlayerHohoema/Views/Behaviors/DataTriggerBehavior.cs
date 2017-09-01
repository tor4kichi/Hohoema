using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;

namespace NicoPlayerHohoema.Views.Behaviors
{
    [ContentProperty(Name = "Actions")]
    public class EqualConditionTriggerBehavior : Behavior<FrameworkElement>
    {
        

        public ActionCollection Actions
        {
            get
            {
                if (GetValue(ActionsProperty) == null)
                {
                    return this.Actions = new ActionCollection();
                }
                return (ActionCollection)GetValue(ActionsProperty);
            }
            set { SetValue(ActionsProperty, value); }
        }

        public static readonly DependencyProperty ActionsProperty =
            DependencyProperty.Register(
                nameof(Actions),
                typeof(ActionCollection),
                typeof(EqualConditionTriggerBehavior),
                new PropertyMetadata(null));


        #region Condition Property

        public static readonly DependencyProperty ConditionProperty =
            DependencyProperty.Register("Condition"
                    , typeof(object)
                    , typeof(EqualConditionTriggerBehavior)
                    , new PropertyMetadata(null, OnConditiRaisePropertyChanged)
                );

        public object Condition
        {
            get { return GetValue(ConditionProperty); }
            set { SetValue(ConditionProperty, value); }
        }


        public static void OnConditiRaisePropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            var source = sender as EqualConditionTriggerBehavior;
            source.Evaluation();
        }

        #endregion


        #region Value Property

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value"
                    , typeof(object)
                    , typeof(EqualConditionTriggerBehavior)
                    , new PropertyMetadata(null, OnValuePropertyChanged)
                );

        public object Value
        {
            get { return GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }


        public static void OnValuePropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            var source = sender as EqualConditionTriggerBehavior;
            source.Evaluation();
        }

        #endregion


        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.Loaded += AssociatedObject_Loaded;
        }

        bool _IsLoaded = false;
        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            _IsLoaded = true;
        }

        private void Evaluation()
        {
            if (!_IsLoaded) { return; }

            bool isTrigger = false;
            if (Value is Enum)
            {
                var isActive = Condition.ToString() == Value?.ToString();
                Debug.WriteLine(Condition.ToString() + ":" + Value?.ToString() + " : " + isActive);
                isTrigger = isActive;
            }
            else if (Value is Boolean)
            {
                if (Condition is string)
                {
                    isTrigger = bool.Parse(Condition as string) == (bool)Value;
                }
                else if (Condition is bool)
                {
                    isTrigger = (bool)Condition == (bool)Value;
                }
                else
                {
                    isTrigger = Condition == Value;
                }
            }
            else
            {
                isTrigger = Condition == Value;
            }


            if (isTrigger)
            {
                Interaction.ExecuteActions(this, this.Actions, null);
            }
        }

    }
}
