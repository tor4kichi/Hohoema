
namespace Hohoema.Models.Domain.PageNavigation
{
    public interface ISearchHistory
    {
        string Keyword { get; }
        SearchTarget Target { get; }
    }
}