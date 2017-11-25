using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using NicoPlayerHohoema.Helpers;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;

namespace NicoPlayerHohoema.Models
{
	public class UserMylistManager : AsyncInitialize
    {
		public const int MaxUserMylistGroupCount = 25;
        public const int MaxPremiumUserMylistGroupCount = 50;



        public HohoemaApp HohoemaApp { get; private set; }

		public MylistGroupInfo Deflist { get; private set; }

		private ObservableCollection<MylistGroupInfo> _UserMylists;
		public ReadOnlyObservableCollection<MylistGroupInfo> UserMylists { get; private set; }

        private AsyncLock _UpdateLock = new AsyncLock();

        public int DeflistRegistrationCapacity
		{
			get
			{
				return HohoemaApp.IsPremiumUser ? 500 : 100;
			}
		}

		public int DeflistRegistrationCount
		{
			get
			{
				return Deflist.ItemCount;
			}
		}
		public int MylistRegistrationCapacity
		{
			get
			{
				return HohoemaApp.IsPremiumUser ? 25000 : 100;
			}
		}
		public int MylistRegistrationCount
		{
			get
			{
				return UserMylists
					.Where(x => !x.IsDeflist)
					.Sum(x => x.ItemCount);
			}
		}


		

		public UserMylistManager(HohoemaApp app)
		{
			HohoemaApp = app;

			_UserMylists = new ObservableCollection<MylistGroupInfo>();
			UserMylists = new ReadOnlyObservableCollection<MylistGroupInfo>(_UserMylists);

			app.OnSignin += App_OnSignin;
			app.OnSignout += App_OnSignout;
		}

		private void App_OnSignout()
		{
			_UserMylists.Clear();
		}

		private void App_OnSignin()
		{
            Initialize();
		}

        public int MaxMylistGroupCountCurrentUser => HohoemaApp.IsPremiumUser ? MaxPremiumUserMylistGroupCount : MaxUserMylistGroupCount;



        public bool CanAddMylistGroup
		{
			get
			{
				return UserMylists.Count < MaxMylistGroupCountCurrentUser;
			}
		}

		public bool HasMylistGroup(string groupId)
		{
			return UserMylists.Any(x => x.GroupId == groupId);
		}


		public MylistGroupInfo GetMylistGroup(string groupId)
		{
			return UserMylists.SingleOrDefault(x => x.GroupId == groupId);
		}


		public async Task SyncMylistGroups()
		{
            using (var releaser = await _UpdateLock.LockAsync())
            {
                if (_UserMylists.Count == 0)
                {
                    // とりあえずマイリストを手動で追加
                    Deflist = new MylistGroupInfo("0", HohoemaApp, this)
                    {
                        Name = "とりあえずマイリスト",
                        Description = "ユーザーの一時的なマイリストです",
                        UserId = HohoemaApp.LoginUserId.ToString(),
                        IsPublic = false,
                        Sort = MylistDefaultSort.Latest
                    };
                    _UserMylists.Add(Deflist);
                    await Deflist.Refresh();
                }


                // ユーザーのマイリストグループの一覧を取得
                List<LoginUserMylistGroup> mylistGroupDataLists = null;
                try
                {
                    mylistGroupDataLists = await HohoemaApp.ContentProvider.GetLoginUserMylistGroups();
                }
                catch
                {
                    Debug.WriteLine("ユーザーマイリストの更新に失敗しました。");
                }

                if (mylistGroupDataLists == null)
                {
                    return;
                }

                // 追加分だけを検出してUserMylistに追加
                var addedMylistGroups = mylistGroupDataLists
                    .Where(x => _UserMylists.All(y => x.Id != y.GroupId))
                    .ToArray();

                foreach (var userMylist in addedMylistGroups)
                {
                    var addedMylistGroupInfo = MylistGroupInfo.FromMylistGroupData(userMylist, HohoemaApp, this);
                    _UserMylists.Add(addedMylistGroupInfo);
                    await addedMylistGroupInfo.Refresh();

                    await Task.Delay(500);
                }

                // 削除分だけ検出してUserMylistから削除
                var removedMylistGroups = _UserMylists
                    .Where(x => !x.IsDeflist)
                    .Where(x => mylistGroupDataLists.All(y => x.GroupId != y.Id))
                    .ToArray();

                foreach (var removeMylistGroup in removedMylistGroups)
                {
                    _UserMylists.Remove(removeMylistGroup);
                }
            }
		}

