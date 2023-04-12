#nullable enable
using Hohoema.Infra;

namespace Hohoema.Models.Playlist;

public sealed class QueuePlaylistSetting : FlagsRepositoryBase
{
    public string LastSelectedSortOptions
    {
        get => Read(string.Empty);
        set => Save(value);
    }


    public bool IsGroupingNearByTitleThenByTitleAscending
    {
        get => Read(true);
        set => Save(value);
    }

    public const double DefaultTitleSimulalityThreshold = 0.8;

    public double TitleSimulalityThreshold
    {
        get => Read(DefaultTitleSimulalityThreshold);
        set => Save(value);
    }

}
