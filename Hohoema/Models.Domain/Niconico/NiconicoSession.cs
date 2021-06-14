using Mntone.Nico2;
using Hohoema.Models.Helpers;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking.Connectivity;
using Windows.UI.Core;
using Windows.Web.Http;
using AsyncLock = Hohoema.Models.Helpers.AsyncLock;
using Hohoema.Models.Domain.Application;
using NiconicoToolkit.Account;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using Hohoema.Models.Infrastructure;

namespace Hohoema.Models.Domain.Niconico
{
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

    public sealed class NiconicoSession : FixPrism.BindableBase, IDisposable
    {
        public NiconicoSession( 
            IScheduler scheduler,
            IMessenger messenger
            )
        {
            UpdateServiceStatus(NiconicoSessionStatus.Failed);

            NetworkInformation.NetworkStatusChanged += OnNetworkStatusChanged;

            App.Current.Suspending += Current_Suspending;
            App.Current.Resuming += Current_Resuming;

            ToolkitContext = new NiconicoToolkit.NiconicoContext(HohoemaUserAgent);
            ToolkitContext.SetupDefaultRequestHeaders();

            Scheduler = scheduler;
            _messenger = messenger;
        }

        public override void Dispose()
        {
            base.Dispose();

            _Context?.Dispose();
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
                var lastStatus = this.ServiceStatus;
                Scheduler.Schedule(async () =>
                {
                    if (lastStatus == HohoemaAppServiceLevel.LoggedIn)
                    {
                        var status = await CheckSignedInStatus();

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

        bool skipOnceNetworkStatusChange;
        private void Current_Resuming(object sender, object e)
        {
            skipOnceNetworkStatusChange = true;
        }

        private void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            skipOnceNetworkStatusChange = true;
        }

        public const string HohoemaUserAgent = "https://github.com/tor4kichi/Hohoema";
        private readonly IMessenger _messenger;


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

        bool _IsLoggedIn;
        public bool IsLoggedIn
        {
            get { return _IsLoggedIn; }
            private set { SetProperty(ref _IsLoggedIn, value); }
        }

        /// <summary>
        /// ユーザーID
        /// </summary>
        private uint _UserId;
        public uint UserId
        {
            get { return _UserId; }
            private set
            {
                if (SetProperty(ref _UserId, value))
                {
                    UserIdString = _UserId.ToString();
                    RaisePropertyChanged(nameof(UserIdString));
                }
            }

        }

        public string UserIdString { get; private set; }

        string _UserName;
        public string UserName
        {
            get { return _UserName; }
            private set { SetProperty(ref _UserName, value); }
        }

        string _UserIconUrl;
        public string UserIconUrl
        {
            get { return _UserIconUrl; }
            private set { SetProperty(ref _UserIconUrl, value); }
        }

        bool _IsPremiumAccount;
        public bool IsPremiumAccount
        {
            get { return _IsPremiumAccount; }
            private set { SetProperty(ref _IsPremiumAccount, value); }
        }

        private HohoemaAppServiceLevel _ServiceStatus;
        public HohoemaAppServiceLevel ServiceStatus
        {
            get { return _ServiceStatus; }
            private set { SetProperty(ref _ServiceStatus, value); }
        }

        private NiconicoContext _Context;
        public NiconicoContext Context
        {
            get { return _Context ??= new NiconicoContext(HohoemaUserAgent); }
            private set 
            {
                var old = _Context;
#pragma warning disable IDISP003 // Dispose previous before re-assigning.
                if (SetProperty(ref _Context, value))
#pragma warning restore IDISP003 // Dispose previous before re-assigning.
                {
                    old?.Dispose();
                }
            }
        }



        public NiconicoToolkit.NiconicoContext ToolkitContext { get; }

        public IScheduler Scheduler { get; }

        public AsyncLock SigninLock { get; } = new AsyncLock();



        public bool IsLoginUserId(string id)
        {
            return UserIdString == id;
        }

        public bool IsLoginUserId(uint id)
        {
            return UserId == id;
        }


        #region Login Manager

        public async Task<NiconicoSessionStatus> SignInWithPrimaryAccount()
        {
            // 資格情報からログインパラメータを取得
            string primaryAccount_id = null;
            string primaryAccount_Password = null;

            var account = await AccountManager.GetPrimaryAccount();
            if (account != null)
            {
                primaryAccount_id = account.Item1;
                primaryAccount_Password = account.Item2;
            }

            if (String.IsNullOrWhiteSpace(primaryAccount_id) || String.IsNullOrWhiteSpace(primaryAccount_Password))
            {
                return NiconicoSessionStatus.Failed;
            }


            return await SignIn(primaryAccount_id, primaryAccount_Password);
        }


        public async ValueTask<bool> CanSignInWithPrimaryAccount()
        {
            string primaryAccount_id = null;
            string primaryAccount_Password = null;

            var account = await AccountManager.GetPrimaryAccount();
            if (account != null)
            {
                primaryAccount_id = account.Item1;
                primaryAccount_Password = account.Item2;
            }

            if (String.IsNullOrWhiteSpace(primaryAccount_id) || String.IsNullOrWhiteSpace(primaryAccount_Password))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        void HandleLoginError(Exception e = null)
        {
            UpdateServiceStatus();

            _Context?.Dispose();
            Context = null;

            LogInFailed?.Invoke(this, new NiconicoSessionLoginErrorEventArgs()
            {
                LoginFailedReason = InternetConnection.IsInternet()
                ? LoginFailedReason.ServiceNotAvailable
                : LoginFailedReason.OfflineNetwork
                ,
                Exception = e,
            });
        }

        async Task<(string UserName, Uri IconUrl)> LoginAfterResolveUserDetailAction(NiconicoToolkit.NiconicoContext context)
        {
            try
            {
                var user = await context.User.GetUserInfoAsync(_UserId.ToString());
                if (!user.IsOK) { throw new HohoemaExpception(); }
                return (user.User.Nickname, user.User.ThumbnailUrl);
            }
            catch (Exception ex)
            {
                IsLoggedIn = false;
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

        public async Task<NiconicoSessionStatus> SignIn(string mailOrTelephone, string password, bool withClearAuthenticationCache = false)
        {
            using (var releaser = await SigninLock.LockAsync())
            {
                if (!Helpers.InternetConnection.IsInternet())
                {
                    Context?.Dispose();
                    Context = null;
                    return NiconicoSessionStatus.Failed;
                }
            }

            if (IsLoggedIn)
            {
                await SignOut();
            }

            using (_ = await SigninLock.LockAsync())
            {
                Debug.WriteLine("try login");

                var context = new NiconicoContext(HohoemaUserAgent, new NiconicoAuthenticationToken(mailOrTelephone, password));

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

                    var result = await ToolkitContext.Account.SignInAsync(new NiconicoToolkit.Account.MailAndPasswordAuthToken(mailOrTelephone, password));                    

                    UpdateServiceStatus(result.status);

                    if (result.status is NiconicoSessionStatus.Success or NiconicoSessionStatus.RequireTwoFactorAuth)
                    {
                        
                    }
                    else 
                    {
                        LogInFailed?.Invoke(this, new NiconicoSessionLoginErrorEventArgs()
                        {
                            LoginFailedReason = result.status == NiconicoSessionStatus.Failed ? LoginFailedReason.InvalidMailOrPassword : LoginFailedReason.ServiceNotAvailable
                        });
                    }

                    Context = context;

                    return result.status;
                }
                catch (Exception e)
                {
                    context.Dispose();
                    ToolkitContext.Account.RequireTwoFactorAuth -= Account_RequireTwoFactorAuth;
                    ToolkitContext.Account.LoggedIn -= Account_LoggedIn;
                    HandleLoginError(e);

                    return NiconicoSessionStatus.Failed;
                }
            }
        }

        private async void Account_LoggedIn(object sender, NiconicoLoggedInEventArgs e)
        {
            IsLoggedIn = true;
            IsPremiumAccount = e.IsPremiumAccount;
            UserId = e.UserId;

            var userInfo = await LoginAfterResolveUserDetailAction(ToolkitContext);
            UserName = userInfo.UserName;
            UserIconUrl = userInfo.IconUrl.OriginalString;

            LogIn?.Invoke(this, new NiconicoSessionLoginEventArgs()
            {
                UserId = UserId,
                IsPremium = IsPremiumAccount,
                UserName = UserName,
                UserIconUrl = UserIconUrl,
            });
        }


        private async void Account_RequireTwoFactorAuth(object sender, NiconicoTwoFactorAuthEventArgs e)
        {
            var res = await _messenger.Send(new NiconicoSessionLoginRequireTwoFactorAsyncRequestMessage(e.Location, ToolkitContext));
            if (res != null)
            {
                var result = await e.MfaAsync(res.Code, res.IsTrustDevice, res.DeviceName);
                UpdateServiceStatus(result);
            }
        }


        public async Task<NiconicoSessionStatus> SignOut()
        {
            var result = NiconicoSessionStatus.Failed;

            using (var releaser = await SigninLock.LockAsync())
            {
                UserName = null;
                UserId = default(uint);
                UserIconUrl = null;

                IsLoggedIn = false;

                UpdateServiceStatus();

                if (Context == null)
                {
                    return result;
                }

                try
                {
                    if (Helpers.InternetConnection.IsInternet())
                    {
                        result = await ToolkitContext.Account.SignOutAsync();
                    }
 
                    Context.Dispose();
                }
                finally
                {
                    Context = null;

                    LogOut?.Invoke(this, EventArgs.Empty);
                }
            }

            return result;
        }

        private void UpdateServiceStatus(NiconicoSessionStatus status = NiconicoSessionStatus.Failed)
        {
            if (Helpers.InternetConnection.IsInternet())
            {
                if (status == NiconicoSessionStatus.Success)
                {
                    ServiceStatus = HohoemaAppServiceLevel.LoggedIn;
                }
                else if (status == NiconicoSessionStatus.ServiceUnavailable)
                {
                    ServiceStatus = HohoemaAppServiceLevel.ServiceUnavailable;
                }
                else
                {
                    ServiceStatus = HohoemaAppServiceLevel.WithoutLoggedIn;
                }
            }
            else
            {
                ServiceStatus = HohoemaAppServiceLevel.Offline;
            }
        }

        public async Task<NiconicoSessionStatus> CheckSignedInStatus()
        {
            NiconicoSessionStatus result = NiconicoSessionStatus.Failed;

            using (var releaser = await SigninLock.LockAsync())
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
}
