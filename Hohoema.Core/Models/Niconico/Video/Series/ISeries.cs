#nullable enable
using NiconicoToolkit.Video;

namespace Hohoema.Models.Niconico.Video.Series;

public interface ISeries
{
    string Id { get; }
    string Title { get; }
    bool IsListed { get; }
    string Description { get; }
    string ThumbnailUrl { get; }
    int ItemsCount { get; }

    OwnerType ProviderType { get; }
    string ProviderId { get; }
}
