using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	public class UserMylistManager
	{
		public HohoemaApp HohoemaApp { get; private set; }


		private List<MylistGroupInfo> _UserMylists;
		public IReadOnlyList<MylistGroupInfo> UserMylists
		{
			get
			{
				return _UserMylists;
			}

		}

		private Dictionary<string, List<string>> _MylistGroupToItems;
		public IReadOnlyDictionary<string, List<string>> MylistGroupToItems
		{
			get
			{
				return _MylistGroupToItems;
			}
		}

		private Dictionary<string, List<MylistGroupInfo>> _VideoIdToMylists;


		public UserMylistManager(HohoemaApp app)
		{
			HohoemaApp = app;

			_UserMylists = new List<MylistGroupInfo>();
			_MylistGroupToItems = new Dictionary<string, List<string>>();
			_VideoIdToMylists = new Dictionary<string, List<MylistGroupInfo>>();

			app.OnSignin += App_OnSignin;
			app.OnSignout += App_OnSignout;
		}

		private void App_OnSignout()
		{
			
		}

		private void App_OnSignin()
		{

		}


		public bool HasMylistGroup(string groupId)
		{
			return UserMylists.Any(x => x.GroupId == groupId);
		}


		public async Task UpdateUserMylists()
		{
			// ユーザーのマイリストグループの一覧を取得
			var mylistGroupDataLists = await HohoemaApp.ContentFinder.GetUserMylistGroups(HohoemaApp.LoginUserId.ToString());

			_UserMylists = mylistGroupDataLists.Select(x => MylistGroupInfo.FromMylistGroupData(x, HohoemaApp, this)).ToList();

			


			// マイリストの最大登録件数はプレミアムでフォルダごと500、通常ユーザーだとフォルダ関係なく最大100まで
			_MylistGroupToItems.Clear();

			var itemCountPerMylist = HohoemaApp.IsPremiumUser ? 500u : 100u;

			foreach (var group in _UserMylists)
			{
				await Task.Delay(250);
				
				var res = await HohoemaApp.ContentFinder.GetMylistItems(group.GroupId, 0, itemCountPerMylist);

				_MylistGroupToItems.Add(group.GroupId, res.Video_info.Select(x => x.Video.Id).ToList());
			}



			// とりあえずマイリストを手動で追加
			_UserMylists.Add(new MylistGroupInfo("0", HohoemaApp, this)
			{
				Name = "とりあえずマイリスト",
				Description = "ユーザーの一時的なマイリストです",
				UserId = HohoemaApp.LoginUserId.ToString(),
				IsPublic = false
			});

			var defMylist = await HohoemaApp.NiconicoContext.Mylist.GetMylistItemListAsync("0");

			_MylistGroupToItems.Add("0", defMylist.Select(x => x.ItemId).ToList());


			_VideoIdToMylists.Clear();

			foreach (var group in UserMylists)
			{
				foreach (var videoId in _MylistGroupToItems[group.GroupId].ToList())
				{					
					AddVideo(videoId, group);
				}
			}
		}


		public async Task<ContentManageResult> AddUpdateMylist(string name, string description, bool is_public, MylistDefaultSort default_sort, IconType iconType)
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
				// _UserMylists
				var removeTargetMylist = _UserMylists.SingleOrDefault(x => x.GroupId == group_id);

				if (removeTargetMylist == null)
				{
					return result;
				}

				// _VideoIdToMylists
				var videos = MylistGroupToItems[group_id];
				foreach (var videoId in videos)
				{
					if (_VideoIdToMylists[videoId].Remove(removeTargetMylist))
					{
						if (_VideoIdToMylists[videoId].Count == 0)
						{
							_VideoIdToMylists.Remove(videoId);
						}
					}
				}

				// _MylistGroupToItems
				_MylistGroupToItems.Remove(group_id);
			}

			return result;
		}



		internal void AddVideo(string videoId, MylistGroupInfo group, bool insertToTop = false)
		{
			if (!_VideoIdToMylists.ContainsKey(videoId))
			{
				_VideoIdToMylists.Add(videoId, new List<MylistGroupInfo>()
				{
					group
				});
			}
			else
			{
				_VideoIdToMylists[videoId].Add(group);
			}

			if (insertToTop)
			{
				_MylistGroupToItems[group.GroupId].Insert(0, videoId);
			}
			else
			{
				_MylistGroupToItems[group.GroupId].Add(videoId);
			}
		}

		internal void RemoveVideo(string videoId, MylistGroupInfo group)
		{
			if (_VideoIdToMylists.ContainsKey(videoId))
			{
				if (_VideoIdToMylists[videoId].Remove(group))
				{
					if (_VideoIdToMylists[videoId].Count == 0)
					{
						_VideoIdToMylists.Remove(videoId);
					}
				}
			}

			_MylistGroupToItems[group.GroupId].Remove(videoId);
		}

		public IEnumerable<MylistGroupInfo> GetVideoRegistratedMylists(string video_id)
		{
			if (_VideoIdToMylists.ContainsKey(video_id))
			{
				return _VideoIdToMylists[video_id];
			}
			else
			{
				return Enumerable.Empty<MylistGroupInfo>();
			}
		}


	}


	public class MylistGroupInfo
	{
		public MylistGroupInfo(string groupId, HohoemaApp hohoemaApp, UserMylistManager mylistManager)
		{
			GroupId = groupId;
			IsDeflist = GroupId == "0";
			HohoemaApp = hohoemaApp;
			MylistManager = mylistManager;
		}

		public HohoemaApp HohoemaApp { get; private set; }
		public UserMylistManager MylistManager { get; private set; }

		public string GroupId { get; private set; }
		public string UserId { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public bool IsPublic { get; set; }
		public IconType IconType { get; set; }


		public bool IsDeflist { get; private set; }

		public async Task<ContentManageResult> UpdateMylist(string group_id, string name, string description, bool is_public, MylistDefaultSort default_sort, IconType iconType)
		{
			if (this.GroupId == "0")
			{

			}

			var result = await HohoemaApp.NiconicoContext.Mylist.UpdateMylistGroupAsync(group_id, name, description, is_public, default_sort, iconType);

			if (result == ContentManageResult.Success)
			{ 
				Description = description;
				Name = name;
				IsPublic = is_public;
				IconType = iconType;
			}

			return result;
		}



		public async Task<ContentManageResult> Registration(string video_id, string mylistComment = "")
		{
			var result = await HohoemaApp.NiconicoContext.Mylist.AddMylistItemAsync(GroupId, Mntone.Nico2.NiconicoItemType.Video, video_id, mylistComment);

			if (result == ContentManageResult.Success)
			{
				// 新しく追加したアイテムが先頭に表示されるように追加
				MylistManager.AddVideo(video_id, this, insertToTop:true);
			}

			return result;
		}

		public async Task<ContentManageResult> Unregistration(string video_id)
		{
			var result = await HohoemaApp.NiconicoContext.Mylist.RemoveMylistItemAsync(GroupId, NiconicoItemType.Video, video_id);

			if (result == ContentManageResult.Success)
			{
				MylistManager.RemoveVideo(video_id, this);
			}

			return result;
		}

		public static MylistGroupInfo FromMylistGroupData(MylistGroupData group, HohoemaApp hohoemaApp, UserMylistManager mylistManager)
		{
			return new MylistGroupInfo(group.Id, hohoemaApp, mylistManager)
			{
				UserId = group.UserId,
				Name = group.Name,
				Description = group.Description,
				IsPublic = group.GetIsPublic(),
				IconType = group.GetIconType()
			};

		}


		public bool CheckRegistratedVideoId(string videoId)
		{
			return MylistManager.GetVideoRegistratedMylists(videoId).Any(x => x == this);
		}
	}
}
