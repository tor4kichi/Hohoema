using Mntone.Nico2.Videos.Ranking;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Windows.Storage;
using Newtonsoft.Json;
using System.IO;
using NicoPlayerHohoema.Util;
using Mntone.Nico2.Videos.Thumbnail;

namespace NicoPlayerHohoema.Models
{
	public class HohoemaUserSettings
	{
		const string RankingSettingsFileName = "ranking.json";
		const string PlayerSettingsFileName = "player.json";
		const string NGSettingsFileName = "ng.json";
		const string SearchSettingsFileName = "search.json";


		public static async Task<HohoemaUserSettings> LoadSettings(StorageFolder userFolder)
		{
			var ranking = await SettingsBase.Load<RankingSettings>(RankingSettingsFileName, userFolder);
			var player = await SettingsBase.Load<PlayerSettings>(PlayerSettingsFileName, userFolder);
			var ng = await SettingsBase.Load<NGSettings>(NGSettingsFileName, userFolder);
			var search = await SettingsBase.Load<SearchSeetings>(SearchSettingsFileName, userFolder);

			return new HohoemaUserSettings()
			{
				RankingSettings = ranking,
				PlayerSettings = player,
				NGSettings = ng,
				SearchSettings = search
			};
		}

		public async Task Save()
		{
			await RankingSettings.Save();
			await PlayerSettings.Save();
			await NGSettings.Save();
			await SearchSettings.Save();
		}

		public RankingSettings RankingSettings { get; private set; }

		public PlayerSettings PlayerSettings { get; private set; }

		public NGSettings NGSettings { get; private set; }

		public SearchSeetings SearchSettings { get; private set; }

		public HohoemaUserSettings()
		{
		}
	}

	[DataContract]
	public abstract class SettingsBase : BindableBase
	{
		public SettingsBase()
		{
			_FileLock = new SemaphoreSlim(1, 1);
		}

		public StorageFile File { get; private set; }
//		public string FileName { get; private set; }
//		public StorageFolder Folder { get; private set; }

		public SemaphoreSlim _FileLock;

		public static async Task<T> Load<T>(string filename, StorageFolder folder)
			where T : SettingsBase, new()
		{
			var localFolder = folder;
			var local = await localFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);

			return await Load<T>(local);
		}


		public static async Task<T> Load<T>(StorageFile file)
			where T : SettingsBase, new()
		{
			var rawText = await FileIO.ReadTextAsync(file);
			if (!String.IsNullOrEmpty(rawText))
			{
				try
				{
					var obj = JsonConvert.DeserializeObject<T>(rawText);
					obj.File = file;
					return obj;
				}
				catch
				{
					await file.DeleteAsync();
				}
			}

			var newInstance = new T()
			{
				File = file
			};

			newInstance.OnInitialize();

			return newInstance;
		}


		public async Task Save()
		{
			try
			{
				await _FileLock.WaitAsync().ConfigureAwait(false);
				var serializedText = JsonConvert.SerializeObject(this);

				await FileIO.WriteTextAsync(File, serializedText);
			}
			finally
			{
				_FileLock.Release();
			}
		}

		public virtual void OnInitialize() { }

		protected virtual void Validate() { }
	}
}
