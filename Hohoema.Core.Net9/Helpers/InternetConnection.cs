#nullable enable
using Windows.Networking.Connectivity;

namespace Hohoema.Helpers;

public static class InternetConnection
{
    public static bool IsInternet()
    {
        try
        {
            ConnectionProfile connections = NetworkInformation.GetInternetConnectionProfile();
            bool internet = connections != null && connections.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;
            return internet;
        }
        catch
        {
            return false;
        }

    }
}
