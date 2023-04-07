using Hohoema.Models;
using Hohoema.Models.PageNavigation;

namespace Hohoema.Models.UseCase.PageNavigation
{
    public class TagSearchPagePayloadContent : VideoSearchOption
	{
		public override SearchTarget SearchTarget => SearchTarget.Tag;
	}
}
