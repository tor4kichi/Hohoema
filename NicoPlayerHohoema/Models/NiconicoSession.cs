using Mntone.Nico2;
using NicoPlayerHohoema.Models.Helpers;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking.Connectivity;
using Windows.UI.Core;

namespace NicoPlayerHohoema.Models
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

    public struct NiconicoSessionLoginRequireTwoFactorAuthEventArgs
    {
        public Deferral Deferral { get; set; }

        public Uri TwoFactorAuthPageUri { get; set; }
    }

    public struct NiconicoSessionLoginErrorEventArgs
    {
        public LoginFailedReason LoginFailedReason { get; set; }
        public Exception Exception { get; set; }
    }

    public sealed class NiconicoSession : BindableBase
    {
        public const string HohoemaUserAgent = "Hohoema_UWP";


        // Events 
        // Login UserId, UserName, isPremium
        // Logout 
        // LoginError exception, またはエラー理由のテキスト

        public event EventHandler<NiconicoSessionLoginEventArgs> LogIn;
        public event EventHandler<NiconicoSessionLoginRequireTwoFactorAuthEventArgs> RequireTwoFactorAuth;
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
        private uint? _UserId;
        public uint? UserId
        {
            get { return _UserId; }
            private set
            {
                if (SetProperty(ref _UserId, value))
                {
                    UserIdString = _UserId?.ToString();
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
            get { return _Context ?? (_Context = new NiconicoContext()); }
            private set { _Context = value; }
        }


        private AsyncLock _SigninLock = new AsyncLock();


        public NiconicoSession()
        {
            NetworkInformation.NetworkStatusChanged += (args) => 
            {
                _ = CheckSignedInStatus();
            };
        }


        public bool IsLoginUserId(string id)
        {
            return UserIdString == id;
        }

        public bool IsLoginUserId(uint id)
        {
            return UserId == id;
        }


        #region Login Manager

        public async Task<NiconicoSignInStatus> SignInWithPrimaryAccount()
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
                return NiconicoSignInStatus.Failed;
            }


            return await SignIn(primaryAccount_id, primaryAccount_Password);
        }


        public async Task<bool> CanSignInWithPrimaryAccount()
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


        public async Task Relogin()
        {
            if (Context != null)
            {
                var context = new NiconicoContext(Context.AuthenticationToken);

                if (await context.SignInAsync() == NiconicoSignInStatus.Success)
                {
                    Context = context;
                }
            }
        }


        void HandleLoginError(Exception e = null)
        {
            UpdateServiceStatus();

            Context?.Dispose();
            Context = new NiconicoContext();

            LogInFailed?.Invoke(this, new NiconicoSessionLoginErrorEventArgs()
            {
                LoginFailedReason = InternetConnection.IsInternet()
                ? LoginFailedReason.ServiceNotAvailable
                : LoginFailedReason.OfflineNetwork
                ,
                Exception = e,
            });

#if DEBUG
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
#endif
        }

        async Task LoginAfterResolveUserDetailAction(NiconicoContext context)
        {
            Context = context;

            Mntone.Nico2.Users.Info.InfoResponse userInfo = null;
            try
            {
                await Task.Delay(500);

                userInfo = await Context.User.GetInfoAsync();
                if (userInfo == null)
                {
                    throw new Exception("ログインに失敗");
                }
            }
            catch (Exception e)
            {
                HandleLoginError(e);
                return;
            }

            UserId = userInfo.Id;
            IsPremiumAccount = userInfo.IsPremium;

            try
            {
                var user = await Context.User.GetUserDetail(_UserId.ToString());
                UserName = user?.Nickname ?? _UserId.ToString();
                UserIconUrl = user?.ThumbnailUri;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ユーザー名取得処理に失敗 + " + _UserId);
                Debug.WriteLine(ex.ToString());
#if DEBUG
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
#endif
            }

            LogIn?.Invoke(this, new NiconicoSessionLoginEventArgs()
            {
                UserId = UserId.Value,
                IsPremium = userInfo.IsPremium,
                UserName = UserName,
                UserIconUrl = UserIconUrl,
            });

            Debug.WriteLine("Login Done! " + ServiceStatus);
        }

        public IAsyncOperation<NiconicoSignInStatus> SignIn(string mailOrTelephone, string password, bool withClearAuthenticationCache = false)
        {
            return AsyncInfo.Run<NiconicoSignInStatus>(async (cancelToken) =>
            {
                using (var releaser = await _SigninLock.LockAsync())
                {
                    if (!Helpers.InternetConnection.IsInternet())
                    {
                        Context?.Dispose();
                        Context = new NiconicoContext();
                        return NiconicoSignInStatus.Failed;
                    }

                    if (Context != null
                        && Context.AuthenticationToken?.MailOrTelephone == mailOrTelephone
                        && Context.AuthenticationToken?.Password == password)
                    {
                        return NiconicoSignInStatus.Success;
                    }
                }

                cancelToken.ThrowIfCancellationRequested();

                if (IsLoggedIn)
                {
                    await SignOut();
                }

                cancelToken.ThrowIfCancellationRequested();


                using (_ = await _SigninLock.LockAsync())
                {
                    Debug.WriteLine("try login");

                    var context = new NiconicoContext(new NiconicoAuthenticationToken(mailOrTelephone, password));

                    context.AdditionalUserAgent = HohoemaUserAgent;

                    if (withClearAuthenticationCache)
                    {
                        context.ClearAuthenticationCache();
                    }


                    NiconicoSignInStatus result = NiconicoSignInStatus.Failed;
                    if ((result = await context.GetIsSignedInAsync()) == NiconicoSignInStatus.ServiceUnavailable)
                    {
                        return result;
                    }
                    try
                    {
                        result = await context.SignInAsync();

                        UpdateServiceStatus(result);

                        if (result == NiconicoSignInStatus.TwoFactorAuthRequired)
                        {
                            var deferral = new Deferral(async () =>
                            {
                                using (_ = await _SigninLock.LockAsync())
                                {
                                    try
                                    {
                                        result = await context.GetIsSignedInAsync();

                                        UpdateServiceStatus(result);

                                        if (context != null)
                                        {
                                            await LoginAfterResolveUserDetailAction(context);
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        HandleLoginError(e);
                                    }
                                }
                            });

                            RequireTwoFactorAuth.Invoke(this, new NiconicoSessionLoginRequireTwoFactorAuthEventArgs()
                            {
                                Deferral = deferral,
                                TwoFactorAuthPageUri = context.LastRedirectHttpRequestMessage.RequestUri
                            });
                        }
                        else if (result == NiconicoSignInStatus.Success)
                        {
                            await LoginAfterResolveUserDetailAction(context);
                        }
                        else 
                        {
                            LogInFailed?.Invoke(this, new NiconicoSessionLoginErrorEventArgs()
                            {
                                LoginFailedReason = LoginFailedReason.InvalidMailOrPassword
                            });
                        }

                        
                    }
                    catch (Exception e)
                    {
                        HandleLoginError(e);
                    }

                    return result;
                }
            });
        }


        public async Task<NiconicoSignInStatus> SignOut()
        {
            NiconicoSignInStatus result = NiconicoSignInStatus.Failed;

            using (var releaser = await _SigninLock.LockAsync())
            {
                UserId = null;
                UserName = null;
                UserIconUrl = null;

                UpdateServiceStatus();

                if (Context == null)
                {
                    return result;
                }

                try
                {
                    if (Helpers.InternetConnection.IsInternet())
                    {
                        result = await Context.SignOutOffAsync();
                    }
                    else
                    {
                        result = NiconicoSignInStatus.Success;
                    }

                    Context.Dispose();
                }
                finally
                {
                    Context = new NiconicoContext();

                    _UserId = null;

                }
            }

            LogOut?.Invoke(this, EventArgs.Empty);

            return result;
        }

        private void UpdateServiceStatus(NiconicoSignInStatus status = NiconicoSignInStatus.Failed)
        {
            if (Helpers.InternetConnection.IsInternet())
            {
                if (status == NiconicoSignInStatus.Success)
                {
                    ServiceStatus = HohoemaAppServiceLevel.LoggedIn;
                }
                else if (status == NiconicoSignInStatus.ServiceUnavailable)
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

        public async Task<NiconicoSignInStatus> CheckSignedInStatus()
        {
            NiconicoSignInStatus result = NiconicoSignInStatus.Failed;

            using (var releaser = await _SigninLock.LockAsync())
            {
                try
                {
                    result = await Context.GetIsSignedInAsync();
                }
                catch
                {
                    // ログイン処理時には例外を捕捉するが、ログイン状態チェックでは例外は無視する
                    result = NiconicoSignInStatus.Failed;
                }
            }

            UpdateServiceStatus(result);

            return result;
        }


        #endregion

    }
}
