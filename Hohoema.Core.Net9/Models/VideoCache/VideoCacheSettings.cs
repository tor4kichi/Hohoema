﻿#nullable enable
using Hohoema.Infra;
using Hohoema.Models.Niconico.Video;

namespace Hohoema.Models.VideoCache;

public sealed class VideoCacheSettings : FlagsRepositoryBase
{
    
    public VideoCacheSettings()
    {
        _MaxVideoCacheStorageSize = Read(default(long?), nameof(MaxVideoCacheStorageSize));
    }

    private bool? _IsAllowDownload;

    
    public bool IsAllowDownload
    {
        get => _IsAllowDownload ??= Read(true);
        set => SetProperty(ref _IsAllowDownload, value);
    }

    private long? _MaxVideoCacheStorageSize;

    
    public long? MaxVideoCacheStorageSize
    {
        get => _MaxVideoCacheStorageSize;
        set => SetProperty(ref _MaxVideoCacheStorageSize, value);
    }


    private bool? _IsAllowDownloadOnMeteredNetwork;

    
    public bool IsAllowDownloadOnMeteredNetwork
    {
        get => _IsAllowDownloadOnMeteredNetwork ??= Read(false);
        set => SetProperty(ref _IsAllowDownloadOnMeteredNetwork, value);
    }


    public long? _CachedStorageSize;

    
    public long CachedStorageSize
    {
        get => _CachedStorageSize ??= Read(0L);
        set => SetProperty(ref _CachedStorageSize, value);
    }

    
    public NicoVideoQuality DefaultCacheQuality
    {
        get => Read(NicoVideoQuality.High);
        set => Save(value);
    }

}
