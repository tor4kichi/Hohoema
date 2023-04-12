#nullable enable
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;

namespace Hohoema.Views.StateTrigger;

public sealed class IsShowWithSecondaryViewTrigger : StateTriggerBase
{
    public IsShowWithSecondaryViewTrigger()
    {
        var coreApplication = CoreApplication.GetCurrentView();
        SetActive(!coreApplication.IsMain);
    }
}
