#if WINDOWS_UWP
#else
using System.Net;
using System.Net.Http;
#endif


namespace NiconicoToolkit.Account
{
    public enum NiconicoAccountAuthority
    {
        NotSignedIn = 0,
        Normal = 1,
        Premium = 3,
    }
}
