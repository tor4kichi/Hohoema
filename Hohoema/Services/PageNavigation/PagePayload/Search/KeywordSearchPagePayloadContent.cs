using Hohoema.Models;
using Hohoema.Models.PageNavigation;

namespace Hohoema.Services.PageNavigation
{
    public class KeywordSearchPagePayloadContent : VideoSearchOption
	{
		public override SearchTarget SearchTarget => SearchTarget.Keyword;
	}
}
