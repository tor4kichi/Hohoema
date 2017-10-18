using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.System.Display;

namespace NicoPlayerHohoema.Helpers
{
	public static class DisplayRequestHelper
	{
		private static DisplayRequest dispRequest = null;

        static AsyncLock _Lock;
        static AsyncLock Lock
        {
            get
            {
                return _Lock
                    ?? (_Lock = new AsyncLock());
            }
        }

		public static async void RequestKeepDisplay()
		{
            await CoreApplication.MainView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                using (var releaser = await Lock.LockAsync())
                {
                    if (dispRequest == null)
                    {

                        // Activate a display-required request. If successful, the screen is 
                        // guaranteed not to turn off automatically due to user inactivity.
                        dispRequest = new DisplayRequest();
                        dispRequest.RequestActive();

                        // Insert your own code here to start the video.

                    }
                }
            });
        }


		public static async void StopKeepDisplay()
		{
            await CoreApplication.MainView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                using (var releaser = await Lock.LockAsync())
                {
                    // Insert your own code here to stop the video.
                    if (dispRequest != null)
                    {

                        // Deactivate the display request and set the var to null.
                        dispRequest.RequestRelease();
                        dispRequest = null;

                    }
                }
            });
		}

	}
}
