using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Hohoema.Presentation.Views.StateTrigger
{
	public class WindowActiveTrigger : InvertibleStateTrigger, IDisposable
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
}
