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
			return new SearchHistoryAppMapItem(history, PageManager);
		}
	}

	public class SearchHistoryAppMapItem : IAppMapItem
	{
		public string PrimaryLabel { get; private set; }
		public string SecondaryLabel { get; private set; }
		public string Parameter { get; private set; }

		public HohoemaPageType PageType { get; private set; }
        
        public PageManager PageManager { get; private set; }

        public SearchHistoryAppMapItem(Db.SearchHistory history, PageManager pageManager)
		{
            PageManager = pageManager;
            PrimaryLabel = history.Keyword;
			SecondaryLabel = history.Target.ToString();


			ISearchPagePayloadContent content = null;
			switch (history.Target)
			{
				case SearchTarget.Keyword:
                    PageType = HohoemaPageType.SearchResultKeyword;
                    content = new KeywordSearchPagePayloadContent()
					{
						Keyword = history.Keyword,
						Sort = Mntone.Nico2.Sort.FirstRetrieve,
						Order = Mntone.Nico2.Order.Descending
					};
					break;
				case SearchTarget.Tag:
                    PageType = HohoemaPageType.SearchResultTag;
                    content = new TagSearchPagePayloadContent()
					{
						Keyword = history.Keyword,
						Sort = Mntone.Nico2.Sort.FirstRetrieve,
						Order = Mntone.Nico2.Order.Descending
					};
					break;
				case SearchTarget.Mylist:
                    PageType = HohoemaPageType.SearchResultMylist;
                    content = new MylistSearchPagePayloadContent()
					{
						Keyword = history.Keyword,
						Sort = Mntone.Nico2.Sort.FirstRetrieve,
						Order = Mntone.Nico2.Order.Descending
					};
					break;
				case SearchTarget.Community:
                    PageType = HohoemaPageType.SearchResultCommunity;
                    content = new CommunitySearchPagePayloadContent()
					{
						Keyword = history.Keyword,
						Sort = Mntone.Nico2.Searches.Community.CommunitySearchSort.CreatedAt,
						Order = Mntone.Nico2.Order.Descending,
						Mode = Mntone.Nico2.Searches.Community.CommunitySearchMode.Keyword
					};
					break;
				case SearchTarget.Niconama:
                    PageType = HohoemaPageType.SearchResultLive;
                    content = new LiveSearchPagePayloadContent()
					{
						Keyword = history.Keyword,
						Sort = Mntone.Nico2.Searches.Live.NicoliveSearchSort.Recent,
						Order = Mntone.Nico2.Order.Ascending,
						Mode = Mntone.Nico2.Searches.Live.NicoliveSearchMode.OnAir,
					};
					break;
				default:
					break;
			}

			if (content == null) { throw new NotSupportedException(history.Target.ToString()); }

            
//            var payload = new SearchPagePayload(content);
            
            Parameter = content.ToParameterString();
		}


        public void SelectedAction()
        {
            PageManager.OpenPage(PageType, Parameter);
        }
	}
}
