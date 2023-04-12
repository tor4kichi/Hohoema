#nullable enable
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Hohoema.Helpers;
using Hohoema.Infra;
using Hohoema.Models.Application;
using NiconicoToolkit.Account;
using NiconicoToolkit.User;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Networking.Connectivity;
using Windows.System;
using AsyncLock = Hohoema.Helpers.AsyncLock;

namespace Hohoema.Models.Niconico;

public enum LoginFailedReason
{
    RequireTwoFactorAuth,
    InvalidMailOrPassword,
    ServiceNotAvailable,
    OfflineNetwork,
}

public struct NiconicoSessionLoginEventArgs
{
    public uint UserId { get; set; }
    public bool IsPremium { get; set; }

    public string UserName { get; set; }
    public string UserIconUrl { get; set; }
}

public class NiconicoSessionLoginRequireTwoFactorAuthResponse
{
    public NiconicoSessionLoginRequireTwoFactorAuthResponse(string code, bool isTrustDevice, string deviceName)
    {
        Code = code;
        IsTrustDevice = isTrustDevice;
        DeviceName = deviceName;
    }

    public string Code { get; }
    public bool IsTrustDevice { get; }
    public string DeviceName { get; }
}


public class NiconicoSessionLoginRequireTwoFactorAsyncRequestMessage : AsyncRequestMessage<NiconicoSessionLoginRequireTwoFactorAuthResponse>
{
    public NiconicoSessionLoginRequireTwoFactorAsyncRequestMessage(Uri location, NiconicoToolkit.NiconicoContext context)
    {
        TwoFactorAuthLocation = location;
        Context = context;
    }

    public Uri TwoFactorAuthLocation { get; }
    public NiconicoToolkit.NiconicoContext Context { get; }
}

public struct NiconicoSessionLoginErrorEventArgs
{
    public LoginFailedReason LoginFailedReason { get; set; }
    public Exception Exception { get; set; }
}

public sealed class NiconicoSession : ObservableObject
{
    public NiconicoSession(
        IMessenger messenger
        )
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        UpdateServiceStatus(NiconicoSessionStatus.Failed);

        ToolkitContext = new NiconicoToolkit.NiconicoContext(HohoemaUserAgent);
        ToolkitContext.SetupDefaultRequestHeaders();

