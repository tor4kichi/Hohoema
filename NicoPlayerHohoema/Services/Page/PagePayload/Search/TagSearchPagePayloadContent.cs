using NicoPlayerHohoema.Models;

namespace NicoPlayerHohoema.Services.Page
{
    public class TagSearchPagePayloadContent : VideoSearchOption
	{
		public override SearchTarget SearchTarget => SearchTarget.Tag;
	}
}
