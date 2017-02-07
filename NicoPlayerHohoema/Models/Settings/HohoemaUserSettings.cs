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
		public const string RankingSettingsFileName = "ranking.json";
		public const string PlayerSettingsFileName = "player.json";
		public const string NGSettingsFileName = "ng.json";
		public const string SearchSettingsFileName = "search.json";
		public const string CacheSettingsFileName = "cache.json";
        public const string PlaylistSettingsFileName = "playlist.json";


        public static async Task<HohoemaUserSettings> LoadSettings(StorageFolder userFolder)
		{
			var ranking = await SettingsBase.Load<RankingSettings>(RankingSettingsFileName, userFolder);
			var player = await SettingsBase.Load<PlayerSettings>(PlayerSettingsFileName, userFolder);
			var ng = await SettingsBase.Load<NGSettings>(NGSettingsFileName, userFolder);
			var cache = await SettingsBase.Load<CacheSettings>(CacheSettingsFileName, userFolder);
            var playlist = await SettingsBase.Load<PlaylistSettings>(PlaylistSettingsFileName, userFolder);

            return new HohoemaUserSettings()
			{
				RankingSettings = ranking,
				PlayerSettings = player,
				NGSettings = ng,
				CacheSettings = cache,
                PlaylistSettings = playlist
            };
		}

		public async Task Save()
		{
			await RankingSettings.Save();
			await PlayerSettings.Save();
			await NGSettings.Save();
			await CacheSettings.Save();
            await PlaylistSettings.Save();
        }

		public RankingSettings RankingSettings { get; private set; }
		public PlayerSettings PlayerSettings { get; private set; }
		public NGSettings NGSettings { get; private set; }
		public CacheSettings CacheSettings { get; private set; }
        public PlaylistSettings PlaylistSettings { get; private set; }

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

		public string FileName { get; private set; }
		public StorageFolder Folder { get; private set; }

		
		public SemaphoreSlim _FileLock;

		


		public static async Task<T> Load<T>(string filename, StorageFolder folder)
			where T : SettingsBase, new()
		{
			var file = await folder.TryGetItemAsync(filename) as StorageFile;
			T result = null;
			if (file != null)
			{
				var rawText = await FileIO.ReadTextAsync(file);
				if (!String.IsNullOrEmpty(rawText))
				{
					try
					{
						var obj = JsonConvert.DeserializeObject<T>(rawText);
						obj.FileName = filename;
						obj.Folder = folder;
						result = obj;
					}
					catch
					{
						await file.DeleteAsync();
					}
				}
			}

			if (result == null)
			{
				var newInstance = new T()
				{
					FileName = filename,
					Folder = folder
				};

				newInstance.OnInitialize();

				result = newInstance;

				await result.Save();
			}

			return result;
		}


		public async Task Save()
		{
			try
			{
				await _FileLock.WaitAsync();
				var file = await Folder.CreateFileAsync(FileName, CreationCollisionOption.OpenIfExists);
				var serializedText = JsonConvert.SerializeObject(this);

				await FileIO.WriteTextAsync(file, serializedText);
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
