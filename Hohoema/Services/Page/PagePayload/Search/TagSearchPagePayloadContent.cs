using Hohoema.Models;

namespace Hohoema.Services.Page
{
    public class TagSearchPagePayloadContent : VideoSearchOption
	{
		public override SearchTarget SearchTarget => SearchTarget.Tag;
	}
}
