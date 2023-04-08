using Hohoema.Models.PageNavigation;

namespace Hohoema.Services.Navigations;

public class KeywordSearchPagePayloadContent : VideoSearchOption
	{
		public override SearchTarget SearchTarget => SearchTarget.Keyword;
	}
