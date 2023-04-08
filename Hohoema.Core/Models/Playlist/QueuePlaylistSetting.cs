using Hohoema.Infra;

namespace Hohoema.Models.Playlist;

[System.Obsolete]
public sealed class QueuePlaylistSetting : FlagsRepositoryBase
{
    [System.Obsolete]
    public string LastSelectedSortOptions
    {
        get => Read(string.Empty);
        set => Save(value);
    }


    [System.Obsolete]
    public bool IsGroupingNearByTitleThenByTitleAscending
    {
        get => Read(true);
        set => Save(value);
    }

    public const double DefaultTitleSimulalityThreshold = 0.8;

    [System.Obsolete]
    public double TitleSimulalityThreshold
    {
        get => Read(DefaultTitleSimulalityThreshold);
        set => Save(value);
    }

}
