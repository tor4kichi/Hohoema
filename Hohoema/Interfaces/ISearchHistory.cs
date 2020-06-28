using Hohoema.Models;

namespace Hohoema.Interfaces
{
    public interface ISearchHistory
    {
        string Keyword { get; }
        SearchTarget Target { get; }
    }
}