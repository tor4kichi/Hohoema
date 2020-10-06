using Hohoema.Models.Domain;
using Hohoema.Models.Domain.PageNavigation;

namespace Hohoema.Presentation.Services.Page
{
    public interface ISearchPagePayloadContent
	{
		string Keyword { get; }

        SearchTarget SearchTarget { get; }

		string ToParameterString();
	}
}
