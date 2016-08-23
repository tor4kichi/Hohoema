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
		public HohoemaApp HohoemaApp { get; private set; }

		public uint UserId { get; private set; }


		public List<Tuple<FileAccessor<FeedGroup>, FeedGroup>> FeedGroups { get; private set; }

		public FeedManager(HohoemaApp hohoemaApp, uint userId)
		{
			HohoemaApp = hohoemaApp;
			UserId = userId;
			FeedGroups = new List<Tuple<FileAccessor<FeedGroup>, FeedGroup>>();
		}
		

		public async Task<StorageFolder> GetFeedGroupFolder()
		{
			var favFolder = await HohoemaApp.GetCurrentUserFavDataFolder();

			return await favFolder.CreateFolderAsync("feed", CreationCollisionOption.OpenIfExists);
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
						var item = await fileAccessor.Load();
						if (item != null)
						{
							item.HohoemaApp = this.HohoemaApp;
							item.FeedManager = this;
							FeedGroups.Add(new Tuple<FileAccessor<FeedGroup>, FeedGroup>(fileAccessor, item));
						}
						Debug.WriteLine($"FeedManager: [Sucesss] load {item.Label}");

					}
					catch
					{
						Debug.WriteLine($"FeedManager: [Failed] load {file.Path}");
					}
				}
			}

			Debug.WriteLine($"FeedManager: {FeedGroups.Count} 件のFeedGroupを読み込みました。");


			var updater = new SimpleBackgroundUpdate("feedManager_" + UserId, () => Refresh());
			await HohoemaApp.BackgroundUpdater.Schedule(updater);
		}


		private async Task Refresh()
		{
			foreach (var items in FeedGroups)
			{
				await items.Item2.Refresh();
			}
		}


		private Task _Save(Tuple<FileAccessor<FeedGroup>, FeedGroup> feedItem)
		{
			var fileAccessor = feedItem.Item1;
			return fileAccessor.Save(feedItem.Item2);
		}

		public async Task Save()
		{
			foreach (var feedGroupTuple in FeedGroups)
			{
				await _Save(feedGroupTuple);
			}
		}

		public Task SaveOne(FeedGroup group)
		{
			var target = FeedGroups.SingleOrDefault(x => x.Item2.Label == group.Label);
			return _Save(target);
		}

		public async Task<FeedGroup> AddFeedGroup(string label)
		{
			var folder = await GetFeedGroupFolder();

			var feedGroup = new FeedGroup(label);
			var fileAccessor = new FileAccessor<FeedGroup>(folder, label + ".json");

			var item = new Tuple<FileAccessor<FeedGroup>, FeedGroup>(fileAccessor, feedGroup);
			feedGroup.HohoemaApp = this.HohoemaApp;
			feedGroup.FeedManager = this;
			FeedGroups.Add(item);

			await _Save(item);

			return feedGroup;
		}

		

		public bool CanAddLabel(string label)
		{
			return FeedGroups.All(x => x.Item2.Label != label);
		}


		public async Task<bool> RemoveFeedGroup(FeedGroup group)
		{
			var removeTarget = FeedGroups.SingleOrDefault(x => x.Item2.Label == group.Label);

			if (removeTarget != null)
			{
				await removeTarget.Item1.Delete(StorageDeleteOption.PermanentDelete);
				return FeedGroups.Remove(removeTarget);
			}
			else
			{
				return false;
			}
		}


		internal Task RenameFeedGroup(FeedGroup group, string newLabel)
		{
			var target = FeedGroups.SingleOrDefault(x => x.Item2.Label == group.Label);

			if (target != null)
			{
				var fileAccessor = target.Item1;

				return fileAccessor.Rename(newLabel + ".json");
			}
			else
			{
				return Task.CompletedTask;
			}
		}
		

		public async Task MarkAsRead(string videoId)
		{
			foreach (var group in FeedGroups)
			{
				var feedGroup = group.Item2;
				if (feedGroup.MarkAsRead(videoId))
				{
					await _Save(group);
				}
			}			
		}
		

		public async Task MarkAsReadAllVideo()
		{
			foreach (var group in FeedGroups)
			{
				var feedGroup = group.Item2;
				feedGroup.ForceMarkAsRead();
				await _Save(group);				
			}
		}
		
	}
}
