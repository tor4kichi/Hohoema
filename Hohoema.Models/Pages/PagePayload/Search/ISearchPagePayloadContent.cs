using Hohoema.Models;

namespace Hohoema.Models.Pages.PagePayload
{
    public interface ISearchPagePayloadContent
	{
		string Keyword { get; }

        SearchTarget SearchTarget { get; }

		string ToParameterString();
	}
}
