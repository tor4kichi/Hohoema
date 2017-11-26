using Mntone.Nico2;
using NicoPlayerHohoema.Helpers;
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

        protected AsyncLock UpdateLock { get; } = new AsyncLock();



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

        public bool IsFailedUpdate { get; private set; }

        public bool CanMoreAddFollow()
		{
			return _FollowInfoList.Count < MaxFollowItemCount;
		}


		protected abstract Task<ContentManageResult> AddFollow_Internal(string id, object token = null);
		protected abstract Task<ContentManageResult> RemoveFollow_Internal(string id);


		protected abstract Task SyncFollowItems_Internal();

        public async Task SyncFollowItems()
        {
            using (var releaser = await UpdateLock.LockAsync())
            {
                try
                {
                    IsFailedUpdate = false;

                    await SyncFollowItems_Internal();
                }
                catch
                {
                    IsFailedUpdate = true;
                }
            }
        }


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

		public async Task<ContentManageResult> RemoveFollow(string id)
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
