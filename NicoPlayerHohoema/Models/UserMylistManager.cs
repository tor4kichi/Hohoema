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


		public UserMylistManager(HohoemaApp app)
		{
			HohoemaApp = app;

			_UserMylists = new List<MylistGroupInfo>();

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


		public MylistGroupInfo GetMylistGroup(string groupId)
		{
			return UserMylists.SingleOrDefault(x => x.GroupId == groupId);
		}


		public async Task UpdateUserMylists()
		{
			_UserMylists = new List<MylistGroupInfo>();
			// とりあえずマイリストを手動で追加
			_UserMylists.Add(new MylistGroupInfo("0", HohoemaApp, this)
			{
				Name = "とりあえずマイリスト",
				Description = "ユーザーの一時的なマイリストです",
				UserId = HohoemaApp.LoginUserId.ToString(),
				IsPublic = false
			});

			// ユーザーのマイリストグループの一覧を取得
			var mylistGroupDataLists = await HohoemaApp.ContentFinder.GetLoginUserMylistGroups();


			var userMylists = mylistGroupDataLists.Select(x => MylistGroupInfo.FromMylistGroupData(x, HohoemaApp, this));

			_UserMylists.AddRange(userMylists);


			// マイリストの最大登録件数はプレミアムでフォルダごと500、通常ユーザーだとフォルダ関係なく最大100まで


			foreach (var group in _UserMylists)
			{
				await Task.Delay(250);

				await group.Refresh();
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

			}

			return result;
		}
		



		public bool CheckIsRegistratedAnyMylist(string videoId)
		{
			return UserMylists.Any(x => x.CheckRegistratedVideoId(videoId));
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
		}

		public HohoemaApp HohoemaApp { get; private set; }
		public UserMylistManager MylistManager { get; private set; }

		public string GroupId { get; private set; }
		public string UserId { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public bool IsPublic { get; set; }
		public IconType IconType { get; set; }

		private List<MylistVideoItemInfo> _VideoItems;
		public IReadOnlyList<MylistVideoItemInfo> VideoItems
		{
			get
			{
				return _VideoItems;
			}
		}

		public bool IsDeflist { get; private set; }


		public async Task Refresh()
		{
			var itemCountPerMylist = HohoemaApp.IsPremiumUser ? 500u : 100u;

			if (IsDeflist)
			{
				var defMylist = await HohoemaApp.NiconicoContext.Mylist.GetMylistItemListAsync("0");

				_VideoItems = defMylist.Select(x =>
				{
					return new MylistVideoItemInfo()
					{
						VideoId = x.WatchId,
						ThreadId = x.ItemId
					};
				}).ToList();
			}
			else
			{
				if (!IsPublic && UserId == HohoemaApp.LoginUserId.ToString())
				{
					var res = await HohoemaApp.NiconicoContext.Mylist.GetMylistItemListAsync(GroupId);

					_VideoItems = res.Select(x =>
					{
						return new MylistVideoItemInfo()
						{
							VideoId = x.WatchId,
							ThreadId = x.ItemId
						};
					}).ToList();
				}
				else
				{
					var res = await HohoemaApp.ContentFinder.GetMylistItems(GroupId, 0, itemCountPerMylist);

					_VideoItems = res.Video_info.Select(x =>
					{
						return new MylistVideoItemInfo()
						{
							VideoId = x.Video.Id,
							ThreadId = x.Thread.Id
						};
					}).ToList();
				}

				
			}


		}

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
				// 新しく追加したアイテムが先頭に表示されるように追加
				await Refresh();
			}

			return result;
		}

		public async Task<ContentManageResult> Unregistration(string video_id, bool withRefresh = true)
		{
			var item = VideoItems.SingleOrDefault(x => x.VideoId == video_id);
			if (item == null)
			{
				throw new Exception();
			}

			var result = await HohoemaApp.NiconicoContext.Mylist.RemoveMylistItemAsync(GroupId, NiconicoItemType.Video, item.ThreadId);

			if (withRefresh && result == ContentManageResult.Success)
			{
				await Refresh();
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
			return VideoItems.Any(x => x.VideoId == videoId);
		}
	}
}
