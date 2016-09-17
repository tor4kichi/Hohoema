using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.AppMap
{
	public class SearchAppMapContainer : SelfGenerateAppMapContainerBase
	{
		public SearchAppMapContainer() : base(HohoemaPageType.Search, label:"検索")
		{
			
		}

		protected override Task<IEnumerable<IAppMapItem>> GenerateItems(int count)
		{
			var histories = Db.SearchHistoryDb.GetHistoryItems();

			return Task.FromResult(histories.Take(count).Select(MakeAppMapItemFromSearchHistory));
		}


		private IAppMapItem MakeAppMapItemFromSearchHistory(Db.SearchHistory history)
		{
			return new SearchHistoryAppMapItem(history);
		}
	}

	public class SearchHistoryAppMapItem : IAppMapItem
	{
		public string PrimaryLabel { get; private set; }
		public string SecondaryLabel { get; private set; }
		public string Parameter { get; private set; }

		public HohoemaPageType PageType => HohoemaPageType.Search;


		public SearchHistoryAppMapItem(Db.SearchHistory history)
		{
			PrimaryLabel = history.Keyword;
			SecondaryLabel = history.Target.ToString();
			Parameter = new SearchOption()
			{
				Keyword = history.Keyword,
				SearchTarget = history.Target,
				Sort = Mntone.Nico2.Sort.FirstRetrieve,
				Order = Mntone.Nico2.Order.Descending
			}
			.ToParameterString();
		}
	}
}
