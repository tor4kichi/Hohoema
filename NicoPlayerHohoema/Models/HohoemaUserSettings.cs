using NicoPlayerHohoema.Mntone.Nico2.Ranking;
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

		public RankingSettings RankingSettings { get; private set; }

		public PlayerSettings PlayerSettings { get; private set; }

		public HohoemaUserSettings()
		{
			LoadSettings();
		}

		public void LoadSettings()
		{
			var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

			Windows.Storage.ApplicationData.Current.SignalDataChanged();
			var accountDataContainer = localSettings.CreateContainer(nameof(AccountSettings), ApplicationDataCreateDisposition.Always);

			// 複数アカウント対応のための拡張性をもたせている
			var firstAccount = accountDataContainer.CreateContainer("00", ApplicationDataCreateDisposition.Always);
			AccontSettings = new AccountSettings(firstAccount);

			// ランキングの表示設定
			var rankingDataContainer = localSettings.CreateContainer(nameof(RankingSettings), ApplicationDataCreateDisposition.Always);
			RankingSettings = new RankingSettings(rankingDataContainer);

			// 動画プレイヤーの設定
			var playerDataContainer = localSettings.CreateContainer(nameof(PlayerSettings), ApplicationDataCreateDisposition.Always);
			PlayerSettings = new PlayerSettings(playerDataContainer);
		}
	}


	public abstract class SettingsBase : BindableBase
	{


		public SettingsBase(ApplicationDataContainer dataContainer)
		{
			_DataContainer = dataContainer;
		}

		protected T GetValue<T>(string valueName)
		{
			if (!HasValue(valueName))
			{
				SetValue(valueName, default(T));
			}
			return (T)_DataContainer.Values[valueName];
		}

		protected void SetValue<T>(string valueName, T value)
		{
			_DataContainer.Values[valueName] = value;
		}

		protected bool HasValue(string valueName)
		{
			return _DataContainer.Values.ContainsKey(valueName);
		}


		ApplicationDataContainer _DataContainer;
	}

	public class RankingSettings : SettingsBase
	{
		public RankingSettings(ApplicationDataContainer rankingDataContainer)
			: base(rankingDataContainer)
		{
		}

		private RankingTarget _RankingTarget;
		public RankingTarget RankingTarget
		{
			get
			{
				return _RankingTarget = GetValue<RankingTarget>(nameof(RankingTarget));
			}
			set
			{
				if (SetProperty(ref _RankingTarget, value))
				{
					SetValue(nameof(RankingTarget), value);
				}
			}
		}


		private RankingTimeSpan _RankingTimeSpan;
		public RankingTimeSpan RankingTimeSpan
		{
			get
			{
				return _RankingTimeSpan = GetValue<RankingTimeSpan>(nameof(RankingTimeSpan));
			}
			set
			{
				if (SetProperty(ref _RankingTimeSpan, value))
				{
					SetValue(nameof(RankingTimeSpan), value);
				}
			}
		}

		






		private RankingCategory _RankingCategory;
		public RankingCategory RankingCategory
		{
			get
			{
				return _RankingCategory = GetValue<RankingCategory>(nameof(RankingCategory));
			}
			set
			{
				if (SetProperty(ref _RankingCategory, value))
				{
					SetValue(nameof(RankingCategory), value);
				}
			}
		}

		
	}

	public class AccountSettings : SettingsBase
	{
		public AccountSettings(ApplicationDataContainer accountDataContainer)
			: base(accountDataContainer)
		{
			_MailOrTelephone = GetValue<string>(nameof(MailOrTelephone));
			_Password = GetValue<string>(nameof(Password));
		}



		private string _MailOrTelephone;
		public string MailOrTelephone
		{
			get
			{
				return _MailOrTelephone;
			}
			set
			{
				if (SetProperty(ref _MailOrTelephone, value))
				{
					SetValue(nameof(MailOrTelephone), value);
				}
			}
		}


		public bool IsValidMailOreTelephone
		{
			get
			{
				return !String.IsNullOrWhiteSpace(MailOrTelephone);
			}
		}

		

		private string _Password;
		public string Password
		{
			get
			{
				return _Password;
			}
			set
			{
				if (SetProperty(ref _Password, value))
				{
					SetValue(nameof(Password), value);
				}
			}
		}


		public bool IsValidPassword
		{
			get
			{
				return !String.IsNullOrWhiteSpace(Password);
			}
		}
		

		
	}



	public class PlayerSettings : SettingsBase
	{
		public PlayerSettings(ApplicationDataContainer dataContainer)
			: base(dataContainer)
		{
		}




		private PlayerDisplayMode _DisplayMode;
		public PlayerDisplayMode DisplayMode
		{
			get
			{
				return _DisplayMode = GetValue<PlayerDisplayMode>(nameof(DisplayMode));
			}
			set
			{
				if (SetProperty(ref _DisplayMode, value))
				{
					SetValue(nameof(DisplayMode), value);
				}
			}
		}
	}
}
