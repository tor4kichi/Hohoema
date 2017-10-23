namespace NicoPlayerHohoema.Models
{
    public interface ISearchPagePayloadContent
	{
		string Keyword { get; }

		string ToParameterString();
	}
}
