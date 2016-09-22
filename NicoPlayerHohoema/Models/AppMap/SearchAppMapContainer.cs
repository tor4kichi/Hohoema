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

		public override ContainerItemDisplayType ItemDisplayType => ContainerItemDisplayType.Card;

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


			ISearchPagePayloadContent content = null;
			switch (history.Target)
			{
				case SearchTarget.Keyword:
					content = new KeywordSearchPagePayloadContent()
					{
						Keyword = history.Keyword,
						Sort = Mntone.Nico2.Sort.FirstRetrieve,
						Order = Mntone.Nico2.Order.Descending
					};
					break;
				case SearchTarget.Tag:
					content = new TagSearchPagePayloadContent()
					{
						Keyword = history.Keyword,
						Sort = Mntone.Nico2.Sort.FirstRetrieve,
						Order = Mntone.Nico2.Order.Descending
					};
					break;
				case SearchTarget.Mylist:
					content = new MylistSearchPagePayloadContent()
					{
						Keyword = history.Keyword,
						Sort = Mntone.Nico2.Sort.FirstRetrieve,
						Order = Mntone.Nico2.Order.Descending
					};
					break;
				case SearchTarget.Community:
					content = new CommunitySearchPagePayloadContent()
					{
						Keyword = history.Keyword,
						Sort = Mntone.Nico2.Searches.Community.CommunitySearchSort.CreatedAt,
						Order = Mntone.Nico2.Order.Descending,
						Mode = Mntone.Nico2.Searches.Community.CommunitySearchMode.Keyword
					};
					break;
				case SearchTarget.Niconama:
					break;
				default:
					break;
			}

			if (content == null) { throw new NotSupportedException(history.Target.ToString()); }

			var payload = new SearchPagePayload(content);
			Parameter = payload.ToParameterString();
		}
	}
}
