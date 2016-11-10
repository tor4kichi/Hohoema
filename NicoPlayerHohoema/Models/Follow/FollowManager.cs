using Mntone.Nico2;
using Mntone.Nico2.Users.Follow;
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
using Windows.UI.Core;

namespace NicoPlayerHohoema.Models
{
	public class FollowManager : IBackgroundUpdateable
	{

		#region Niconico follow constants

		// Note: 2016/10/31 から お気に入りユーザー枠は一般プレミアムどちらも600に変更
		public const uint FOLLOW_USER_MAX_COUNT = 600;
		public const uint PREMIUM_FOLLOW_USER_MAX_COUNT = 600;

		public const uint FOLLOW_MYLIST_MAX_COUNT = 20;
		public const uint PREMIUM_FOLLOW_MYLIST_MAX_COUNT = 50;

		public const uint FOLLOW_TAG_MAX_COUNT = 10;
		public const uint PREMIUM_FOLLOW_TAG_MAX_COUNT = 10;

		public const uint FOLLOW_COMMUNITY_MAX_COUNT = 50;
		public const uint PREMIUM_FOLLOW_COMMUNITY_MAX_COUNT = 300;

		#endregion


		public static Task<FollowManager> Create(HohoemaApp hohoemaApp, uint userId)
		{
			var followManager = new FollowManager(hohoemaApp, userId);

			var updater = hohoemaApp.BackgroundUpdater.CreateBackgroundUpdateInfoWithImmidiateSchedule(followManager, "followmanager_" + userId);

			return Task.FromResult(followManager);
		}


		#region Properties 

		public uint UserId { get; set; }


		public IFollowInfoGroup Tag { get; private set; }
		public IFollowInfoGroup Mylist { get; private set; }
		public IFollowInfoGroup User { get; private set; }
		public IFollowInfoGroup Community { get; private set; }


		public IReadOnlyList<IFollowInfoGroup> GetAllFollowInfoGroups() => new[] 
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

		internal FollowManager(HohoemaApp hohoemaApp, uint userId)
		{
			_HohoemaApp = hohoemaApp;
			UserId = userId;
		}


		#region interface IBackgroundUpdateable

		public IAsyncAction BackgroundUpdate(CoreDispatcher uiDispatcher)
		{
			return Initialize()
				.AsAsyncAction();
		}

		#endregion


		private async Task Initialize()
		{
			Tag = new TagFollowInfoGroup(_HohoemaApp);
			Mylist = new MylistFollowInfoGroup(_HohoemaApp);
			User = new UserFollowInfoGroup(_HohoemaApp);
			Community = new CommunityFollowInfoGroup(_HohoemaApp);

			await SyncAll();
		}

		private IFollowInfoGroup GetFollowInfoGroup(FollowItemType itemType)
		{
			switch (itemType)
			{
				case FollowItemType.Tag:
					return Tag;
				case FollowItemType.Mylist:
					return Mylist;
				case FollowItemType.User:
					return User;
				case FollowItemType.Community:
					return Community;
				default:
					throw new Exception();
			}
		}

		public bool CanMoreAddFollow(FollowItemType itemType)
		{
			return GetFollowInfoGroup(itemType).CanMoreAddFollow();
		}

		

		public bool IsFollowItem(FollowItemType itemType, string id)
		{
			var group = GetFollowInfoGroup(itemType);

			if (itemType == FollowItemType.Tag)
			{
				id = TagStringHelper.ToEnsureHankakuNumberTagString(id);
			}

			return group.IsFollowItem(id);
		}


	

		public async Task SyncAll()
		{
			foreach (var followInfoGroup in GetAllFollowInfoGroups())
			{
				await Sync(followInfoGroup);

				await Task.Delay(1000);
			}
		}


		public Task Sync(FollowItemType itemType)
		{
			switch (itemType)
			{
				case FollowItemType.Tag:
					return Sync(Tag);
				case FollowItemType.Mylist:
					return Sync(Mylist);
				case FollowItemType.User:
					return Sync(User);
				case FollowItemType.Community:
					return Sync(Community);
				default:
					return Task.CompletedTask;
			}
		}


		private async Task Sync(IFollowInfoGroup group)
		{
			await group.Sync();
		}

		



		public FollowItemInfo FindFollowInfo(FollowItemType itemType, string id)
		{
			return GetFollowInfoGroup(itemType).FollowInfoItems.SingleOrDefault(x => x.Id == id);
		}

		public async Task<ContentManageResult> AddFollow(FollowItemType itemType, string id, string name)
		{
			var group = GetFollowInfoGroup(itemType);

			var result = await group.AddFollow(name, id);
		
			return result;
		}

		public async Task<ContentManageResult> RemoveFollow(FollowItemType itemType, string id)
		{
			var group = GetFollowInfoGroup(itemType);

			var result = await group.RemoveFollow(id);

			return result;
		}

	}

}
