using Mntone.Nico2.Videos.Ranking;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Newtonsoft.Json;
using System.IO;

namespace NicoPlayerHohoema.Models
{
	public class HohoemaUserSettings
	{
		const string AccountSettingsFileName = "account.json";
		const string RankingSettingsFileName = "ranking.json";
		const string PlayerSettingsFileName = "player.json";


		public static async Task<HohoemaUserSettings> LoadSettings()
		{
			var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;


			var account = await SettingsBase.Load<AccountSettings>(AccountSettingsFileName);
			var ranking = await SettingsBase.Load<RankingSettings>(RankingSettingsFileName);
			var player = await SettingsBase.Load<PlayerSettings>(PlayerSettingsFileName);

			return new HohoemaUserSettings()
			{
				AccontSettings = account,
				RankingSettings = ranking,
				PlayerSettings = player
			};
		}

		public static async Task<string> GetText(IStorageFile file)
		{
			using (var stream = await file.OpenAsync(FileAccessMode.Read))
			{
				using (var reader = new StreamReader(stream.AsStream(), Encoding.UTF8)) 
				{
					return reader.ReadToEnd();
				}
			}
		}

		public static async Task SaveText(IStorageFile file, string text)
		{
			using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
			{
				using (var reader = new StreamWriter(stream.AsStream(), Encoding.UTF8))
				{
					await reader.WriteAsync(text);
				}
			}
		}

		public async Task Save()
		{
			var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
			var local = await localFolder.CreateFileAsync("settings.json", CreationCollisionOption.OpenIfExists);

			var serializedText = JsonConvert.SerializeObject(this);

			await SaveText(local, serializedText);
		}

		public AccountSettings AccontSettings { get; private set; }

		public RankingSettings RankingSettings { get; private set; }

		public PlayerSettings PlayerSettings { get; private set; }

		public HohoemaUserSettings()
		{
		}
	}

	[DataContract]
	public abstract class SettingsBase : BindableBase
	{
		public string FileName { get; private set; }

		public static async Task<T> Load<T>(string filename)
			where T : SettingsBase, new()
		{
			var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
			var local = await localFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);

			var rawText = await HohoemaUserSettings.GetText(local);
			if (!String.IsNullOrEmpty(rawText))
			{
				try
				{
					var obj = JsonConvert.DeserializeObject<T>(rawText);
					obj.FileName = filename;
					return obj;
				}
				catch
				{
					await local.DeleteAsync();
				}
			}

			return new T()
			{
				FileName = filename
			};
		}


		public async Task Save()
		{
			var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
			var local = await localFolder.CreateFileAsync(FileName, CreationCollisionOption.OpenIfExists);

			var serializedText = JsonConvert.SerializeObject(this);

			await HohoemaUserSettings.SaveText(local, serializedText);
		}
	}


	[DataContract]
	public class RankingSettings : SettingsBase
	{
		public RankingSettings()
			: base()
		{
			
		}





		


		private RankingTarget _Target;

		[DataMember]
		public RankingTarget Target
		{
			get
			{
				return _Target;
			}
			set
			{
				SetProperty(ref _Target, value);
			}
		}


		private RankingTimeSpan _TimeSpan;

		[DataMember]
		public RankingTimeSpan TimeSpan
		{
			get
			{
				return _TimeSpan;
			}
			set
			{
				SetProperty(ref _TimeSpan, value);
			}
		}




		private RankingCategory _Category;

		[DataMember]
		public RankingCategory Category
		{
			get
			{
				return _Category;
			}
			set
			{
				SetProperty(ref _Category, value);
			}
		}

		[DataMember]
		public ObservableCollection<RankingCategory> HighPriorityCategory { get; private set; }

		[DataMember]
		public ObservableCollection<RankingCategory> MiddlePriorityCategory { get; private set; }

		[DataMember]
		public ObservableCollection<RankingCategory> LowPriorityCategory { get; private set; }
		
	}

	[DataContract]
	public class AccountSettings : SettingsBase
	{
		public AccountSettings()
			: base()
		{
			MailOrTelephone = "";
			Password = "";
		}



		private string _MailOrTelephone;

		[DataMember]
		public string MailOrTelephone
		{
			get
			{
				return _MailOrTelephone;
			}
			set
			{
				SetProperty(ref _MailOrTelephone, value);
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

		[DataMember]
		public string Password
		{
			get
			{
				return _Password;
			}
			set
			{
				SetProperty(ref _Password, value);
			}
		}


		public bool IsValidPassword
		{
			get
			{
				return !String.IsNullOrWhiteSpace(Password);
			}
		}


		private bool _AutoLoginEnable;

		[DataMember]
		public bool AutoLoginEnable
		{
			get
			{
				return _AutoLoginEnable;
			}
			set
			{
				SetProperty(ref _AutoLoginEnable, value);
			}
		}

	}


	[DataContract]
	public class PlayerSettings : SettingsBase
	{
		public PlayerSettings()
			: base()
		{
		}




		private PlayerDisplayMode _DisplayMode;

		[DataMember]
		public PlayerDisplayMode DisplayMode
		{
			get
			{
				return _DisplayMode;
			}
			set
			{
				SetProperty(ref _DisplayMode, value);
			}
		}
	}
}
