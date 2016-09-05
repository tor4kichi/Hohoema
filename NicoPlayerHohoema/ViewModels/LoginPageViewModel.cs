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
using Windows.ApplicationModel;
using Windows.Foundation.Diagnostics;
using System.Threading;
using Windows.Storage;

namespace NicoPlayerHohoema.ViewModels
{
	public class LoginPageViewModel : HohoemaViewModelBase
	{
		public LoginPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
			 : base(hohoemaApp, pageManager, false)
		{



			CanChangeValue = new ReactiveProperty<bool>(true)
				.AddTo(_CompositeDisposable);

			var account = HohoemaApp.GetPrimaryAccount();

			MailOrTelephone = new ReactiveProperty<string>(account?.Item1 ?? "")
				.AddTo(_CompositeDisposable);
			Password = new ReactiveProperty<string>(account?.Item2 ?? "")
				.AddTo(_CompositeDisposable);

			// すでにパスワード保存済みの場合は「パスワードを保存する」をチェックした状態にする
			IsRememberPassword = new ReactiveProperty<bool>(!String.IsNullOrEmpty(Password.Value))
				.AddTo(_CompositeDisposable);

			// メールとパスワードが1文字以上あればログインボタンが押せる
			CheckLoginCommand = Observable.CombineLatest(
					MailOrTelephone.Select(x => x?.Length > 0),
					Password.Select(x => x?.Length > 0),
					CanChangeValue
				)
				.Select(x => x[0] && x[1] && x[2])
				.ToReactiveCommand()
				.AddTo(_CompositeDisposable);


			CheckLoginCommand.Subscribe(async x => await CheckLoginAndGo())
				.AddTo(_CompositeDisposable);

			IsLoginFailed = new ReactiveProperty<bool>()
				.AddTo(_CompositeDisposable);
			IsServiceUnavailable = new ReactiveProperty<bool>()
				.AddTo(_CompositeDisposable);


			var version = Package.Current.Id.Version;
			VersionText = $"{version.Major}.{version.Minor}.{version.Build}";


			LoginErrorText = new ReactiveProperty<string>();
			
		}

		protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			// Note: ログインページだけはbase.OnNavigatedTo が不要
			//			base.OnNavigatedTo(e, viewModelState);

			IsLoginFailed.Value = false;

			// ログイン済みの場合、ログアウトする
			if (await HohoemaApp.CheckSignedInStatus() == NiconicoSignInStatus.Success)
			{
				await HohoemaApp.SignOut();

				// ログアウト前のページに戻れないようにページ履歴を削除
				PageManager.ClearNavigateHistory();
			}

			if (e.Parameter is bool)
			{
				var canAutoLogin = (bool)e.Parameter;

				var account = HohoemaApp.GetPrimaryAccount();
				if (canAutoLogin && account != null)
				{
					await CheckLoginAndGo();
				}
			}

			//			return base.NavigatedToAsync(cancelToken, e, viewModelState);
		}


		public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			base.OnNavigatingFrom(e, viewModelState, suspending);
		}


		private async Task CheckLoginAndGo()
		{
			CanChangeValue.Value = false;
			IsLoginFailed.Value = false;
			LoginErrorText.Value = null;

			if (!Util.InternetConnection.IsInternet())
			{
				IsLoginFailed.Value = true;
				LoginErrorText.Value = "インターネット接続が必要です";
				CanChangeValue.Value = true;
				return;
			}

			var cancelSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));


			NiconicoSignInStatus signinResult = NiconicoSignInStatus.Failed;
			try
			{
				signinResult = await HohoemaApp.SignIn(MailOrTelephone.Value, Password.Value).AsTask(cancelSource.Token);
			}
			catch (TaskCanceledException)
			{
				IsLoginFailed.Value = true;
				LoginErrorText.Value = "ログインに時間が掛かっています。もう一度お試しください。";
			}
			finally
			{
				cancelSource.Dispose();
			}

			if (signinResult == NiconicoSignInStatus.Success)
			{
				if (IsRememberPassword.Value)
				{
					HohoemaApp.AddOrUpdateAccount(MailOrTelephone.Value, Password.Value);
				}

				HohoemaApp.SetPrimaryAccountId(MailOrTelephone.Value);

				// ログインページにバックキーで戻れないようにページ履歴削除
				PageManager.ClearNavigateHistory();

				// ポータルページへGO
				PageManager.OpenPage(HohoemaPageType.Portal);
				IsServiceUnavailable.Value = false;
			}
			else if (signinResult == NiconicoSignInStatus.Failed)
			{
				IsLoginFailed.Value = true;
				IsServiceUnavailable.Value = false;
				LoginErrorText.Value = HohoemaApp.LoginErrorText;
			}
			else if (signinResult == NiconicoSignInStatus.ServiceUnavailable)
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


		public ReactiveProperty<string> LoginErrorText { get; private set; }

		public string VersionText { get; private set; }
	}
}
