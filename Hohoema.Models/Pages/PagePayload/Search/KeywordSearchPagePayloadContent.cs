using Hohoema.Models;

namespace Hohoema.Models.Pages.PagePayload
{
    public class KeywordSearchPagePayloadContent : VideoSearchOption<KeywordSearchPagePayloadContent>
	{
		public override SearchTarget SearchTarget => SearchTarget.Keyword;
	}
}
