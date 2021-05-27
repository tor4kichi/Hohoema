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
        UnknonwAccountAuthority_2 = 2,
        Premium = 3,
        UnknonwAccountAuthority_4 = 4,
        UnknonwAccountAuthority_5 = 5,
    }
}
