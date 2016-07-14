using Mntone.Nico2;
using NicoPlayerHohoema.Models;
using Prism.Commands;
using Prism.Windows.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;

namespace NicoPlayerHohoema.ViewModels
{
	public class LoginPageViewModel : HohoemaViewModelBase
	{
		public LoginPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
			 : base(hohoemaApp, pageManager)
		{
			AccountSettings = HohoemaApp.CurrentAccount;

			CanChangeValue = new ReactiveProperty<bool>(true);

			MailOrTelephone = AccountSettings.ToReactivePropertyAsSynchronized(x => x.MailOrTelephone);
			Password = AccountSettings.ToReactivePropertyAsSynchronized(x => x.Password);
			
			// すでにパスワード保存済みの場合は「パスワードを保存する」をチェックした状態にする
			IsRememberPassword = new ReactiveProperty<bool>(!String.IsNullOrEmpty(Password.Value));

			// メールとパスワードが1文字以上あればログインボタンが押せる
			CheckLoginCommand = Observable.CombineLatest(
					MailOrTelephone.Select(x => x?.Length > 0),
					Password.Select(x => x?.Length > 0),
					CanChangeValue
				)
				.Select(x => x[0] && x[1] && x[2])
				.ToReactiveCommand();


			CheckLoginCommand.Subscribe(async x => await CheckLoginAndGo());

			IsLoginFailed = new ReactiveProperty<bool>();
			IsServiceUnavailable = new ReactiveProperty<bool>();
		}

		

		public override async void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			IsLoginFailed.Value = false;


			// ログイン済みの場合、ログアウトする
			if (await HohoemaApp.CheckSignedInStatus() == NiconicoSignInStatus.Success)
			{
				await HohoemaApp.SignOut();

				// ログアウト前のページに戻れないようにページ履歴を削除
				PageManager.ClearNavigateHistory();
			}

			if (e.Parameter is bool && HohoemaApp.CurrentAccount != null)
			{
				var canAutoLogin = (bool)e.Parameter;

				if (canAutoLogin && !String.IsNullOrEmpty(HohoemaApp.CurrentAccount.Password))
				{
					await CheckLoginAndGo();
				}
			}

			base.OnNavigatedTo(e, viewModelState);
		}




		private async Task CheckLoginAndGo()
		{
			CanChangeValue.Value = false;
			IsLoginFailed.Value = false;

			var result = await HohoemaApp.SignInFromUserSettings();

			if (result == NiconicoSignInStatus.Success)
			{
				// アカウント情報をアプリケーションデータとして保存
				HohoemaApp.SaveAccount(IsRememberPassword.Value);

				// ログインページにバックキーで戻れないようにページ履歴削除
				PageManager.ClearNavigateHistory();

				// ポータルページへGO
				PageManager.OpenPage(HohoemaPageType.Portal);
				IsServiceUnavailable.Value = false;
			}
			else if (result == NiconicoSignInStatus.Failed)
			{
				IsLoginFailed.Value = true;
				IsServiceUnavailable.Value = false;
			}
			else if (result == NiconicoSignInStatus.ServiceUnavailable)
			{
				IsLoginFailed.Value = false;
				IsServiceUnavailable.Value = true;
			}
			else
			{
				HohoemaApp.NiconicoContext?.Dispose();
				HohoemaApp.NiconicoContext = null;
			}

			CanChangeValue.Value = true;
		}

		public ReactiveProperty<bool> IsLoginFailed { get; private set; }
		public ReactiveProperty<bool> IsServiceUnavailable { get; private set; }

		public ReactiveProperty<bool> CanChangeValue { get; private set; }

		public ReactiveProperty<string> MailOrTelephone { get; private set; }
		public ReactiveProperty<string> Password { get; private set; }
		public ReactiveProperty<bool> IsRememberPassword { get; private set; }

		public ReactiveCommand CheckLoginCommand { get; private set; }


		public AccountSettings AccountSettings { get; private set; }
	}

	/*
	 
	 public class AccountSettingsPageContentViewModel : SettingsPageContentViewModel
	{
		public AccountSettingsPageContentViewModel(HohoemaApp hohoemaApp, string title)
			: base(title)
		{
			HohoemaApp = hohoemaApp;
			AccountSettings = hohoemaApp.UserSettings.AccontSettings;

			IsValidAccount = new ReactiveProperty<bool>(false);
			MailOrTelephone = AccountSettings.ToReactivePropertyAsSynchronized(x => x.MailOrTelephone);
			Password = AccountSettings.ToReactivePropertyAsSynchronized(x => x.Password);


			Observable.CombineLatest(
				MailOrTelephone.ToUnit(),
				Password.ToUnit()
				)
				.Subscribe(_ => IsValidAccount.Value = false);

			CheckLoginCommand = new DelegateCommand(CheckLogin);
		}



		private async void CheckLogin()
		{
			var result = await HohoemaApp.SignInFromUserSettings();

			IsValidAccount.Value = (result == NiconicoSignInStatus.Success);
		}
		
		public ReactiveProperty<bool> IsValidAccount { get; private set; }

		public ReactiveProperty<string> MailOrTelephone { get; private set; }
		public ReactiveProperty<string> Password { get; private set; }


		public DelegateCommand CheckLoginCommand { get; private set; }



		public HohoemaApp HohoemaApp { get; private set; }
		public AccountSettings AccountSettings { get; private set; }
	}
	 
	 */
}
