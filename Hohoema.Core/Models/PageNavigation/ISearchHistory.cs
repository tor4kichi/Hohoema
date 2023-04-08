
namespace Hohoema.Models.PageNavigation;

public interface ISearchHistory
{
    string Keyword { get; }
    SearchTarget Target { get; }
}