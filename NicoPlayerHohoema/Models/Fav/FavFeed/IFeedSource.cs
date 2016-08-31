using System.Collections.Generic;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	public interface IFeedSource
	{
		FavoriteItemType FavoriteItemType { get; }
		string Id { get; }
		string Name { get; set; }

		Task<IEnumerable<FavFeedItem>> GetLatestItems(HohoemaApp hohoemaApp);
	}
}