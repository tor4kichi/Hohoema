using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;

namespace NicoPlayerHohoema.Helpers
{
	public static class InternetConnection
	{
		public static bool IsInternet()
		{
			ConnectionProfile connections = NetworkInformation.GetInternetConnectionProfile();
			bool internet = connections != null && connections.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;
			return internet;
		}
	}
}