		public async Task<ContentManageResult> AddMylist(string name, string description, bool is_public, MylistDefaultSort default_sort, IconType iconType)
		{
			var result = await HohoemaApp.NiconicoContext.User.CreateMylistGroupAsync(name, description, is_public, default_sort, iconType);

			if (result == ContentManageResult.Success)
			{
				await SyncMylistGroups();
			}

			return result;
		}

		
		public async Task<ContentManageResult> RemoveMylist(string group_id)
		{
			var result = await HohoemaApp.NiconicoContext.User.RemoveMylistGroupAsync(group_id);

			if (result == ContentManageResult.Success)
			{
				await SyncMylistGroups();
			}

			return result;
		}


		public bool IsDeflistCapacityReached
		{
			get
			{
				return DeflistRegistrationCount >= DeflistRegistrationCapacity;
			}
		}

		public bool CanAddMylistItem
		{
			get
			{
				return MylistRegistrationCount < MylistRegistrationCapacity;
			}
		}


		public bool CheckIsRegistratedAnyMylist(string videoId)
		{
			return UserMylists.Any(x => x.CheckRegistratedVideoId(videoId));
		}



		internal void DeflistUpdated()
		{
//			RaisePropertyChanged(nameof(IsDeflistCapacityReached));
		}

		internal void MylistUpdated()
		{
//			RaisePropertyChanged(nameof(CanAddMylistItem));
		}

        protected override Task OnInitializeAsync(CancellationToken cancelToken)
        {
            return Windows.System.Threading.ThreadPool.RunAsync(async (x) => 
            {
                await SyncMylistGroups();
            }
            , Windows.System.Threading.WorkItemPriority.Low)
            .AsTask(cancelToken);
        }
    }

	public class MylistVideoItemInfo
	{
		public string VideoId { get; set; }
		public string ThreadId { get; set; }
	}

	public class MylistGroupInfo : IPlayableList
    {
		public HohoemaApp HohoemaApp { get; private set; }
		public UserMylistManager MylistManager { get; private set; }


        public PlaylistOrigin Origin => PlaylistOrigin.LoginUser;
        public string Id => GroupId;
        public int SortIndex => 0;

