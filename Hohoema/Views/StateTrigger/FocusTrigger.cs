using Microsoft.Toolkit.Uwp.Helpers;
using System.Diagnostics;
using Windows.UI.Xaml;

namespace Hohoema.Views.StateTrigger;

public class FocusTrigger : UIIntractionTriggerBase
{
    private WeakEventListener<UIElement, object, RoutedEventArgs> _gotFocus_we;
    private WeakEventListener<UIElement, object, RoutedEventArgs> _lostFocus_we;

    protected override void OnAttachTargetHandler(UIElement ui)
    {
        _gotFocus_we = new WeakEventListener<UIElement, object, RoutedEventArgs>(ui)
        {
            OnEventAction = (inst, s, e) => Ui_GotFocus(s, e)
        };

        _lostFocus_we = new WeakEventListener<UIElement, object, RoutedEventArgs>(ui)
        {
            OnEventAction = (inst, s, e) => Ui_LostFocus(s, e)
        };

        ui.GotFocus += _gotFocus_we.OnEvent;
        ui.LostFocus += _lostFocus_we.OnEvent;
    }

    protected override void OnDetachTargetHandler(UIElement ui)
    {
        _gotFocus_we.Detach();
        _lostFocus_we.Detach();
    }


    private void Ui_GotFocus(object sender, RoutedEventArgs e)
    {
        SetActiveInvertible(true);

        Debug.WriteLine("GotFocus");
    }

    private void Ui_LostFocus(object sender, RoutedEventArgs e)
    {
        SetActiveInvertible(false);

        Debug.WriteLine("LostFocus");
    }



}
