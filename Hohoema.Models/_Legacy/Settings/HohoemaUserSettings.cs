﻿using System;
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
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using Hohoema.Models.Repository.NicoRepo;

namespace Hohoema.Models
{
    [Obsolete]
    public class HohoemaUserSettings
	{
		public const string RankingSettingsFileName = "ranking.json";
        public const string PlaylistSettingsFileName = "playlist.json";
        public const string NGSettingsFileName = "ng.json";

        public const string PlayerSettingsFileName = "player.json";
        public const string CacheSettingsFileName = "cache.json";
        public const string AppearanceSettingsFileName = "appearance.json";
        public const string NicoRepoAndFeedSettingsFileName = "nicorepo_feed.json";
        public const string PinSettingsFileName = "pin.json";


        public static async Task<HohoemaUserSettings> LoadSettings(StorageFolder userFolder)
		{
			var ranking = await SettingsBase.Load<RankingSettings>(RankingSettingsFileName, userFolder);
            var ng = await SettingsBase.Load<NGSettings>(NGSettingsFileName, userFolder);

            var player = await SettingsBase.Load<PlayerSettings>(PlayerSettingsFileName, userFolder);
            var cache = await SettingsBase.Load<CacheSettings>(CacheSettingsFileName, userFolder);
            var appearance = await SettingsBase.Load<AppearanceSettings>(AppearanceSettingsFileName, userFolder);
            var nicorepoAndFeed = await SettingsBase.Load<ActivityFeedSettings>(NicoRepoAndFeedSettingsFileName, userFolder);

            var pin = await SettingsBase.Load<PinSettings>(PinSettingsFileName, userFolder);

            if (nicorepoAndFeed.DisplayNicoRepoItemTopics.Count == 0)
            {
                nicorepoAndFeed.DisplayNicoRepoItemTopics.AddRange(new []
                {
                    NicoRepoItemTopic.NicoVideo_User_Video_Upload,
                    NicoRepoItemTopic.NicoVideo_User_Mylist_Add_Video,
                    NicoRepoItemTopic.Live_Channel_Program_Onairs,
                    NicoRepoItemTopic.Live_Channel_Program_Reserve,
                    NicoRepoItemTopic.NicoVideo_Channel_Video_Upload,
                    NicoRepoItemTopic.Live_User_Program_OnAirs,
                    NicoRepoItemTopic.Live_User_Program_Reserve,
                });
            }

            var settings = new HohoemaUserSettings()
			{
				RankingSettings = ranking,
                PlayerSettings = player,
                NGSettings = ng,
                CacheSettings = cache,
                AppearanceSettings = appearance,
                ActivityFeedSettings = nicorepoAndFeed,
                PinSettings = pin,
            };

            //settings.SetupSaveWithPropertyChanged();

            return settings;
		}

        public RankingSettings RankingSettings { get; private set; }
		public CacheSettings CacheSettings { get; private set; }
        public AppearanceSettings AppearanceSettings { get; private set; }
        public ActivityFeedSettings ActivityFeedSettings { get; private set; }
        public PinSettings PinSettings { get; private set; }
        public NGSettings NGSettings { get; private set; }
        public PlayerSettings PlayerSettings { get; private set; }

        public HohoemaUserSettings()
		{
            
        }

        private void SetupSaveWithPropertyChanged()
        {
            NGSettings.PropertyChangedAsObservable()
                .Throttle(TimeSpan.FromSeconds(1))
                .Subscribe(e => _ = RankingSettings.Save());

            RankingSettings.PropertyChangedAsObservable()
                .Throttle(TimeSpan.FromSeconds(1))
                .Subscribe(e => _ = RankingSettings.Save());

            CacheSettings.PropertyChangedAsObservable()
                .Throttle(TimeSpan.FromSeconds(1))
                .Subscribe(e => _ = CacheSettings.Save());

            AppearanceSettings.PropertyChangedAsObservable()
                .Throttle(TimeSpan.FromSeconds(1))
                .Subscribe(e => _ = AppearanceSettings.Save());

            ActivityFeedSettings.PropertyChangedAsObservable()
                .Throttle(TimeSpan.FromSeconds(1))
                .Subscribe(e => _ = ActivityFeedSettings.Save());

            PlayerSettings.PropertyChangedAsObservable()
                .Throttle(TimeSpan.FromSeconds(1))
                .Subscribe(e => _ = PlayerSettings.Save());

            new[] {
                PinSettings.PropertyChangedAsObservable().ToUnit(),
                PinSettings.Pins.CollectionChangedAsObservable().ToUnit(),
                PinSettings.Pins.ObserveElementPropertyChanged().ToUnit()
            }
            .Merge()
            .Throttle(TimeSpan.FromSeconds(1))
            .Subscribe(pair => _ = PinSettings.Save());
        }
    }

    [Obsolete]
	[DataContract]
	public abstract class SettingsBase : FixPrism.BindableBase
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

        [Obsolete]
		public async Task Save()
		{
            await Task.CompletedTask;
			//try
			//{
			//	await _FileLock.WaitAsync();
			//	var file = await Folder.CreateFileAsync(FileName, CreationCollisionOption.OpenIfExists);
			//	var serializedText = JsonConvert.SerializeObject(this);

			//	await FileIO.WriteTextAsync(file, serializedText);
			//}
   //         catch (FileNotFoundException)
   //         {
   //             System.Diagnostics.Debug.WriteLine($" failed save setting. {FileName}");
   //         }
			//finally
			//{
			//	_FileLock.Release();
			//}
		}

        public async Task<StorageFile> GetFile()
        {
            return await Folder.TryGetItemAsync(FileName) as StorageFile;
        }


        public virtual void OnInitialize() { }

		protected virtual void Validate() { }
	}
}
