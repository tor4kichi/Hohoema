using System;

namespace NicoPlayerHohoema.Models
{
    public static class SearchPagePayloadContentHelper
	{
		public static ISearchPagePayloadContent CreateDefault(SearchTarget target, string keyword = null)
		{
			switch (target)
			{
				case SearchTarget.Keyword:
                    return new KeywordSearchPagePayloadContent() { Keyword = keyword };
				case SearchTarget.Tag:
					return new TagSearchPagePayloadContent() { Keyword = keyword };
                case SearchTarget.Mylist:
					return new MylistSearchPagePayloadContent() { Keyword = keyword };
                case SearchTarget.Community:
					return new CommunitySearchPagePayloadContent() { Keyword = keyword };
                case SearchTarget.Niconama:
					return new LiveSearchPagePayloadContent() { Keyword = keyword };
                default:
					break;
			}

			throw new NotSupportedException();
		}
	}
}
