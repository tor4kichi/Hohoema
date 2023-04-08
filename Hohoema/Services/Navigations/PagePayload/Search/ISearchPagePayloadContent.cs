using Hohoema.Models.PageNavigation;

namespace Hohoema.Services.Navigations;

public interface ISearchPagePayloadContent
	{
		string Keyword { get; }

    SearchTarget SearchTarget { get; }

		string ToParameterString();
	}
