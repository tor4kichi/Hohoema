using Mntone.Nico2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	public interface IFavInfoGroup
	{
		ReadOnlyObservableCollection<FavInfo> FavInfoItems { get; }
		FavoriteItemType FavoriteItemType { get; }

		bool CanMoreAddFavorite();
		bool IsFavoriteItem(string id);

		Task Sync();


		Task<ContentManageResult> AddFav(string name, string id);
		Task<ContentManageResult> RemoveFav(string id);
	}
}
