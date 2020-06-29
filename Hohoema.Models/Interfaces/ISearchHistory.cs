using Hohoema.Models;
using Hohoema.Models.Pages;

namespace Hohoema.Interfaces
{
    public interface ISearchHistory
    {
        string Keyword { get; }
        SearchTarget Target { get; }
    }
}