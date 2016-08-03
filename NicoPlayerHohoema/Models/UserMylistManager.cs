using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	public class UserMylistManager : BindableBase
	{
		public const int MaxUserMylistGroupCount = 25;


		public HohoemaApp HohoemaApp { get; private set; }

		public MylistGroupInfo Deflist { get; private set; }

		private ObservableCollection<MylistGroupInfo> _UserMylists;
		public ReadOnlyObservableCollection<MylistGroupInfo> UserMylists { get; private set; }

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
				return HohoemaApp.IsPremiumUser ? 12500 : 100;
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
			
		}

		private void App_OnSignin()
		{

		}


		public bool CanAddMylistGroup
		{
			get
			{
				return UserMylists.Count < MaxUserMylistGroupCount;
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


		public async Task UpdateUserMylists()
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
			var mylistGroupDataLists = await HohoemaApp.ContentFinder.GetLoginUserMylistGroups();


			// 追加分だけを検出してUserMylistに追加
			var addedMylistGroups = mylistGroupDataLists
				.Where(x => _UserMylists.All(y => x.Id != y.GroupId))
				.ToArray();

			foreach (var userMylist in addedMylistGroups)
			{
				var addedMylistGroupInfo = MylistGroupInfo.FromMylistGroupData(userMylist, HohoemaApp, this);
				_UserMylists.Add(addedMylistGroupInfo);

				await Task.Delay(250);

				await addedMylistGroupInfo.Refresh();
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


		public async Task<ContentManageResult> AddMylist(string name, string description, bool is_public, MylistDefaultSort default_sort, IconType iconType)
		{
			var result = await HohoemaApp.NiconicoContext.Mylist.CreateMylistGroupAsync(name, description, is_public, default_sort, iconType);

			if (result == ContentManageResult.Success)
			{
				await UpdateUserMylists();
			}

			return result;
		}

		
		public async Task<ContentManageResult> RemoveMylist(string group_id)
		{
			var result = await HohoemaApp.NiconicoContext.Mylist.RemoveMylistGroupAsync(group_id);

			if (result == ContentManageResult.Success)
			{
				await UpdateUserMylists();
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
			OnPropertyChanged(nameof(IsDeflistCapacityReached));
		}

		internal void MylistUpdated()
		{
			OnPropertyChanged(nameof(CanAddMylistItem));
		}
	}

	public class MylistVideoItemInfo
	{
		public string VideoId { get; set; }
		public string ThreadId { get; set; }
	}

	public class MylistGroupInfo
	{
		public MylistGroupInfo(string groupId, HohoemaApp hohoemaApp, UserMylistManager mylistManager)
		{
			GroupId = groupId;
			IsDeflist = GroupId == "0";
			HohoemaApp = hohoemaApp;
			MylistManager = mylistManager;
			_VideoIdToThreadIdMap = new Dictionary<string, string>();
		}

		public HohoemaApp HohoemaApp { get; private set; }
		public UserMylistManager MylistManager { get; private set; }

		public string GroupId { get; private set; }
		public string UserId { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public bool IsPublic { get; set; }
		public IconType IconType { get; set; }
		public MylistDefaultSort Sort { get; set; }

		public Windows.UI.Color ThemeColor
		{
			get
			{
				return IconType.ToColor();
			}
		}

		private Dictionary<string, string> _VideoIdToThreadIdMap;
		public IReadOnlyCollection<string> VideoItems
		{
			get
			{
				return _VideoIdToThreadIdMap.Keys;
			}
		}

		

		public int ItemCount
		{
			get
			{
				return VideoItems.Count;
			}
		}

		public bool IsDeflist { get; private set; }


		public async Task Refresh()
		{
			var itemCountPerMylist = HohoemaApp.IsPremiumUser ? 500u : 100u;

			_VideoIdToThreadIdMap.Clear();

			if (IsDeflist)
			{
				var defMylist = await HohoemaApp.NiconicoContext.Mylist.GetMylistItemListAsync("0");

				foreach (var item in defMylist)
				{
					_VideoIdToThreadIdMap.Add(item.WatchId, item.ItemId);
				}

				MylistManager.DeflistUpdated();
			}
			else
			{
				if (!IsPublic && UserId == HohoemaApp.LoginUserId.ToString())
				{
					var res = await HohoemaApp.NiconicoContext.Mylist.GetMylistItemListAsync(GroupId);

					foreach (var item in res)
					{
						_VideoIdToThreadIdMap.Add(item.WatchId, item.ItemId);
					}

					MylistManager.MylistUpdated();
				}
				else
				{
					var res = await HohoemaApp.ContentFinder.GetMylistItems(GroupId, 0, itemCountPerMylist);


					foreach (var item in res.Video_info)
					{
						_VideoIdToThreadIdMap.Add(item.Video.Id, item.Thread.Id);
					}
				}

				
			}


		}

		public async Task<ContentManageResult> UpdateMylist(string name, string description, bool is_public, MylistDefaultSort default_sort, IconType iconType)
		{
			if (this.GroupId == "0")
			{
				throw new Exception();
			}

			var result = await HohoemaApp.NiconicoContext.Mylist.UpdateMylistGroupAsync(GroupId, name, description, is_public, default_sort, iconType);

			if (result == ContentManageResult.Success)
			{ 
				Description = description;
				Name = name;
				IsPublic = is_public;
				IconType = iconType;
				Sort = default_sort;
			}

			return result;
		}

		public bool CanMoreRegistration()
		{
			return IsDeflist ? true : MylistManager.CanAddMylistItem;
		}


		public async Task<ContentManageResult> Registration(string videoId, string mylistComment = "", bool withRefresh = true)
		{
			var result = await HohoemaApp.NiconicoContext.Mylist.AddMylistItemAsync(
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
			var result = await HohoemaApp.NiconicoContext.Mylist.RemoveMylistItemAsync(GroupId, NiconicoItemType.Video, threadId);

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

			var result = await HohoemaApp.NiconicoContext.Mylist.CopyMylistItemAsync(this.GroupId, targetGroupInfo.GroupId, NiconicoItemType.Video, threadIdList);

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

			var result = await HohoemaApp.NiconicoContext.Mylist.MoveMylistItemAsync(this.GroupId, targetGroupInfo.GroupId, NiconicoItemType.Video, threadIdList);

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
				Sort = group.GetDefaultSort()
			};

		}


		public bool CheckRegistratedVideoId(string videoId)
		{
			return _VideoIdToThreadIdMap.ContainsKey(videoId);
		}
	}
}
