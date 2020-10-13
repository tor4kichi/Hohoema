using Hohoema.Models.Domain;
using Hohoema.Models.Domain.PageNavigation;

namespace Hohoema.Presentation.Services.Page
{
    public class KeywordSearchPagePayloadContent : VideoSearchOption
	{
		public override SearchTarget SearchTarget => SearchTarget.Keyword;
	}
}
