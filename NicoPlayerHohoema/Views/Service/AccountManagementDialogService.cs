using NicoPlayerHohoema.Models;
using Prism.Commands;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Views.Service
{
    public class AccountManagementDialogService
    {
        public HohoemaApp HohoemaApp { get; private set; }
        public AccountManagementDialogService(HohoemaApp hohoemaApp)
        {
            HohoemaApp = hohoemaApp;
        }

        public async Task<bool> ShowChangeAccountDialogAsync()
        {
            var context = new AccountManagementDialogContext(HohoemaApp);

            var dialog = new AccountManagementdDialog()
            {
                DataContext = context
            };

            var result = await dialog.ShowAsync();

            return context.IsValidAccount.Value;
        }
    }

    internal class AccountManagementDialogContext
    {

        public HohoemaApp HohoemaApp { get; private set; }
        public ReactiveProperty<string> Mail { get; private set; }
        public ReactiveProperty<string> Password { get; private set; }

        public ReactiveProperty<bool> IsValidAccount { get; private set; }
        public ReactiveProperty<bool> NowProcessLoggedIn { get; private set; }

        public ReactiveProperty<bool> IsAuthoricationFailed { get; private set; }
        public ReactiveProperty<bool> IsServiceUnavailable { get; private set; }

        public ReactiveCommand ValidateCommand { get; private set; }
        public ReactiveCommand ApplyCommand { get; private set; }
        public ReactiveCommand CancelCommand { get; private set; }

        public AccountManagementDialogContext(HohoemaApp hohoemaApp)
        {
            HohoemaApp = hohoemaApp;
            var accountInfo = AccountManager.GetPrimaryAccount();
            Mail = new ReactiveProperty<string>(accountInfo?.Item1, mode:ReactivePropertyMode.DistinctUntilChanged);
            Password = new ReactiveProperty<string>(accountInfo?.Item2, mode: ReactivePropertyMode.DistinctUntilChanged);

            IsValidAccount = new ReactiveProperty<bool>(hohoemaApp.IsLoggedIn);
            NowProcessLoggedIn = new ReactiveProperty<bool>(false);
            IsAuthoricationFailed = new ReactiveProperty<bool>(false);
            IsServiceUnavailable = new ReactiveProperty<bool>(false);

            // メールかパスワードが変更されたらログイン検証されていないアカウントとしてマーク
            Observable.Merge(
                Mail.ToUnit(),
                Password.ToUnit()
                )
                .Subscribe(x => IsValidAccount.Value = false);

            // アカウントが検証されてない && 処理中ではない場合
            // ValidateCommandが有効になる
            ValidateCommand = 
                Observable.CombineLatest(
                    IsValidAccount.Select(x => !x),
                    NowProcessLoggedIn.Select(x => !x)
                    )
                    .Select(x => x.All(y => y))
                .ToReactiveCommand();

            ValidateCommand.Subscribe(async _ => 
            {
                NowProcessLoggedIn.Value = true;

                try
                {
                    await ValidateAccount();
                }
                finally
                {
                    NowProcessLoggedIn.Value = false;
                }
            });

            ApplyCommand = IsValidAccount
                .ToReactiveCommand();
            ApplyCommand.Subscribe(async _ => 
            {
                AccountManager.SetPrimaryAccountId(Mail.Value);
                AccountManager.AddOrUpdateAccount(Mail.Value, Password.Value);

                await HohoemaApp.SignInWithPrimaryAccount();
            });

            CancelCommand = NowProcessLoggedIn.Select(x => !x)
                .ToReactiveCommand();
            CancelCommand.Subscribe(async _ =>
            {
                // ログイン状態をダイアログを開いた前の状態に復帰
                await HohoemaApp.SignInWithPrimaryAccount();
            });
        }

        private async Task ValidateAccount()
        {
            // Note: NiconicoContextのインスタンスを作成してサインインを試行すると
            // HttpClientのキャッシュ削除がされていない状態で試行されてしまい
            // 正常な結果を得られません。
            // HohoemaApp上で管理しているNiconicoContextのみに限定することで
            // HttpClientのキャッシュが残る挙動に対処しています

            IsAuthoricationFailed.Value = false;
            IsServiceUnavailable.Value = false;

            var result = await HohoemaApp.SignIn(Mail.Value, Password.Value);
            IsValidAccount.Value = result == Mntone.Nico2.NiconicoSignInStatus.Success;
            IsAuthoricationFailed.Value = result == Mntone.Nico2.NiconicoSignInStatus.Failed;
            IsServiceUnavailable.Value = result == Mntone.Nico2.NiconicoSignInStatus.ServiceUnavailable;
        }



    }
}
