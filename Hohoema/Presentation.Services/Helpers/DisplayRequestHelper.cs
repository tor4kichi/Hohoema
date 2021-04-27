using Hohoema.Models.Domain.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.System;
using Windows.System.Display;

namespace Hohoema.Presentation.Services.Helpers
{
	public static class DisplayRequestHelper
	{
		private static DisplayRequest dispRequest = null;

		public static void RequestKeepDisplay()
		{
            CoreApplication.MainView.DispatcherQueue.TryEnqueue(() => 
            {
                if (dispRequest == null)
                {
                    // Activate a display-required request. If successful, the screen is 
                    // guaranteed not to turn off automatically due to user inactivity.
                    dispRequest = new DisplayRequest();
                    dispRequest.RequestActive();

                    // Insert your own code here to start the video.

                }
            });
        }


		public static void StopKeepDisplay()
		{
            CoreApplication.MainView.DispatcherQueue.TryEnqueue(() => 
            {
                // Insert your own code here to stop the video.
                if (dispRequest != null)
                {

                    // Deactivate the display request and set the var to null.
                    dispRequest.RequestRelease();
                    dispRequest = null;

                }
                
            });
		}

	}
}
