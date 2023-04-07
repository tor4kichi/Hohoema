using Hohoema.Models;
using Hohoema.Models.PageNavigation;

namespace Hohoema.Models.UseCase.PageNavigation
{
    public class KeywordSearchPagePayloadContent : VideoSearchOption
	{
		public override SearchTarget SearchTarget => SearchTarget.Keyword;
	}
}
