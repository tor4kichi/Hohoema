using Hohoema.Models;
using Hohoema.Models.PageNavigation;

namespace Hohoema.Services.Navigations
{
    public class TagSearchPagePayloadContent : VideoSearchOption
	{
		public override SearchTarget SearchTarget => SearchTarget.Tag;
	}
}
