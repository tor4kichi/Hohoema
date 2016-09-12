using NicoPlayerHohoema.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace NicoPlayerHohoema.Models
{
	// FavFeedGroupを管理する

	// 常にFavManagerへのアクションを購読して
	// 自身が管理するfavFeedGroupが参照しているFavが購読解除された場合に、
	// グループ内からも削除するように働く

	// フィードの更新を指揮する

	// フィードの保存処理をコントロールする

	public class FeedManager
	{
		static Newtonsoft.Json.JsonSerializerSettings FeedGroupSerializerSettings = new Newtonsoft.Json.JsonSerializerSettings()
		{
			TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects
		};


		public HohoemaApp HohoemaApp { get; private set; }

		public uint UserId { get; private set; }


		public Dictionary<FeedGroup, FileAccessor<FeedGroup>> FeedGroupDict { get; private set; }
		public IReadOnlyCollection<FeedGroup> FeedGroups
		{
			get
			{
				return FeedGroupDict.Keys;
			}
		}
		


		public FeedManager(HohoemaApp hohoemaApp, uint userId)
		{
			HohoemaApp = hohoemaApp;
			UserId = userId;
			FeedGroupDict = new Dictionary<FeedGroup, FileAccessor<FeedGroup>>();
		}
		

		public async Task<StorageFolder> GetFeedGroupFolder()
		{
			var folder = await HohoemaApp.GetApplicationLocalDataFolder();

			return await folder.CreateFolderAsync("feed", CreationCollisionOption.OpenIfExists);
		}
		
		internal async Task Initialize()
		{
			var feedGroupFolder = await GetFeedGroupFolder();

			var files = await feedGroupFolder.GetFilesAsync();

			foreach (var file in files)
			{
				if (file.FileType == ".json")
				{
					var fileName = file.Name;
					var fileAccessor = new FileAccessor<FeedGroup>(feedGroupFolder, fileName);

					try
					{
						var item = await fileAccessor.Load(FeedGroupSerializerSettings);
						if (item != null)
						{
							item.HohoemaApp = this.HohoemaApp;
							item.FeedManager = this;
							FeedGroupDict.Add(item, fileAccessor);
						}
						Debug.WriteLine($"FeedManager: [Sucesss] load {item.Label}");

					}
					catch
					{
						Debug.WriteLine($"FeedManager: [Failed] load {file.Path}");
					}
				}
			}

			Debug.WriteLine($"FeedManager: {FeedGroupDict.Count} 件のFeedGroupを読み込みました。");


			var updater = new SimpleBackgroundUpdate("feedManager_" + UserId, () => Refresh());
			await HohoemaApp.BackgroundUpdater.Schedule(updater);
		}


		public FeedGroup GetFeedGroup(Guid id)
		{
			return FeedGroups.SingleOrDefault(x => x.Id == id);
		}


		private async Task Refresh()
		{
			foreach (var items in FeedGroups)
			{
				await Task.Delay(500);

				await items.Refresh();
			}
		}

		

		private Task _Save(KeyValuePair<FeedGroup, FileAccessor<FeedGroup>> feedItem)
		{
			var fileAccessor = feedItem.Value;
			return fileAccessor.Save(feedItem.Key, FeedGroupSerializerSettings);
		}

		public async Task Save()
		{
			foreach (var feedGroupTuple in FeedGroupDict)
			{
				await _Save(feedGroupTuple);
			}
		}

		public Task SaveOne(FeedGroup group)
		{
			var target = FeedGroupDict.SingleOrDefault(x => x.Key.Id == group.Id);
			return _Save(target);
		}

		public async Task<FeedGroup> AddFeedGroup(string label)
		{
			var folder = await GetFeedGroupFolder();

			var feedGroup = new FeedGroup(label);
			var fileAccessor = new FileAccessor<FeedGroup>(folder, label + ".json");

			feedGroup.HohoemaApp = this.HohoemaApp;
			feedGroup.FeedManager = this;
			FeedGroupDict.Add(feedGroup, fileAccessor);

			await fileAccessor.Save(feedGroup);
			return feedGroup;
		}

		

		public bool CanAddLabel(string label)
		{
			if (String.IsNullOrWhiteSpace(label)) { return false; }

			return FeedGroups.All(x => x.Label != label);
		}


		public async Task<bool> RemoveFeedGroup(FeedGroup group)
		{
			var removeTarget = FeedGroups.SingleOrDefault(x => x.Id == group.Id);

			if (removeTarget != null)
			{
				var fileAccessor = FeedGroupDict[removeTarget];
				await fileAccessor.Delete(StorageDeleteOption.PermanentDelete);
				return FeedGroupDict.Remove(removeTarget);
			}
			else
			{
				return false;
			}
		}


		internal Task<bool> RenameFeedGroup(FeedGroup group, string newLabel)
		{
			var target = FeedGroups.SingleOrDefault(x => x.Id == group.Id);

			if (target != null)
			{
				var fileAccessor = FeedGroupDict[target];

				return fileAccessor.Rename(newLabel + ".json");
			}
			else
			{
				return Task.FromResult(false);
			}
		}
		

		public async Task MarkAsRead(string videoId)
		{
			foreach (var group in FeedGroupDict)
			{
				var feedGroup = group.Key;
				if (feedGroup.MarkAsRead(videoId))
				{
					await _Save(group);
				}
			}			
		}
		

		public async Task MarkAsReadAllVideo()
		{
			foreach (var group in FeedGroupDict)
			{
				var feedGroup = group.Key;
				feedGroup.ForceMarkAsRead();
				await _Save(group);				
			}
		}
		
	}
}
