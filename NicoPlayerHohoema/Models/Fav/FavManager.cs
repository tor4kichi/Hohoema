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


		#endregion


		public const string TagFavFileName = "tag.json";
		public const string MylistFavFileName = "mylist.json";
		public const string UserFavFileName = "user.json";


		public static async Task<FavManager> Create(HohoemaApp hohoemaApp, uint userId)
		{
			var favManager = new FavManager(hohoemaApp, userId);

			var favDataFolder = await hohoemaApp.GetCurrentUserFavDataFolder();

			favManager._TagFavFileAccessor = new Util.FileAccessor<List<FavInfo>>(favDataFolder, TagFavFileName);
			favManager._MylistFavFileAccessor = new Util.FileAccessor<List<FavInfo>>(favDataFolder, MylistFavFileName);
			favManager._UserFavFileAccessor = new Util.FileAccessor<List<FavInfo>>(favDataFolder, UserFavFileName);

			var updater = new SimpleBackgroundUpdate("favmanager_" + userId, () => favManager.Initialize());
			await hohoemaApp.BackgroundUpdater.Schedule(updater);

			return favManager;
		}


		#region Properties 

		public uint UserId { get; set; }


		public IFavInfoGroup Tag { get; private set; }
		public IFavInfoGroup Mylist { get; private set; }
		public IFavInfoGroup User { get; private set; }


		#endregion


		#region Fields

		HohoemaApp _HohoemaApp;

		private Util.FileAccessor<List<FavInfo>> _TagFavFileAccessor;
		private Util.FileAccessor<List<FavInfo>> _MylistFavFileAccessor;
		private Util.FileAccessor<List<FavInfo>> _UserFavFileAccessor;


		#endregion

		internal FavManager(HohoemaApp hohoemaApp, uint userId)
		{
			_HohoemaApp = hohoemaApp;
			UserId = userId;
		}


		private async Task Initialize()
		{
			{
				var tagItems = await _TagFavFileAccessor.Load() ?? new List<FavInfo>();
				Tag = new TagFavInfoGroup(_HohoemaApp, tagItems);
			}
			{
				var mylistItems = await _MylistFavFileAccessor.Load() ?? new List<FavInfo>();
				Mylist = new MylistFavInfoGroup(_HohoemaApp, mylistItems);
			}
			{
				var userItems = await _UserFavFileAccessor.Load() ?? new List<FavInfo>();
				User = new UserFavInfoGroup(_HohoemaApp, userItems);
			}

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


		public async Task SaveAll()
		{
			await SaveTag();
			await SaveMylist();
			await SaveUser();
		}

		public Task Save(FavoriteItemType itemType)
		{
			switch (itemType)
			{
				case FavoriteItemType.Tag:
					return SaveTag();
				case FavoriteItemType.Mylist:
					return SaveMylist();
				case FavoriteItemType.User:
					return SaveUser();
				default:
					return Task.CompletedTask;
			}
		}


		private Task SaveTag()
		{
			return _TagFavFileAccessor.Save(Tag.FavInfoItems.ToList());
		}

		private Task SaveMylist()
		{
			return _MylistFavFileAccessor.Save(Mylist.FavInfoItems.ToList());
		}

		private Task SaveUser()
		{
			return _UserFavFileAccessor.Save(User.FavInfoItems.ToList());
		}


		public async Task SyncAll()
		{
			await SyncTag();
			await Task.Delay(500);
			await SyncMylist();
			await Task.Delay(500);
			await SyncUser();
		}


		public Task Sync(FavoriteItemType itemType)
		{
			switch (itemType)
			{
				case FavoriteItemType.Tag:
					return SyncTag();
				case FavoriteItemType.Mylist:
					return SyncMylist();
				case FavoriteItemType.User:
					return SyncUser();
				default:
					return Task.CompletedTask;
			}
		}


		private async Task Sync(IFavInfoGroup group)
		{
			var isNothingItem = group.FavInfoItems.Count == 0;

			await group.Sync();

			if (group.FavInfoItems.Count > 0 && isNothingItem)
			{
				await Save(group.FavoriteItemType);
			}
		}

		private Task SyncTag()
		{
			return Sync(Tag);
		}

		private Task SyncMylist()
		{
			return Sync(Mylist);
		}
		
		private Task SyncUser()
		{
			return Sync(User);
		}




		public FavInfo FindFavInfo(FavoriteItemType itemType, string id)
		{
			return GetFavInfoGroup(itemType).FavInfoItems.SingleOrDefault(x => x.Id == id);
		}

		public async Task<ContentManageResult> AddFav(FavoriteItemType itemType, string id, string name)
		{
			var group = GetFavInfoGroup(itemType);

			var result = await group.AddFav(name, id);

			if (result == ContentManageResult.Success)
			{
				await Save(itemType);
			}

			return result;
		}

		public async Task<ContentManageResult> RemoveFav(FavoriteItemType itemType, string id)
		{
			var group = GetFavInfoGroup(itemType);

			var result = await group.RemoveFav(id);

			if (result == ContentManageResult.Success)
			{
				await Save(itemType);
			}

			return result;
		}



	}

}
