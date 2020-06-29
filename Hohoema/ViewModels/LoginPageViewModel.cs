using Hohoema.Models;
using Hohoema.Models.Helpers;
using Hohoema.Services;
using Hohoema.ViewModels.Pages;
using Hohoema.UseCase;
using Prism.Navigation;
using Reactive.Bindings;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Hohoema.Models.Niconico;
using Hohoema.Models.Pages.PagePayload;
using Hohoema.Models.Niconico.Account;

namespace Hohoema.ViewModels
{
    public class LoginPageViewModel : HohoemaViewModelBase, INavigatedAwareAsync
    {
        // TODO: ログインエラー時のテキスト表示

        public LoginPageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            PageManager pageManager,
            NiconicoSession niconicoSession
            )
        {
            PageManager = pageManager;
            NiconicoSession = niconicoSession;

            var version = Windows.ApplicationModel.Package.Current.Id.Version;
            VersionText = $"{version.Major}.{version.Minor}.{version.Build}";

            Mail = new ReactiveProperty<string>("", mode: ReactivePropertyMode.DistinctUntilChanged);
            Password = new ReactiveProperty<string>("", mode: ReactivePropertyMode.DistinctUntilChanged);

            IsRememberPassword = new ReactiveProperty<bool>(!string.IsNullOrEmpty(Password.Value));

            IsValidAccount = new ReactiveProperty<bool>(NiconicoSession.IsLoggedIn);
            NowProcessLoggedIn = new ReactiveProperty<bool>(false);
            IsAuthoricationFailed = new ReactiveProperty<bool>(false);
            IsServiceUnavailable = new ReactiveProperty<bool>(false);

            // メールかパスワードが変更されたらログイン検証されていないアカウントとしてマーク
            TryLoginCommand = Observable.CombineLatest(
                Mail.Select(x => !string.IsNullOrWhiteSpace(x)),
                Password.Select(x => !string.IsNullOrWhiteSpace(x)),
                NowProcessLoggedIn.Select(x => !x)
                )
                .Select(x => x.All(y => y))
                .ToReactiveCommand();

            TryLoginCommand.Subscribe(async _ =>
            {
                NowProcessLoggedIn.Value = true;

                try
                {
                    await TryLogin();
                }
                finally
                {
                    NowProcessLoggedIn.Value = false;
                }
            });
            
        }


        public string VersionText { get; private set; }

        public ReactiveProperty<string> Mail { get; private set; }
        public ReactiveProperty<string> Password { get; private set; }
        public ReactiveProperty<bool> IsRememberPassword { get; private set; }

        public ReactiveProperty<bool> IsValidAccount { get; private set; }
        public ReactiveProperty<bool> NowProcessLoggedIn { get; private set; }

        public ReactiveProperty<bool> IsAuthoricationFailed { get; private set; }
        public ReactiveProperty<bool> IsServiceUnavailable { get; private set; }

        public ReactiveCommand TryLoginCommand { get; private set; }

        public ReactiveProperty<string> LoginErrorText { get; private set; }
        public PageManager PageManager { get; }
        public NiconicoSession NiconicoSession { get; }

        private LoginRedirectPayload _RedirectInfo;

        public async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            PageManager.ClearNavigateHistory();

            /*
            if (e.Parameter is string)
            {
                _RedirectInfo = LoginRedirectPayload.FromParameterString<LoginRedirectPayload>(e.Parameter as string);
            }
            */


            var accountInfo = await AccountManager.GetPrimaryAccount();
            if (accountInfo != null)
            {
                Mail.Value = accountInfo.Item1;
                Password.Value = accountInfo.Item2;
            }
            else if (AccountManager.HasPrimaryAccount())
            {
                Mail.Value = AccountManager.GetPrimaryAccountId();
            }

            IsRememberPassword.Value = !string.IsNullOrWhiteSpace(Password.Value);
        }


        private async Task TryLogin()
        {
            // Note: NiconicoContextのインスタンスを作成してサインインを試行すると
            // HttpClientのキャッシュ削除がされていない状態で試行されてしまい
            // 正常な結果を得られません。
            // HohoemaApp上で管理しているNiconicoContextのみに限定することで
            // HttpClientのキャッシュが残る挙動に対処しています

            IsAuthoricationFailed.Value = false;
            IsServiceUnavailable.Value = false;

            var result = await NiconicoSession.SignIn(Mail.Value, Password.Value, withClearAuthenticationCache:true);
            IsValidAccount.Value = result == NiconicoSignInStatus.Success;
            IsAuthoricationFailed.Value = result == NiconicoSignInStatus.Failed;
            IsServiceUnavailable.Value = result == NiconicoSignInStatus.ServiceUnavailable;


            if (IsValidAccount.Value)
            {
                AccountManager.SetPrimaryAccountId(Mail.Value);

                if (IsRememberPassword.Value)
                {
                    await AccountManager.AddOrUpdateAccount(Mail.Value, Password.Value);
                }
                else
                {
                    AccountManager.RemoveAccount(Mail.Value);
                }

                // TODO: 初期セットアップ補助ページを開く？

                if (_RedirectInfo != null)
                {
                    try
                    {
                        PageManager.OpenPage(_RedirectInfo.RedirectPageType, _RedirectInfo.RedirectParamter);
                    }
                    catch
                    {
                        PageManager.OpenStartupPage();
                    }
                }
                else
                {
                    PageManager.OpenStartupPage();
                }
            }
        }
    }
}
