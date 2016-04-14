using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using Prism.Mvvm;
using Reactive.Bindings;
using NicoPlayerHohoema.Models;
using Reactive.Bindings.Extensions;
using Prism.Commands;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using Mntone.Nico2;

namespace NicoPlayerHohoema.ViewModels
{
	public class SettingsPageViewModel : ViewModelBase
	{
		public SettingsPageViewModel(HohoemaApp hohoemaApp)
		{
			HohoemaApp = hohoemaApp;

			AccountSeetingsContentVM = new AccountSettingsPageContentViewModel(HohoemaApp);

			CurrentSettingsContent = new ReactiveProperty<SettingsPageContentViewModel>(AccountSeetingsContentVM);

			SettingItems = new List<SettingsPageContentViewModel>()
			{
				AccountSeetingsContentVM
			};
		}





		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			base.OnNavigatedTo(e, viewModelState);
		}

		public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			base.OnNavigatingFrom(e, viewModelState, suspending);
		}




		public HohoemaApp HohoemaApp { get; private set; }

		public ReactiveProperty<SettingsPageContentViewModel> CurrentSettingsContent { get; private set; }

		public AccountSettingsPageContentViewModel AccountSeetingsContentVM { get; private set; }

		public List<SettingsPageContentViewModel> SettingItems { get; private set; }
	}


	public abstract class SettingsPageContentViewModel : ViewModelBase
	{
		public string Title { get; private set; }

		public SettingsPageContentViewModel(string title)
		{
			Title = title;
		}
	}

	public class AccountSettingsPageContentViewModel : SettingsPageContentViewModel
	{
		public AccountSettingsPageContentViewModel(HohoemaApp hohoemaApp)
			: base(title:"アカウント")
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
}
