using Hohoema.Models.Domain;
using Hohoema.Models.Domain.PageNavigation;

namespace Hohoema.Models.UseCase.PageNavigation
{
    public class KeywordSearchPagePayloadContent : VideoSearchOption
	{
		public override SearchTarget SearchTarget => SearchTarget.Keyword;
	}
}
