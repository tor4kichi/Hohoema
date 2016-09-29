using Mntone.Nico2;
using Mntone.Nico2.Users.Fav;
using Mntone.Nico2.Users.Video;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Foundation;
using System.Runtime.InteropServices.WindowsRuntime;
using Prism.Mvvm;

namespace NicoPlayerHohoema.Models
{
	public class FavManager
	{

		#region Niconico fav constants

		public const uint FAV_USER_MAX_COUNT = 50;
		public const uint PREMIUM_FAV_USER_MAX_COUNT = 400;

		public const uint FAV_MYLIST_MAX_COUNT = 20;
		public const uint PREMIUM_FAV_MYLIST_MAX_COUNT = 50;

		public const uint FAV_TAG_MAX_COUNT = 10;
		public const uint PREMIUM_FAV_TAG_MAX_COUNT = 10;

		public const uint FAV_COMMUNITY_MAX_COUNT = 50;
		public const uint PREMIUM_FAV_COMMUNITY_MAX_COUNT = 300;

		#endregion


		public static async Task<FavManager> Create(HohoemaApp hohoemaApp, uint userId)
		{
			var favManager = new FavManager(hohoemaApp, userId);

			var updater = new SimpleBackgroundUpdate("favmanager_" + userId, () => favManager.Initialize());
			await hohoemaApp.BackgroundUpdater.Schedule(updater);

			return favManager;
		}


		#region Properties 

		public uint UserId { get; set; }


		public IFavInfoGroup Tag { get; private set; }
		public IFavInfoGroup Mylist { get; private set; }
		public IFavInfoGroup User { get; private set; }
		public IFavInfoGroup Community { get; private set; }


		public IReadOnlyList<IFavInfoGroup> GetAllFavInfoGroups() => new[] 
		{
			Tag,
			Mylist,
			User,
			Community
		};
			

		#endregion


		#region Fields

		HohoemaApp _HohoemaApp;

		#endregion

		internal FavManager(HohoemaApp hohoemaApp, uint userId)
		{
			_HohoemaApp = hohoemaApp;
			UserId = userId;
		}


		private async Task Initialize()
		{
			Tag = new TagFavInfoGroup(_HohoemaApp);
			Mylist = new MylistFavInfoGroup(_HohoemaApp);
			User = new UserFavInfoGroup(_HohoemaApp);
			Community = new CommunityFavInfoGroup(_HohoemaApp);

			await SyncAll();
		}

		private IFavInfoGroup GetFavInfoGroup(FavoriteItemType itemType)
		{
			switch (itemType)
			{
				case FavoriteItemType.Tag:
					return Tag;
				case FavoriteItemType.Mylist:
					return Mylist;
				case FavoriteItemType.User:
					return User;
				case FavoriteItemType.Community:
					return Community;
				default:
					throw new Exception();
			}
		}

		public bool CanMoreAddFavorite(FavoriteItemType itemType)
		{
			return GetFavInfoGroup(itemType).CanMoreAddFavorite();
		}

		

		public bool IsFavoriteItem(FavoriteItemType itemType, string id)
		{
			var group = GetFavInfoGroup(itemType);

			if (itemType == FavoriteItemType.Tag)
			{
				id = TagStringHelper.ToEnsureHankakuNumberTagString(id);
			}

			return group.IsFavoriteItem(id);
		}


	

		public async Task SyncAll()
		{
			foreach (var favInfoGroup in GetAllFavInfoGroups())
			{
				await Sync(favInfoGroup);

				await Task.Delay(1000);
			}
		}


		public Task Sync(FavoriteItemType itemType)
		{
			switch (itemType)
			{
				case FavoriteItemType.Tag:
					return Sync(Tag);
				case FavoriteItemType.Mylist:
					return Sync(Mylist);
				case FavoriteItemType.User:
					return Sync(User);
				case FavoriteItemType.Community:
					return Sync(Community);
				default:
					return Task.CompletedTask;
			}
		}


		private async Task Sync(IFavInfoGroup group)
		{
			await group.Sync();
		}

		



		public FavInfo FindFavInfo(FavoriteItemType itemType, string id)
		{
			return GetFavInfoGroup(itemType).FavInfoItems.SingleOrDefault(x => x.Id == id);
		}

		public async Task<ContentManageResult> AddFav(FavoriteItemType itemType, string id, string name)
		{
			var group = GetFavInfoGroup(itemType);

			var result = await group.AddFav(name, id);
		
			return result;
		}

		public async Task<ContentManageResult> RemoveFav(FavoriteItemType itemType, string id)
		{
			var group = GetFavInfoGroup(itemType);

			var result = await group.RemoveFav(id);

			return result;
		}



	}

}