		public string GroupId { get; private set; }
		public string UserId { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public bool IsPublic { get; set; }
		public IconType IconType { get; set; }
		public MylistDefaultSort Sort { get; set; }
        public int Count { get; set; }
        public int RegistrationLimit { get; set; }

        public MylistGroupInfo(string groupId, HohoemaApp hohoemaApp, UserMylistManager mylistManager)
		{
			GroupId = groupId;
			IsDeflist = GroupId == "0";
			HohoemaApp = hohoemaApp;
			MylistManager = mylistManager;
            _PlaylistItems = new ObservableCollection<PlaylistItem>();
            PlaylistItems = new ReadOnlyObservableCollection<PlaylistItem>(_PlaylistItems);
            _VideoIdToThreadIdMap = new Dictionary<string, string>();
		}

		

		public Windows.UI.Color ThemeColor
		{
			get
			{
				return IconType.ToColor();
			}
		}

		private Dictionary<string, string> _VideoIdToThreadIdMap;


		private ObservableCollection<PlaylistItem> _PlaylistItems;
		public ReadOnlyObservableCollection<PlaylistItem> PlaylistItems { get; }
		

		public int ItemCount
		{
			get
			{
                return PlaylistItems.Count != 0 ? PlaylistItems.Count : Count;
			}
		}

		public bool IsDeflist { get; private set; }


		/// <summary>
		/// [非推奨] 基本的にBackgroundUpdaterから更新されます
		/// </summary>
		/// <returns></returns>
		public Task Refresh()
		{
			return Update();
		}

		public async Task<ContentManageResult> UpdateMylist(string name, string description, bool is_public, MylistDefaultSort default_sort, IconType iconType)
		{
			if (this.GroupId == "0")
			{
				throw new Exception();
			}

			var sortChanged = default_sort != Sort;

			var result = await HohoemaApp.NiconicoContext.User.UpdateMylistGroupAsync(GroupId, name, description, is_public, default_sort, iconType);

			if (result == ContentManageResult.Success)
			{ 
				Description = description;
				Name = name;
				IsPublic = is_public;
				IconType = iconType;
				Sort = default_sort;


				if (sortChanged)
				{
					await Refresh();
				}
			}

			return result;
		}

		public bool CanMoreRegistration()
		{
			return IsDeflist ? true : MylistManager.CanAddMylistItem;
		}

		public async Task<ContentManageResult> Registration(string videoId, string mylistComment = "", bool withRefresh = true)
		{
			var result = await HohoemaApp.NiconicoContext.User.AddMylistItemAsync(
				GroupId
				, Mntone.Nico2.NiconicoItemType.Video
				, videoId
				, mylistComment
				);

			if (withRefresh && result == ContentManageResult.Success)
			{
				await Refresh();
			}

			return result;
		}

		public async Task<ContentManageResult> Unregistration(string video_id, bool withRefresh = true)
		{
			if (!_VideoIdToThreadIdMap.ContainsKey(video_id))
			{
				throw new Exception();
			}

			var threadId = _VideoIdToThreadIdMap[video_id];
			var result = await HohoemaApp.NiconicoContext.User.RemoveMylistItemAsync(GroupId, NiconicoItemType.Video, threadId);

			if (withRefresh && result == ContentManageResult.Success)
			{
				await Refresh();
			}

			return result;
		}

		public async Task<ContentManageResult> CopyMylistTo(MylistGroupInfo targetGroupInfo, params string[] videoIdList)
		{
			var threadIdList = videoIdList.Select(x =>
			{
				return _VideoIdToThreadIdMap[x];
			})
			.ToArray();

			var result = await HohoemaApp.NiconicoContext.User.CopyMylistItemAsync(this.GroupId, targetGroupInfo.GroupId, NiconicoItemType.Video, threadIdList);

			if (result == ContentManageResult.Success)
			{
				await targetGroupInfo.Refresh();
			}

			return result;
		}


		public async Task<ContentManageResult> MoveMylistTo(MylistGroupInfo targetGroupInfo, params string[] videoIdList)
		{
			var threadIdList = videoIdList.Select(x =>
			{
				return _VideoIdToThreadIdMap[x];
			})
			.ToArray();

			var result = await HohoemaApp.NiconicoContext.User.MoveMylistItemAsync(this.GroupId, targetGroupInfo.GroupId, NiconicoItemType.Video, threadIdList);

			if (result == ContentManageResult.Success)
			{
				await this.Refresh();
				await targetGroupInfo.Refresh();
			}

			return result;
		}


		public static MylistGroupInfo FromMylistGroupData(LoginUserMylistGroup group, HohoemaApp hohoemaApp, UserMylistManager mylistManager)
		{
			return new MylistGroupInfo(group.Id, hohoemaApp, mylistManager)
			{
				UserId = group.UserId,
				Name = group.Name,
				Description = group.Description,
				IsPublic = group.GetIsPublic(),
				IconType = group.GetIconType(),
				Sort = group.GetDefaultSort(),
                Count = group.ItemCount
                
			};
		}


		public bool CheckRegistratedVideoId(string videoId)
		{
			return _VideoIdToThreadIdMap.ContainsKey(videoId);
		}

		private async Task Update()
		{

			var itemCountPerMylist = HohoemaApp.IsPremiumUser ? 500u : 100u;

			_VideoIdToThreadIdMap.Clear();
			_PlaylistItems.Clear();

			if (IsDeflist)
			{
				var defMylist = await HohoemaApp.NiconicoContext.User.GetMylistItemListAsync("0");

				foreach (var item in defMylist)
				{
					_VideoIdToThreadIdMap.Add(item.WatchId, item.ItemId);
					_PlaylistItems.Add(new PlaylistItem()
                    {
                        ContentId = item.WatchId,
                        Owner = this,
                        Title = item.Title,
                        Type = PlaylistItemType.Video
                    }
                    );
				}

				MylistManager.DeflistUpdated();
			}
			else
			{
				if (UserId == HohoemaApp.LoginUserId.ToString())
				{
					var res = await HohoemaApp.NiconicoContext.User.GetMylistItemListAsync(GroupId);

					foreach (var item in res)
					{
						_VideoIdToThreadIdMap.Add(item.WatchId, item.ItemId);
						_PlaylistItems.Add(new PlaylistItem()
                        {
                            ContentId = item.WatchId,
                            Owner = this,
                            Title = item.Title,
                            Type = PlaylistItemType.Video
                        });
					}

					MylistManager.MylistUpdated();
				}
				else
				{
					var res = await HohoemaApp.ContentProvider.GetMylistGroupVideo(GroupId, 0, itemCountPerMylist);


					if (res.GetCount() > 0)
					{
						foreach (var item in res.MylistVideoInfoItems)
						{
							_VideoIdToThreadIdMap.Add(item.Video.Id, item.Thread.Id);
							_PlaylistItems.Add(new PlaylistItem()
                            {
                                ContentId = item.Video.Id,
                                Owner = this,
                                Title = item.Video.Title,
                                Type = PlaylistItemType.Video
                            });
						}
					}
				}
			}

			await Task.Delay(100);
		}


		
		private static void SortMylistData(ref List<MylistData> mylist, MylistDefaultSort sort)
		{
			switch (sort)
			{
				case MylistDefaultSort.Old:
					mylist.Sort((x, y) => DateTime.Compare(x.UpdateTime, y.UpdateTime));
					break;
				case MylistDefaultSort.Latest:
					mylist.Sort((x, y) => -DateTime.Compare(x.UpdateTime, y.UpdateTime));
					break;
				case MylistDefaultSort.Memo_Ascending:
					mylist.Sort((x, y) => string.Compare(x.Description, y.Description));
					break;
				case MylistDefaultSort.Memo_Descending:
					mylist.Sort((x, y) => -string.Compare(x.Description, y.Description));
					break;
				case MylistDefaultSort.Title_Ascending:
					mylist.Sort((x, y) => string.Compare(x.Title, y.Title));
					break;
				case MylistDefaultSort.Title_Descending:
					mylist.Sort((x, y) => -string.Compare(x.Title, y.Title));
					break;
				case MylistDefaultSort.FirstRetrieve_Ascending:
					mylist.Sort((x, y) => DateTime.Compare(x.FirstRetrieve, y.FirstRetrieve));
					break;
				case MylistDefaultSort.FirstRetrieve_Descending:
					mylist.Sort((x, y) => - DateTime.Compare(x.FirstRetrieve, y.FirstRetrieve));
					break;
				case MylistDefaultSort.View_Ascending:
					mylist.Sort((x, y) => (int)(x.ViewCount - y.ViewCount));
					break;
				case MylistDefaultSort.View_Descending:
					mylist.Sort((x, y) => -(int)(x.ViewCount - y.ViewCount));
					break;
				case MylistDefaultSort.Comment_New:
					// Note: コメント順は非対応
					mylist.Sort((x, y) => (int)(x.CommentCount - y.CommentCount));
					break;
				case MylistDefaultSort.Comment_Old:
					// Note: コメント順は非対応
					mylist.Sort((x, y) => -(int)(x.CommentCount - y.CommentCount));
					break;
				case MylistDefaultSort.CommentCount_Ascending:
					mylist.Sort((x, y) => (int)(x.CommentCount - y.CommentCount));
					break;
				case MylistDefaultSort.CommentCount_Descending:
					mylist.Sort((x, y) => -(int)(x.CommentCount - y.CommentCount));
					break;
				case MylistDefaultSort.MylistCount_Ascending:
					mylist.Sort((x, y) => (int)(x.MylistCount - y.MylistCount));
					break;
				case MylistDefaultSort.MylistCount_Descending:
					mylist.Sort((x, y) => -(int)(x.MylistCount - y.MylistCount));
					break;
				case MylistDefaultSort.Length_Ascending:
					mylist.Sort((x, y) => TimeSpan.Compare(x.Length, y.Length));
					break;
				case MylistDefaultSort.Length_Descending:
					mylist.Sort((x, y) => -TimeSpan.Compare(x.Length, y.Length));
					break;
				default:
					break;
			}
		}
	}
}
