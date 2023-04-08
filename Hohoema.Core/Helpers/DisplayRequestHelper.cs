#nullable enable
using Windows.ApplicationModel.Core;
using Windows.System.Display;

namespace Hohoema.Helpers;

public static class DisplayRequestHelper
{
    private static DisplayRequest dispRequest = null;

    public static void RequestKeepDisplay()
    {
        _ = CoreApplication.MainView.DispatcherQueue.TryEnqueue(() =>
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
        _ = CoreApplication.MainView.DispatcherQueue.TryEnqueue(() =>
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
