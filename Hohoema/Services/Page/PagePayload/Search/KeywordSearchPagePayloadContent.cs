using Hohoema.Models;

namespace Hohoema.Services.Page
{
    public class KeywordSearchPagePayloadContent : VideoSearchOption
	{
		public override SearchTarget SearchTarget => SearchTarget.Keyword;
	}
}
