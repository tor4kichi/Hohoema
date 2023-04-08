namespace Hohoema.Models.Player.Video.Cache;

public enum NicoVideoCacheState
{
    NotCacheRequested,
    Pending,
    Downloading,
    Cached,

    Failed,
    FailedWithQualityNotAvairable,
    DeletedFromUser,
    DeletedFromNiconicoServer,
}
