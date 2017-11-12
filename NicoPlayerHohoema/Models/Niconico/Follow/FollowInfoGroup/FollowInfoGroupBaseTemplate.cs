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
	public abstract class FollowInfoGroupBaseTemplate<FOLLOW_SOURCE> : FollowInfoGroupBase
	{
		public FollowInfoGroupBaseTemplate(HohoemaApp hohoemaApp) 
			: base(hohoemaApp)
		{

		}

		protected abstract Task<List<FOLLOW_SOURCE>> GetFollowSource();
		protected abstract string FollowSourceToItemId(FOLLOW_SOURCE source);
		protected abstract FollowItemInfo ConvertToFollowInfo(FOLLOW_SOURCE source);


		


		protected override async Task SyncFollowItems_Internal()
		{
            var userFavDatas = await GetFollowSource();

            if (userFavDatas == null) { return; }

            // まだローカルデータとして登録されていないIDを追加分として抽出
            var addedItems = userFavDatas
                .Where(x =>
                {
                    var itemId = FollowSourceToItemId(x);
                    return _FollowInfoList.All(y => y.Id != itemId);
                })
                .Select(ConvertToFollowInfo);

            foreach (var addItem in addedItems)
            {
                _FollowInfoList.Add(addItem);
            }


            // オンラインデータから削除されているアイテムを抽出
            var itemIds = userFavDatas.Select(FollowSourceToItemId).ToArray();
            var removedItems = _FollowInfoList
                .Where(x => !itemIds.Any(y => x.Id == y))
                .ToList();
            foreach (var removeItem in removedItems)
            {
                _FollowInfoList.Remove(removeItem);
            }
		}


		

	}
}
