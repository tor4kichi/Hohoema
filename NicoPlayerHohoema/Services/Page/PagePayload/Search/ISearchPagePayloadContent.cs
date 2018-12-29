namespace NicoPlayerHohoema.Services.Page
{
    public interface ISearchPagePayloadContent
	{
		string Keyword { get; }

		string ToParameterString();
	}
}
