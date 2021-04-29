using Hohoema.Models.Domain.Niconico.Follow;
using Mntone.Nico2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.Follow.LoginUser
{
	public interface IFollowInfoGroup
	{
		ReadOnlyObservableCollection<FollowItemInfo> FollowInfoItems { get; }

        bool IsFailedUpdate { get; }

		FollowItemType FollowItemType { get; }
		uint MaxFollowItemCount { get; }

		bool CanMoreAddFollow();
		bool IsFollowItem(string id);

		Task SyncFollowItems();


		Task<ContentManageResult> AddFollow(string name, string id, object token = null);

		/// <summary>
		/// フォローを解除します
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		Task<ContentManageResult> RemoveFollow(string id);
	}
}
