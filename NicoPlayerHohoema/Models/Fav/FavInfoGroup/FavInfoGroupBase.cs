using Mntone.Nico2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	abstract public class FavInfoGroupBase : IFavInfoGroup
	{
		#region Fields

		protected ObservableCollection<FavInfo> _FavInfoList;



		#endregion

		public ReadOnlyObservableCollection<FavInfo> FavInfoItems { get; private set; }

		public HohoemaApp HohoemaApp { get; private set; }


		public FavInfoGroupBase(HohoemaApp hohoemaApp, List<FavInfo> items)
		{
			HohoemaApp = hohoemaApp;

			_FavInfoList = new ObservableCollection<FavInfo>(items);
			FavInfoItems = new ReadOnlyObservableCollection<FavInfo>(_FavInfoList);
		}



		abstract public FavoriteItemType FavoriteItemType { get; }
		abstract public bool CanMoreAddFavorite();


		abstract protected Task<ContentManageResult> AddFav_Internal(string id);
		abstract protected Task<ContentManageResult> RemoveFav_Internal(string id);


		abstract public Task Sync();


		public bool IsFavoriteItem(string id)
		{
			return _FavInfoList.Any(x => x.Id == id);
		}

		public async Task<ContentManageResult> AddFav(string name, string id)
		{
			var result = await AddFav_Internal(id);

			if (result == ContentManageResult.Success)
			{
				var newList = new FavInfo()
				{
					Name = name,
					Id = id,
					FavoriteItemType = FavoriteItemType,
				};

				_FavInfoList.Add(newList);
			}

			return result;
		}

		public async Task<ContentManageResult> RemoveFav(string id)
		{
			var result = await RemoveFav_Internal(id);

			if (result == ContentManageResult.Success)
			{
				var removeTarget = _FavInfoList.SingleOrDefault(x => x.Id == id);
				_FavInfoList.Remove(removeTarget);
			}

			return result;
		}

	}
}
