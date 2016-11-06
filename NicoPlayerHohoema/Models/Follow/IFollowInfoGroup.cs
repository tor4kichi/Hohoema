using Mntone.Nico2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	public interface IFollowInfoGroup
	{
		ReadOnlyObservableCollection<FollowItemInfo> FollowInfoItems { get; }
		FollowItemType FollowItemType { get; }
		uint MaxFollowItemCount { get; }

		bool CanMoreAddFollow();
		bool IsFollowItem(string id);

		Task Sync();


		Task<ContentManageResult> AddFollow(string name, string id);
		Task<ContentManageResult> RemoveFollow(string id);
	}
}
