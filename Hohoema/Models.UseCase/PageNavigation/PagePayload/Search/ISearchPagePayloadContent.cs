using Hohoema.Models.Domain;
using Hohoema.Models.Domain.PageNavigation;

namespace Hohoema.Models.UseCase.PageNavigation
{
    public interface ISearchPagePayloadContent
	{
		string Keyword { get; }

        SearchTarget SearchTarget { get; }

		string ToParameterString();
	}
}
