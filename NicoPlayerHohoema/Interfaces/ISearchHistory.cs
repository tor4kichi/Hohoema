using NicoPlayerHohoema.Models;

namespace NicoPlayerHohoema.Interfaces
{
    public interface ISearchHistory
    {
        string Keyword { get; }
        SearchTarget Target { get; }
    }
}