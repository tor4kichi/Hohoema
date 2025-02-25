﻿#nullable enable
using Windows.UI.Xaml;

namespace Hohoema.Views.StateTrigger;

public class PointerFocusTrigger : UIIntractionTriggerBase
{
    protected override void OnAttachTargetHandler(UIElement ui)
    {
        ui.PointerEntered += Ui_PointerEntered;
        ui.PointerExited += Ui_PointerExited;
    }

    protected override void OnDetachTargetHandler(UIElement ui)
    {
        ui.PointerEntered -= Ui_PointerEntered;
        ui.PointerExited -= Ui_PointerExited;
    }


    private void Ui_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        SetActiveInvertible(true);
    }

    private void Ui_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        SetActiveInvertible(false);
    }
}
