using Hohoema.Models;
using Hohoema.Models.PageNavigation;

namespace Hohoema.Services.PageNavigation
{
    public interface ISearchPagePayloadContent
	{
		string Keyword { get; }

        SearchTarget SearchTarget { get; }

		string ToParameterString();
	}
}
