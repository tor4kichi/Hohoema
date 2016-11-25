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
        public ReactiveProperty<bool> ServiceUnavailable { get; private set; }

        public ReactiveCommand ValidateCommand { get; private set; }
        public ReactiveCommand ApplyCommand { get; private set; }
        public ReactiveCommand CancelCommand { get; private set; }

        public AccountManagementDialogContext(HohoemaApp hohoemaApp)
        {
            HohoemaApp = hohoemaApp;
            var accountInfo = HohoemaApp.GetPrimaryAccount();
            Mail = new ReactiveProperty<string>(accountInfo?.Item1, mode:ReactivePropertyMode.DistinctUntilChanged);
            Password = new ReactiveProperty<string>(accountInfo?.Item2, mode: ReactivePropertyMode.DistinctUntilChanged);

            IsValidAccount = new ReactiveProperty<bool>(hohoemaApp.IsLoggedIn);
            NowProcessLoggedIn = new ReactiveProperty<bool>(false);
            ServiceUnavailable = new ReactiveProperty<bool>(!Util.InternetConnection.IsInternet());


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
                    var mail = Mail.Value;
                    var password = Password.Value;
                    var signinResult = await HohoemaApp.SignIn(mail, password);

                    await Task.Delay(2000);

                    IsValidAccount.Value = signinResult == Mntone.Nico2.NiconicoSignInStatus.Success;
                    ServiceUnavailable.Value = signinResult == Mntone.Nico2.NiconicoSignInStatus.ServiceUnavailable;
                }
                finally
                {
                    NowProcessLoggedIn.Value = false;
                }
            });

            ApplyCommand = NowProcessLoggedIn.Select(x => !x)
                .ToReactiveCommand();
            ApplyCommand.Subscribe(async _ => 
            {
                HohoemaApp.SetPrimaryAccountId(Mail.Value);
                HohoemaApp.AddOrUpdateAccount(Mail.Value, Password.Value);

                var signinResult = await HohoemaApp.SignInWithPrimaryAccount();
                IsValidAccount.Value = signinResult == Mntone.Nico2.NiconicoSignInStatus.Success;
            });

            CancelCommand = NowProcessLoggedIn.Select(x => !x)
                .ToReactiveCommand();
            CancelCommand.Subscribe(async _ =>
            {
                // ログイン状態をダイアログを開いた前の状態に復帰
                var signinResult = await HohoemaApp.SignInWithPrimaryAccount();
                IsValidAccount.Value = signinResult == Mntone.Nico2.NiconicoSignInStatus.Success;
            });
        }

        


        
    }
}
