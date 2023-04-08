using System;
using Windows.UI.Xaml;

namespace Hohoema.Views.StateTrigger;

public sealed class WindowActiveTrigger : InvertibleStateTrigger, IDisposable
	{
		public WindowActiveTrigger()
		{
			Window.Current.Activated += Current_Activated;
			SetWindowActive(Window.Current.CoreWindow.Visible);
		}

    public void Dispose()
    {
			Window.Current.Activated -= Current_Activated;
		}

    private void Current_Activated(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
		{
			var isWindowActive = e.WindowActivationState != Windows.UI.Core.CoreWindowActivationState.Deactivated;
			SetWindowActive(isWindowActive);
		}

		private void SetWindowActive(bool isWindowActive)
		{
        SetActiveInvertible(isWindowActive);
		}
	}
