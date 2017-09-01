using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.Views.StateTrigger
{
    public class EqualConditionStateTrigger : StateTriggerBase
    {
        #region Condition Property

        public static readonly DependencyProperty ConditionProperty =
            DependencyProperty.Register("Condition"
                    , typeof(object)
                    , typeof(EqualConditionStateTrigger)
                    , new PropertyMetadata(null, OnConditiRaisePropertyChanged)
                );

        public object Condition
        {
            get { return GetValue(ConditionProperty); }
            set { SetValue(ConditionProperty, value); }
        }


        public static void OnConditiRaisePropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            var source = sender as EqualConditionStateTrigger;
            source.Evaluation();
        }

        #endregion


        #region Value Property

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value"
                    , typeof(object)
                    , typeof(EqualConditionStateTrigger)
                    , new PropertyMetadata(null, OnValuePropertyChanged)
                );

        public object Value
        {
            get { return GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }


        public static void OnValuePropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            var source = sender as EqualConditionStateTrigger;
            source.Evaluation();
        }

        #endregion

        private void Evaluation()
        {
            if (Value is Enum)
            {
                var isActive = Condition.ToString() == Value?.ToString();
                Debug.WriteLine(Condition.ToString() + ":" + Value?.ToString() + " : " + isActive);
                SetActive(isActive);
            }
            else
            {
                SetActive(Condition == Value);
            }
        }
        
    }
}
