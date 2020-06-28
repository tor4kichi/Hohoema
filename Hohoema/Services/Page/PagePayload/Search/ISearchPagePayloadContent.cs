using Hohoema.Models;

namespace Hohoema.Services.Page
{
    public interface ISearchPagePayloadContent
	{
		string Keyword { get; }

        SearchTarget SearchTarget { get; }

		string ToParameterString();
	}
}
