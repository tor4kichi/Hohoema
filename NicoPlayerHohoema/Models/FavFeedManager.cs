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

namespace NicoPlayerHohoema.Models
{
	public class FavFeedManager 
	{
		public const uint FAV_USER_MAX_COUNT = 50;
		public const uint PREMIUM_FAV_USER_MAX_COUNT = 400;
		public const uint FAV_MYLIST_MAX_COUNT = 20;
		public const uint PREMIUM_FAV_MYLIST_MAX_COUNT = 50;
		public const uint FAV_TAG_MAX_COUNT = 10;
		public const uint PREMIUM_FAV_TAG_MAX_COUNT = 10;



		public const string UserFavGroupName = "user";
		public const string MylistFavGroupName = "mylist";
		public const string TagFavGroupName = "tag";

		private async Task<StorageFolder> GetSpecifyFavFolder(string groupName, uint userId)
		{
			var favFolder = await _HohoemaApp.GetCurrentUserFavFolder();
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



		public static async Task<FavFeedManager> Create(HohoemaApp hohoemaApp, uint userId)
		{
			var favFeedManager = new FavFeedManager(hohoemaApp, userId);

			await favFeedManager.Initialize();

			return favFeedManager;
		}


		internal FavFeedManager(HohoemaApp hohoemaApp, uint userId)
		{
			_HohoemaApp = hohoemaApp;
			UserId = userId;

			ItemsByGroupName = new Dictionary<FavoriteItemType, ObservableCollection<FavFeedList>>();
			_FavFeedWriterLock = new SemaphoreSlim(1, 1);
		}


		private async Task Initialize()
		{
			// 保存フォルダから読み込み
			var userLists = new ObservableCollection<FavFeedList>(await LoadFeedLists(await GetSpecifyFavFolder(UserFavGroupName, UserId)));
			ItemsByGroupName.Add(FavoriteItemType.User, userLists);
			var mylistLists = new ObservableCollection<FavFeedList>(await LoadFeedLists(await GetSpecifyFavFolder(MylistFavGroupName, UserId)));
			ItemsByGroupName.Add(FavoriteItemType.Mylist, mylistLists);
			var tagLists = new ObservableCollection<FavFeedList>(await LoadFeedLists(await GetSpecifyFavFolder(TagFavGroupName, UserId)));
			ItemsByGroupName.Add(FavoriteItemType.Tag, tagLists);

			await SyncAllFav();

			await UpdateAll();

			await SaveAllFavFeedLists();
		}

		public bool CanMoreAddFavorite(FavoriteItemType itemType)
		{
			var list = ItemsByGroupName[itemType];
			var isPremium = _HohoemaApp.IsPremiumUser;
			switch (itemType)
			{
				case FavoriteItemType.Tag:
					return list.Count < (isPremium ? PREMIUM_FAV_USER_MAX_COUNT   : FAV_USER_MAX_COUNT);
				case FavoriteItemType.Mylist:
					return list.Count < (isPremium ? PREMIUM_FAV_MYLIST_MAX_COUNT : FAV_MYLIST_MAX_COUNT);
				case FavoriteItemType.User:
					return list.Count < (isPremium ? PREMIUM_FAV_TAG_MAX_COUNT    : FAV_TAG_MAX_COUNT);
				default:
					return false;
			}
		}


		public bool IsFavoriteItem(FavoriteItemType itemType, string id)
		{
			ObservableCollection<FavFeedList> list = ItemsByGroupName[itemType];

			if (itemType == FavoriteItemType.Tag)
			{
				id = TagStringHelper.ToEnsureHankakuNumberTagString(id);
			}

			return list.Any(x => x.Id == id);
		}


		public async Task MarkAsRead(string videoId)
		{
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
		}

		public async Task MarkAsReadAllVideo()
		{
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
		}

		public async Task SyncAllFav()
		{
			await SyncFavUsers();
			await SyncFavMylists();
			await SyncFavTags();
		}


		private async Task SyncFavUsers()
		{
			var userFavFeedLists = GetFavUserFeedListAll();
			var userFavDatas = await _HohoemaApp.ContentFinder.GetFavUsers();

			// まだローカルデータとして登録されていないIDを追加分として抽出
			var addedItems = userFavDatas.Where(x => userFavFeedLists.All(y => y.Id != x.ItemId))
				.Select(x => new FavFeedList()
				{
					Id = x.ItemId,
					FavoriteItemType = FavoriteItemType.User,
					Name = x.Title,
					FeedSource = FeedSource.Account,
					UserLabel = "default",
				});

			foreach (var addItem in addedItems)
			{
				userFavFeedLists.Add(addItem);
			}


			// オンラインデータから削除されているアイテムを抽出
			var removedItems = userFavFeedLists.Where(x => !userFavDatas.Any(y => x.Id == y.ItemId)).ToList();
			foreach (var removeItem in removedItems)
			{
				userFavFeedLists.Remove(removeItem);
			}
		}

		private async Task SyncFavMylists()
		{
			var mylistFavFeedLists = GetFavMylistFeedListAll();
			var mylistfavDatas = await _HohoemaApp.ContentFinder.GetFavMylists();


			var addedItems = mylistfavDatas.Where(x => mylistFavFeedLists.All(y => y.Id != x.ItemId))
				.Select(x => new FavFeedList()
				{
					Id = x.ItemId,
					FavoriteItemType = FavoriteItemType.Mylist,
					Name = x.Title,
					FeedSource = FeedSource.Account,
					UserLabel = "default",
				});

			foreach (var addItem in addedItems)
			{
				mylistFavFeedLists.Add(addItem);
			}


			var removedItems = mylistFavFeedLists.Where(x => !mylistfavDatas.Any(y => x.Id == y.ItemId)).ToList();
			foreach (var removeItem in removedItems)
			{
				removeItem.IsDeleted = true;
				mylistFavFeedLists.Remove(removeItem);
			}

		}


		private async Task SyncFavTags()
		{
			var tagFavFeedLists = GetFavTagFeedListAll();
			var tagFavDatas = await _HohoemaApp.ContentFinder.GetFavTags();


			var addedItems = tagFavDatas.Where(x => tagFavFeedLists.All(y => y.Name != x))
				.Select(x => new FavFeedList()
				{
					Id = x,
					FavoriteItemType = FavoriteItemType.Tag,
					Name = x,
					FeedSource = FeedSource.Account,
					UserLabel = "default",
				});

			foreach (var addItem in addedItems)
			{
				tagFavFeedLists.Add(addItem);
			}



			var removedItems = tagFavFeedLists.Where(x => !tagFavDatas.Any(y => x.Name == y)).ToList();
			foreach (var removeItem in removedItems)
			{
				removeItem.IsDeleted = true;
				tagFavFeedLists.Remove(removeItem);
			}

		}


		private async Task<IList<FavFeedList>> LoadFeedLists(StorageFolder folder)
		{
			var files = await folder.GetFilesAsync();

			List<FavFeedList> list = new List<FavFeedList>();

			foreach (var file in files)
			{
				list.Add(await LoadFeedList(file));
			}

			return list;
		}

		private async Task<FavFeedList> LoadFeedList(StorageFile file)
		{			
			var text = await FileIO.ReadTextAsync(file);
			var favFeedList = Newtonsoft.Json.JsonConvert.DeserializeObject<FavFeedList>(text);
			return favFeedList;
		}


		public async Task SaveAllFavFeedLists()
		{
			await SaveUserFavFeedLists();
			await SaveMylistFavFeedLists();
			await SaveTagFavFeedLists();
		}

		public async Task SaveUserFavFeedLists()
		{
			foreach (var list in GetFavUserFeedListAll())
			{
				await SaveFavFeedList(list);
			}
		}

		public async Task SaveMylistFavFeedLists()
		{
			foreach (var list in GetFavMylistFeedListAll())
			{
				await SaveFavFeedList(list);
			}
		}

		public async Task SaveTagFavFeedLists()
		{
			foreach (var list in GetFavTagFeedListAll())
			{
				await SaveFavFeedList(list);
			}
		}


		private async Task SaveFavFeedList(FavFeedList feedList)
		{

			var serializedText = JsonConvert.SerializeObject(feedList);

			var saveFile = await GetFeedListFile(feedList);

			try
			{
				await _FavFeedWriterLock.WaitAsync();

				await FileIO.WriteTextAsync(saveFile, serializedText);
			}
			catch(Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
			finally
			{
				_FavFeedWriterLock.Release();
			}
		}

		private SemaphoreSlim _FavFeedWriterLock;

		private async Task UpdateFavFeedList(IList<FavFeedList> lists)
		{
			foreach (var feedList in lists)
			{
				await UpdateFavFeedList(feedList);
			}
		}


		public async Task UpdateAll()
		{
			await UpdateFavUsers();
			await UpdateFavMylists();
			await UpdateFavTags();
		}

		public async Task UpdateFavUsers()
		{
			await UpdateFavFeedList(GetFavUserFeedListAll());
		}

		public async Task UpdateFavMylists()
		{
			await UpdateFavFeedList(GetFavMylistFeedListAll());
		}

		public async Task UpdateFavTags()
		{
			await UpdateFavFeedList(GetFavTagFeedListAll());
		}



		public async Task UpdateFavFeedList(FavFeedList feedList)
		{
			List<FavFeedItem> newItems = null;
			switch (feedList.FavoriteItemType)
			{
				case FavoriteItemType.User:
					newItems = await GetUserFeedItems(feedList);
					break;
				case FavoriteItemType.Mylist:
					newItems = await GetMylistFeedItems(feedList);
					break;
				case FavoriteItemType.Tag:
					newItems = await GetTagFeedItems(feedList);
					break;
				default:
					break;
			}

			if (newItems == null)
			{
				return;
			}

			var updateTime = DateTime.Now;
			foreach (var item in newItems)
			{
				item.CheckedTime = updateTime;
//				item.IsNewItem = true;
			}

			await MergeFavFeedList(feedList, newItems, updateTime);


			// ユーザーの投稿動画を取得する
			// 保存済みの情報

			await SaveFavFeedList(feedList);
		}


		


		private async Task<List<FavFeedItem>> GetUserFeedItems(FavFeedList userFavFeedList)
		{
			var userVideos = await _HohoemaApp.ContentFinder.GetUserVideos(uint.Parse(userFavFeedList.Id), 1);

			var list = new List<FavFeedItem>();

			foreach (var video in userVideos.Items)
			{
				var item = new FavFeedItem()
				{
					VideoId = video.VideoId,
					Title = video.Title,
					ParentList = userFavFeedList,
				};

				list.Add(item);
			}

			return list;
		}


		private async Task<List<FavFeedItem>> GetTagFeedItems(FavFeedList tagFavFeedList)
		{
			var tagVideos = await _HohoemaApp.ContentFinder.GetTagSearch(tagFavFeedList.Id, 1, SortMethod.FirstRetrieve);

			return tagVideos.list.Select(x => 
			{
				return new FavFeedItem()
				{
					VideoId = x.id,
					Title = x.title,
					SubmitDate = x.FirstRetrieve,
					ParentList = tagFavFeedList,
				};
			})
			.ToList();
		}

		private async Task<List<FavFeedItem>> GetMylistFeedItems(FavFeedList mylistFavFeedList)
		{
			var response = await _HohoemaApp.ContentFinder.GetMylistItems(mylistFavFeedList.Id);

			return response.Video_info.Select(x => 
			{
				return new FavFeedItem()
				{
					VideoId = x.Video.Id,
					Title = x.Video.Title,
					SubmitDate = DateTime.Parse(x.Video.First_retrieve),
					IsDeleted = int.Parse(x.Video.Deleted) == 0 ? false : true,
					ParentList = mylistFavFeedList,
				};
			})
			.ToList();
		}


		private async Task MergeFavFeedList(FavFeedList feedList, List<FavFeedItem> items, DateTime updateTime)
		{
			var exceptItems = items.Except(feedList.Items, FavFeedItemComparer.Default).ToList();

			var addedItems = exceptItems.Where(x => x.CheckedTime == updateTime).ToList();

			var removedItems = exceptItems.Except(addedItems, FavFeedItemComparer.Default);

			foreach (var addItem in addedItems)
			{
				addItem.IsUnread = true;

				// 投稿日時が初期化されていない場合はThumbnailInfoから拾ってくる

				if (addItem.SubmitDate == default(DateTime))
				{
					try
					{
						var nicoVideo = await _HohoemaApp.MediaManager.GetNicoVideo(addItem.VideoId);
						var thumbnail = await nicoVideo.GetThumbnailResponse();

						addItem.SubmitDate = thumbnail.PostedAt.DateTime;
					}
					catch (Exception ex)
					{
						Debug.Fail("UserFeedItem 更新中、NicoVideoオブジェクトの取得に失敗しました。", ex.Message);
					}
				}
			

				feedList.Items.Add(addItem);

				AddFavFeedEvent?.Invoke(addItem);
			}


			foreach (var removedItem in removedItems)
			{
				var item = feedList.Items.SingleOrDefault(x => x.VideoId == removedItem.VideoId);
				if (item != null)
				{
					item.IsDeleted = true;
					feedList.Items.Remove(item);
				}
			}

			feedList.Items.Sort();

			feedList.UpdateTime = updateTime;
		}



		private FavFeedList GetUserFeed(string favUserId)
		{
			return GetFavUserFeedListAll()
				.SingleOrDefault(x => x.Id == favUserId);
		}


		public ObservableCollection<FavFeedList> GetFavUserFeedListAll()
		{
			return ItemsByGroupName[FavoriteItemType.User];
		}

		public ObservableCollection<FavFeedList> GetFavMylistFeedListAll()
		{
			return ItemsByGroupName[FavoriteItemType.Mylist];
		}

		public ObservableCollection<FavFeedList> GetFavTagFeedListAll()
		{
			return ItemsByGroupName[FavoriteItemType.Tag];
		}


		public FavFeedList FindFavFeedList(FavoriteItemType itemType, string id)
		{
			return ItemsByGroupName[itemType].SingleOrDefault(x => x.Id == id);
		}


		public IEnumerable<FavFeedItem> GetAllFeedItems()
		{
			var mylistFeeds = this.GetFavMylistFeedListAll().SelectMany(x => x.Items);
			var tagFeeds = this.GetFavTagFeedListAll().SelectMany(x => x.Items);
			var userFeeds = this.GetFavUserFeedListAll().SelectMany(x => x.Items);

			var allFeeds = mylistFeeds.Concat(tagFeeds).Concat(userFeeds);

			return allFeeds.OrderBy(x => x.SubmitDate).Reverse();
		}

		public IEnumerable<FavFeedItem> GetUnreadFeedItems()
		{
			var mylistFeeds = this.GetFavMylistFeedListAll().SelectMany(x => x.Items);
			var tagFeeds = this.GetFavTagFeedListAll().SelectMany(x => x.Items);
			var userFeeds = this.GetFavUserFeedListAll().SelectMany(x => x.Items);

			var allFeeds = mylistFeeds.Concat(tagFeeds).Concat(userFeeds);
				
			var allUnreadFeeds = allFeeds.Where(x => x.IsUnread);

			return allUnreadFeeds.OrderBy(x => x.SubmitDate).Reverse();
		}


		public async Task<ContentManageResult> AddFav(FavoriteItemType itemType, string id, string name)
		{
			ContentManageResult? result = null;
			switch (itemType)
			{
				case FavoriteItemType.Tag:
					result = await _HohoemaApp.NiconicoContext.User.AddFavTagAsync(id);
					break;
				case FavoriteItemType.Mylist:
					result = await _HohoemaApp.NiconicoContext.User.AddUserFavAsync(NiconicoItemType.Mylist, id);
					break;
				case FavoriteItemType.User:
					result = await _HohoemaApp.NiconicoContext.User.AddUserFavAsync(NiconicoItemType.User, id);
					break;
				default:
					throw new NotSupportedException();
			}

			if (result.Value == ContentManageResult.Success)
			{
				var newList = new FavFeedList()
				{
					Name = name,
					Id = id,
					FavoriteItemType = itemType,
				};
				ItemsByGroupName[itemType].Add(newList);

				await UpdateFavFeedList(newList);
			}

			return result.Value;
		}

		public async Task<ContentManageResult> RemoveFav(FavoriteItemType itemType, string id)
		{

			ContentManageResult? result = null;
			switch (itemType)
			{
				case FavoriteItemType.Tag:
					result = await _HohoemaApp.NiconicoContext.User.RemoveFavTagAsync(id);
					break;
				case FavoriteItemType.Mylist:
					result = await _HohoemaApp.NiconicoContext.User.RemoveUserFavAsync(NiconicoItemType.Mylist, id);
					break;
				case FavoriteItemType.User:
					result = await _HohoemaApp.NiconicoContext.User.RemoveUserFavAsync(NiconicoItemType.User, id);

					break;
				default:
					throw new NotSupportedException();
			}

			if (result.Value == ContentManageResult.Success)
			{
				var list = ItemsByGroupName[itemType];
				var removeTarget = list.SingleOrDefault(x => x.Id == id);
				list.Remove(removeTarget);
			}

			return result.Value;
		}



		public async Task RemoveLocalStoreData()
		{
			foreach (var lists in ItemsByGroupName.Values)
			{
				foreach (var list in lists)
				{
					await RemoveFeedListLocalData(list);
				}
			}
		}

		public async Task RemoveFeedListLocalData(FavFeedList feedList)
		{
			var saveFile = await GetFeedListFile(feedList);
			await saveFile.DeleteAsync(StorageDeleteOption.PermanentDelete);

			feedList.Items.Clear();
			feedList.UpdateTime = DateTime.MinValue;
		}

		private async Task<StorageFile> GetFeedListFile(FavFeedList feedList)
		{
			string SaveFolderName = null;
			switch (feedList.FavoriteItemType)
			{
				case FavoriteItemType.User:
					SaveFolderName = UserFavGroupName;
					break;
				case FavoriteItemType.Mylist:
					SaveFolderName = MylistFavGroupName;
					break;
				case FavoriteItemType.Tag:
					SaveFolderName = TagFavGroupName;
					break;
				default:
					break;
			}

			if (string.IsNullOrEmpty(SaveFolderName))
			{
				throw new NotSupportedException();
			}

			var saveFolder = await GetSpecifyFavFolder(SaveFolderName, UserId);
			return await saveFolder.CreateFileAsync($"{feedList.Id}.json", CreationCollisionOption.OpenIfExists);
		}

		public uint UserId { get; set; }

		public Dictionary<FavoriteItemType, ObservableCollection<FavFeedList>> ItemsByGroupName { get; private set; }

		HohoemaApp _HohoemaApp;

		public event Action<FavFeedItem> AddFavFeedEvent;
	}

}
