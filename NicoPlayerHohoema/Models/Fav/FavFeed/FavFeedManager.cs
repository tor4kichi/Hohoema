using System;
using System.Collections.Generic;
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

	public class FavFeedManager
	{
		public HohoemaApp HohoemaApp { get; private set; }

		public uint UserId { get; private set; }


		public FavFeedManager(HohoemaApp hohoemaApp, uint userId)
		{
			HohoemaApp = hohoemaApp;
			UserId = userId;
		}

		public const string UserFavGroupName = "user";
		public const string MylistFavGroupName = "mylist";
		public const string TagFavGroupName = "tag";

		private async Task<StorageFolder> GetSpecifyFavFolder(string groupName, uint userId)
		{
			var favFolder = await HohoemaApp.GetCurrentUserFavDataFolder();
			return await favFolder.CreateFolderAsync(groupName, CreationCollisionOption.OpenIfExists);
		}


		public Task<StorageFolder> GetFavUserFolder(uint userId)
		{
			return GetSpecifyFavFolder(UserFavGroupName, userId);
		}

		public Task<StorageFolder> GetFavMylistFolder(uint userId)
		{
			return GetSpecifyFavFolder(MylistFavGroupName, userId);
		}

		public Task<StorageFolder> GetFavTagFolder(uint userId)
		{
			return GetSpecifyFavFolder(TagFavGroupName, userId);
		}

		
		public Task MarkAsRead(string videoId)
		{
			return Task.CompletedTask;
/*
			bool isChanged = false;
			foreach (var item in GetAllFeedItems())
			{
				if (item.VideoId == videoId && item.IsUnread)
				{
					item.IsUnread = false;
					isChanged = true;
				}
			}

			if (isChanged)
			{
				await SaveAllFavFeedLists().ConfigureAwait(false);
			}
			*/
		}
		

		public async Task MarkAsReadAllVideo()
		{
			await Task.Delay(0);
			/*
			bool isChanged = false;
			foreach (var item in GetAllFeedItems())
			{
				if (item.IsUnread)
				{
					item.IsUnread = false;
					isChanged = true;
				}
			}

			if (isChanged)
			{
				await SaveAllFavFeedLists().ConfigureAwait(false);
			}
			*/
		}
		
	}
}
