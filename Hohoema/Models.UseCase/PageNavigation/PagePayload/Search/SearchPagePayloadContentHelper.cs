using Hohoema.Models.Domain;
using Hohoema.Models.Domain.PageNavigation;
using System;

namespace Hohoema.Models.UseCase.PageNavigation
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
                case SearchTarget.Niconama:
					return new LiveSearchPagePayloadContent() { Keyword = keyword };
                default:
					break;
			}

			throw new NotSupportedException();
		}
	}
}
