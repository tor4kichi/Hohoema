using NicoPlayerHohoema.Models;

namespace NicoPlayerHohoema.Services.Page
{
    public interface ISearchPagePayloadContent
	{
		string Keyword { get; }

        SearchTarget SearchTarget { get; }

		string ToParameterString();
	}
}
