using Hohoema.Infra;
using Hohoema.Models.Niconico.Video;

namespace Hohoema.Models.VideoCache;

public sealed class VideoCacheSettings : FlagsRepositoryBase
{
    [System.Obsolete]
    public VideoCacheSettings()
    {
        _MaxVideoCacheStorageSize = Read(default(long?), nameof(MaxVideoCacheStorageSize));
    }

    private bool? _IsAllowDownload;

    [System.Obsolete]
    public bool IsAllowDownload
    {
        get => _IsAllowDownload ??= Read(true);
        set => SetProperty(ref _IsAllowDownload, value);
    }

    private long? _MaxVideoCacheStorageSize;

    [System.Obsolete]
    public long? MaxVideoCacheStorageSize
    {
        get => _MaxVideoCacheStorageSize;
        set => SetProperty(ref _MaxVideoCacheStorageSize, value);
    }


    private bool? _IsAllowDownloadOnMeteredNetwork;

    [System.Obsolete]
    public bool IsAllowDownloadOnMeteredNetwork
    {
        get => _IsAllowDownloadOnMeteredNetwork ??= Read(false);
        set => SetProperty(ref _IsAllowDownloadOnMeteredNetwork, value);
    }


    public long? _CachedStorageSize;

    [System.Obsolete]
    public long CachedStorageSize
    {
        get => _CachedStorageSize ??= Read(0L);
        set => SetProperty(ref _CachedStorageSize, value);
    }

    [System.Obsolete]
    public NicoVideoQuality DefaultCacheQuality
    {
        get => Read(NicoVideoQuality.High);
        set => Save(value);
    }

}
