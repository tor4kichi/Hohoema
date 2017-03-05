using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.Views.StateTrigger
{
    public class FocusTrigger : UIIntractionTriggerBase
    {
        protected override void OnAttachTargetHandler(UIElement ui)
        {
            ui.GotFocus += Ui_GotFocus;
            ui.LostFocus += Ui_LostFocus;
        }

        protected override void OnDetachTargetHandler(UIElement ui)
        {
            ui.GotFocus -= Ui_GotFocus;
            ui.LostFocus -= Ui_LostFocus;
        }


        private void Ui_GotFocus(object sender, RoutedEventArgs e)
        {
            SetActiveInvertible(true);
        }

        private void Ui_LostFocus(object sender, RoutedEventArgs e)
        {
            SetActiveInvertible(false);
        }



    }
}
