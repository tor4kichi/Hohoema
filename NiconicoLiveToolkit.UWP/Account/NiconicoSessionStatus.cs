#if WINDOWS_UWP
#else
using System.Net;
using System.Net.Http;
#endif


namespace NiconicoToolkit.Account
{
    public enum NiconicoSessionStatus
    {
        ServiceUnavailable = -2,
        Failed = -1,
        Success = 1,
        RequireTwoFactorAuth = 2,
    }
}
