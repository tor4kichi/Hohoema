using Hohoema.Models;

namespace Hohoema.Models.Pages.PagePayload
{
    public class TagSearchPagePayloadContent : VideoSearchOption<TagSearchPagePayloadContent>
	{
		public override SearchTarget SearchTarget => SearchTarget.Tag;
	}
}