        _messenger = messenger;
    }


    private void OnNetworkStatusChanged(object sender)
    {
        // Note: Resumingのタイミングで NetworkInformation.NetworkStatusChanged += OnNetworkStatusChanged; をやるとExcpetion HRESULT: 

        if (skipOnceNetworkStatusChange)
        {
            skipOnceNetworkStatusChange = false;
            return;
        }

        if (Helpers.InternetConnection.IsInternet())
        {
            HohoemaAppServiceLevel lastStatus = ServiceStatus;
            _ = _dispatcherQueue.TryEnqueue(async () =>
            {
                if (lastStatus == HohoemaAppServiceLevel.LoggedIn)
                {
                    NiconicoSessionStatus status = await CheckSignedInStatus();

                    if (status == NiconicoSessionStatus.Failed)
                    {
                        status = await SignInWithPrimaryAccount();
                    }
                }
                else
                {
                    // ログインセッションが時間切れしていた場合、自動でログイン状態に復帰する
                    //                        await SignInWithPrimaryAccount();
                }
            });
        }
        else
        {
        }
    }

    private bool skipOnceNetworkStatusChange;

    private void CoreApplication_Resuming(object sender, object e)
    {
        skipOnceNetworkStatusChange = true;
    }

    private void CoreApplication_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
    {
        skipOnceNetworkStatusChange = true;
    }


    public const string HohoemaUserAgent = "https://github.com/tor4kichi/Hohoema";
    private readonly IMessenger _messenger;
    private readonly DispatcherQueue _dispatcherQueue;


    // Events 
    // Login UserId, UserName, isPremium
    // Logout 
    // LoginError exception, またはエラー理由のテキスト

    public event EventHandler<NiconicoSessionLoginEventArgs> LogIn;
    public event EventHandler LogOut;
    public event EventHandler<NiconicoSessionLoginErrorEventArgs> LogInFailed;


    // ServiceStatusとIsLoggedInは別々に管理する
    // ServiceStatus == LoggedIn を取ればIsLoggedInを構成できるが
    // 仮にログイン済みセッションを保持した状態でネットワークオフライン → オンライン復帰 といった状況が発生した場合に
    // オンライン復帰ごとに再ログインが走ってユーザービリティが下がる可能性がある

    private bool _IsLoggedIn;
    public bool IsLoggedIn
    {
        get => _IsLoggedIn;
        private set => SetProperty(ref _IsLoggedIn, value);
    }

    /// <summary>
    /// ユーザーID
    /// </summary>
    private UserId _UserId;
    public UserId UserId
    {
        get => _UserId;
        private set => SetProperty(ref _UserId, value);
    }

    private string _UserName;
    public string UserName
    {
        get => _UserName;
        private set => SetProperty(ref _UserName, value);
    }

    private string _UserIconUrl;
    public string UserIconUrl
    {
        get => _UserIconUrl;
        private set => SetProperty(ref _UserIconUrl, value);
    }

    private bool _IsPremiumAccount;
    public bool IsPremiumAccount
    {
        get => _IsPremiumAccount;
        private set => SetProperty(ref _IsPremiumAccount, value);
    }

    private HohoemaAppServiceLevel _ServiceStatus;
    public HohoemaAppServiceLevel ServiceStatus
    {
        get => _ServiceStatus;
        private set => SetProperty(ref _ServiceStatus, value);
    }

    public NiconicoToolkit.NiconicoContext ToolkitContext { get; }

    public AsyncLock SigninLock { get; } = new AsyncLock();



    public bool IsLoginUserId(UserId id)
    {
        return UserId == id;
    }

    #region Login Manager

    public async Task<NiconicoSessionStatus> SignInWithPrimaryAccount()
    {
        // 資格情報からログインパラメータを取得
        string primaryAccount_id = null;
        string primaryAccount_Password = null;

        Tuple<string, string> account = await AccountManager.GetPrimaryAccount();
        if (account != null)
        {
            primaryAccount_id = account.Item1;
            primaryAccount_Password = account.Item2;
        }

        return string.IsNullOrWhiteSpace(primaryAccount_id) || string.IsNullOrWhiteSpace(primaryAccount_Password)
            ? NiconicoSessionStatus.Failed
            : await SignIn(primaryAccount_id, primaryAccount_Password);
    }


    public async ValueTask<bool> CanSignInWithPrimaryAccount()
    {
        string primaryAccount_id = null;
        string primaryAccount_Password = null;

        Tuple<string, string> account = await AccountManager.GetPrimaryAccount();
        if (account != null)
        {
            primaryAccount_id = account.Item1;
            primaryAccount_Password = account.Item2;
        }

        return !string.IsNullOrWhiteSpace(primaryAccount_id) && !string.IsNullOrWhiteSpace(primaryAccount_Password);
    }

    private void HandleLoginError(Exception e = null)
    {
        UpdateServiceStatus();

        LogInFailed?.Invoke(this, new NiconicoSessionLoginErrorEventArgs()
        {
            LoginFailedReason = InternetConnection.IsInternet()
            ? LoginFailedReason.ServiceNotAvailable
            : LoginFailedReason.OfflineNetwork
            ,
            Exception = e,
        });
    }

    private async Task<(string UserName, Uri IconUrl)> LoginAfterResolveUserDetailAction(NiconicoToolkit.NiconicoContext context)
    {
        try
        {
            NicovideoUserResponse user = await context.User.GetUserInfoAsync(_UserId.ToString());
            return !user.IsOK ? throw new HohoemaException() : ((string UserName, Uri IconUrl))(user.User.Nickname, user.User.ThumbnailUrl);
        }
        catch (Exception ex)
        {
            try
            {
                var res = await context.User.GetUserDetailAsync(UserId.ToString());
                Guard.IsTrue(res.IsSuccess, nameof(res.IsSuccess));
                return (res.Data.User.Nickname, res.Data.User.Icons.Small);
            }
            catch
            {
                Debug.WriteLine("ユーザーのアイコン取得処理に失敗 + " + _UserId);
                Debug.WriteLine(ex.ToString());
#if DEBUG
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
#endif
                return (string.Empty, default);
            }
        }
    }

    public async Task<NiconicoSessionStatus> SignIn(string mailOrTelephone, string password, bool withClearAuthenticationCache = false)
    {
        using (IDisposable releaser = await SigninLock.LockAsync())
        {
            if (!Helpers.InternetConnection.IsInternet())
            {
                return NiconicoSessionStatus.Failed;
            }
        }

        NetworkInformation.NetworkStatusChanged -= OnNetworkStatusChanged;
        CoreApplication.Suspending -= CoreApplication_Suspending;
        CoreApplication.Resuming -= CoreApplication_Resuming;

        NetworkInformation.NetworkStatusChanged += OnNetworkStatusChanged;
        CoreApplication.Suspending += CoreApplication_Suspending;
        CoreApplication.Resuming += CoreApplication_Resuming;


        if (IsLoggedIn)
        {
            _ = await SignOut();
        }

        using (await SigninLock.LockAsync())
        {
            Debug.WriteLine("try login");

            /*
            if (withClearAuthenticationCache)
            {
                context.ClearAuthenticationCache();
            }
            */

            try
            {
                ToolkitContext.Account.RequireTwoFactorAuth += Account_RequireTwoFactorAuth;
                ToolkitContext.Account.LoggedIn += Account_LoggedIn;

                (NiconicoSessionStatus status, NiconicoAccountAuthority authority, UserId userId) = await ToolkitContext.Account.SignInAsync(new NiconicoToolkit.Account.MailAndPasswordAuthToken(mailOrTelephone, password));

                UpdateServiceStatus(status);

                if (status is NiconicoSessionStatus.Success or NiconicoSessionStatus.RequireTwoFactorAuth)
                {

                }
                else
                {
                    LogInFailed?.Invoke(this, new NiconicoSessionLoginErrorEventArgs()
                    {
                        LoginFailedReason = status == NiconicoSessionStatus.Failed ? LoginFailedReason.InvalidMailOrPassword : LoginFailedReason.ServiceNotAvailable
                    });
                }

                return status;
            }
            catch (Exception e)
            {
                ToolkitContext.Account.RequireTwoFactorAuth -= Account_RequireTwoFactorAuth;
                ToolkitContext.Account.LoggedIn -= Account_LoggedIn;
                HandleLoginError(e);

                return NiconicoSessionStatus.Failed;
            }
        }
    }

    private void Account_LoggedIn(object sender, NiconicoLoggedInEventArgs e)
    {
        _ = _dispatcherQueue.TryEnqueue(async () =>
        {
            IsLoggedIn = true;
            IsPremiumAccount = e.IsPremiumAccount;
            UserId = e.UserId;

            try
            {
                (string UserName, Uri IconUrl) userInfo = await LoginAfterResolveUserDetailAction(ToolkitContext);
                UserName = userInfo.UserName;
                UserIconUrl = userInfo.IconUrl.OriginalString;
            }
            catch
            {
                
            }

            LogIn?.Invoke(this, new NiconicoSessionLoginEventArgs()
            {
                UserId = UserId,
                IsPremium = IsPremiumAccount,
                UserName = UserName,
                UserIconUrl = UserIconUrl,
            });
        });
    }


    private async void Account_RequireTwoFactorAuth(object sender, NiconicoTwoFactorAuthEventArgs e)
    {
        NiconicoSessionLoginRequireTwoFactorAuthResponse res = await _messenger.Send(new NiconicoSessionLoginRequireTwoFactorAsyncRequestMessage(e.Location, ToolkitContext));
        if (res != null)
        {
            NiconicoSessionStatus result = await e.MfaAsync(res.Code, res.IsTrustDevice, res.DeviceName);
            UpdateServiceStatus(result);
        }
    }


    public async Task<NiconicoSessionStatus> SignOut()
    {
        NiconicoSessionStatus result = NiconicoSessionStatus.Failed;

        using (IDisposable releaser = await SigninLock.LockAsync())
        {
            UserName = null;
            UserId = default(uint);
            UserIconUrl = null;

            IsLoggedIn = false;

            UpdateServiceStatus();
            try
            {
                if (Helpers.InternetConnection.IsInternet())
                {
                    result = await ToolkitContext.Account.SignOutAsync();
                }
            }
            finally
            {
                LogOut?.Invoke(this, EventArgs.Empty);
            }
        }

        return result;
    }

    private void UpdateServiceStatus(NiconicoSessionStatus status = NiconicoSessionStatus.Failed)
    {
        if (Helpers.InternetConnection.IsInternet())
        {
            ServiceStatus = status == NiconicoSessionStatus.Success
                ? HohoemaAppServiceLevel.LoggedIn
                : status == NiconicoSessionStatus.ServiceUnavailable
                    ? HohoemaAppServiceLevel.ServiceUnavailable
                    : HohoemaAppServiceLevel.WithoutLoggedIn;
        }
        else
        {
            ServiceStatus = HohoemaAppServiceLevel.Offline;
        }
    }

    public async Task<NiconicoSessionStatus> CheckSignedInStatus()
    {
        NiconicoSessionStatus result = NiconicoSessionStatus.Failed;

        using (IDisposable releaser = await SigninLock.LockAsync())
        {
            try
            {
                result = await ToolkitContext.Account.CheckSessionStatusAsync();
            }
            catch
            {
                // ログイン処理時には例外を捕捉するが、ログイン状態チェックでは例外は無視する
                result = NiconicoSessionStatus.Failed;
            }

            UpdateServiceStatus(result);
        }

        return result;
    }


    #endregion

}
