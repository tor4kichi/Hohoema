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
			throw new NotImplementedException();
		}


		public async Task UpdateUserMylists()
		{
			// ユーザーのマイリストグループの一覧を取得
			var mylistGroupDataLists = await HohoemaApp.ContentFinder.GetUserMylistGroups(HohoemaApp.LoginUserId.ToString());

			_UserMylists = mylistGroupDataLists.Select(x => MylistGroupInfo.FromMylistGroupData(x)).ToList();


			// マイリストの最大登録件数はプレミアムでフォルダごと500、通常ユーザーだとフォルダ関係なく最大100まで
			_MylistGroupToItems.Clear();

			var itemCountPerMylist = HohoemaApp.IsPremiumUser ? 500u : 100u;

			foreach (var group in _UserMylists)
			{
				await Task.Delay(250);
				
				var res = await HohoemaApp.ContentFinder.GetMylistItems(group.GroupId, 0, itemCountPerMylist);

				_MylistGroupToItems.Add(group.GroupId, res.Video_info.Select(x => x.Video.Id).ToList());
			}


			_VideoIdToMylists.Clear();

			foreach (var group in UserMylists)
			{
				foreach (var videoId in _MylistGroupToItems[group.GroupId])
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

		public async Task<ContentManageResult> UpdateMylist(string group_id, string name, string description, bool is_public, MylistDefaultSort default_sort, IconType iconType)
		{
			var result = await HohoemaApp.NiconicoContext.Mylist.UpdateMylistGroupAsync(group_id, name, description, is_public, default_sort, iconType);

			if (result == ContentManageResult.Success)
			{
				var mylist = _UserMylists.SingleOrDefault(x => x.GroupId == group_id);

				if (mylist == null)
				{
					throw new Exception("");
				}

				mylist.Description = description;
				mylist.Name = name;
				mylist.IsPublic = is_public;
				mylist.IconType = iconType;
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


		private void AddVideo(string videoId, MylistGroupInfo group)
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

			_MylistGroupToItems[group.GroupId].Add(videoId);
		}

		private void RemoveVideo(string videoId, MylistGroupInfo group)
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


		public async Task<ContentManageResult> Registration(string group_id, string video_id, string mylistComment = "")
		{
			var result = await HohoemaApp.NiconicoContext.Mylist.AddMylistItemAsync(group_id, Mntone.Nico2.NiconicoItemType.Mylist, video_id, mylistComment);

			if (result == ContentManageResult.Success)
			{
				var group = UserMylists.Single(x => x.GroupId == group_id);

				// ビデオIDからマイリストグループを求めるマップに追加
				AddVideo(video_id, group);
			}

			return result;
		}

		public async Task<ContentManageResult> Unregistration(string group_id, string video_id)
		{
			var result = await HohoemaApp.NiconicoContext.Mylist.RemoveMylistItemAsync(group_id, NiconicoItemType.Mylist, video_id);

			if (result == ContentManageResult.Success)
			{
				var group = UserMylists.Single(x => x.GroupId == group_id);

				RemoveVideo(video_id, group);
			}

			return result;
		}
	}


	public class MylistGroupInfo
	{
		public string GroupId { get; set; }
		public string UserId { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public bool IsPublic { get; set; }
		public IconType IconType { get; set; }



		public static MylistGroupInfo FromMylistGroupData(MylistGroupData group)
		{
			return new MylistGroupInfo()
			{
				GroupId = group.Id,
				UserId = group.UserId,
				Name = group.Name,
				Description = group.Description,
				IsPublic = group.GetIsPublic(),
				IconType = group.GetIconType()
			};

		}
	}
}
