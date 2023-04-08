using Hohoema.Models.Niconico.Video.Series;
using NiconicoToolkit.Series;
using NiconicoToolkit.Video;

namespace Hohoema.ViewModels.Niconico.Series;

public class UserSeriesItemViewModel : ISeries
{
    private readonly SeriesItem _userSeries;

    public UserSeriesItemViewModel(SeriesItem userSeries)
    {
        _userSeries = userSeries;
    }

    public string Id => _userSeries.Id.ToString();

    public string Title => _userSeries.Title;

    public bool IsListed => _userSeries.IsListed;

    public string Description => _userSeries.Description;

    public string ThumbnailUrl => _userSeries.ThumbnailUrl.OriginalString;

    public int ItemsCount => (int)_userSeries.ItemsCount;

    public OwnerType ProviderType => _userSeries.Owner.Type;

    public string ProviderId => _userSeries.Owner.Id;        
}


