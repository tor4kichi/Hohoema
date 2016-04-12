using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace NicoPlayerHohoema.Models
{
	public class HohoemaUserSettings
	{
		public AccountSettings AccontSettings { get; private set; }



		public HohoemaUserSettings()
		{
			LoadSettings();
		}

		public void LoadSettings()
		{
			var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

			var accountDataContainer = localSettings.CreateContainer(nameof(AccountSettings), ApplicationDataCreateDisposition.Always);

			// 複数アカウント対応のための拡張性をもたせている
			var firstAccount = accountDataContainer.CreateContainer("00", ApplicationDataCreateDisposition.Always);
			AccontSettings = new AccountSettings(firstAccount);
		}
	}

	public class AccountSettings : BindableBase
	{
		public ApplicationDataContainer AccountDataContainer { get; private set; }


		private string _MailOrTelephone;
		public string MailOrTelephone
		{
			get
			{
				if (_MailOrTelephone == null)
				{
					if (HasMailOrTelephone)
					{
						_MailOrTelephone = (string)AccountDataContainer.Values[nameof(MailOrTelephone)];
					}
					else
					{
						_MailOrTelephone = "";
					}
				}
				return _MailOrTelephone;
			}
			set
			{
				if (SetProperty(ref _MailOrTelephone, value))
				{
					AccountDataContainer.Values[nameof(MailOrTelephone)] = value;
				}
			}
		}

		public bool HasMailOrTelephone
		{
			get
			{
				return AccountDataContainer.Values.ContainsKey(nameof(MailOrTelephone));
			}
		}

		private string _Password;
		public string Password
		{
			get
			{
				if (_Password == null)
				{
					if (HasPassword)
					{
						_Password = (string)AccountDataContainer.Values[nameof(Password)];
					}
					else
					{
						_Password = "";
					}
				}
				return _Password;
			}
			set
			{
				if (SetProperty(ref _Password, value))
				{
					AccountDataContainer.Values[nameof(Password)] = value;
				}
			}
		}

		public bool HasPassword
		{
			get
			{
				return AccountDataContainer.Values.ContainsKey(nameof(Password));
			}
		}

		public AccountSettings(ApplicationDataContainer accountDataContainer)
		{
			AccountDataContainer = accountDataContainer;
		}
	}
}
