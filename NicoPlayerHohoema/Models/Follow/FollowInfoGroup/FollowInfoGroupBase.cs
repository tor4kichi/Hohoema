using Mntone.Nico2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	public abstract class FollowInfoGroupBase : IFollowInfoGroup
	{
		#region Fields

		protected ObservableCollection<FollowItemInfo> _FollowInfoList;



		#endregion

		public ReadOnlyObservableCollection<FollowItemInfo> FollowInfoItems { get; private set; }

		public HohoemaApp HohoemaApp { get; private set; }


		public FollowInfoGroupBase(HohoemaApp hohoemaApp)
		{
			HohoemaApp = hohoemaApp;

			_FollowInfoList = new ObservableCollection<FollowItemInfo>();
			FollowInfoItems = new ReadOnlyObservableCollection<FollowItemInfo>(_FollowInfoList);
		}



		public abstract FollowItemType FollowItemType { get; }
		public abstract uint MaxFollowItemCount { get; }

		public bool CanMoreAddFollow()
		{
			return _FollowInfoList.Count < MaxFollowItemCount;
		}


		protected abstract Task<ContentManageResult> AddFollow_Internal(string id, object token = null);
		protected abstract Task<ContentManageResult> RemoveFollow_Internal(string id, object token = null);


		public abstract Task Sync();


		public bool IsFollowItem(string id)
		{
			return _FollowInfoList.Any(x => x.Id == id);
		}

		public async Task<ContentManageResult> AddFollow(string name, string id, object token = null)
		{
			var result = await AddFollow_Internal(id, token);

			if (result == ContentManageResult.Success)
			{
				var newList = new FollowItemInfo()
				{
					Name = name,
					Id = id,
					FollowItemType = FollowItemType,
				};

				_FollowInfoList.Add(newList);
			}

			return result;
		}

		public async Task<ContentManageResult> RemoveFollow(string id, object token = null)
		{
			var result = await RemoveFollow_Internal(id);

			if (result == ContentManageResult.Success)
			{
				var removeTarget = _FollowInfoList.SingleOrDefault(x => x.Id == id);
				_FollowInfoList.Remove(removeTarget);
			}

			return result;
		}

	}
}
