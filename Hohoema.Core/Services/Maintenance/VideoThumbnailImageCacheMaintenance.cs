using Hohoema.Contracts.Maintenances;
using Hohoema.Models.Application;
using System;

namespace Hohoema.Services.Maintenance;

public class VideoThumbnailImageCacheMaintenance : IMaintenance
{
    private readonly ThumbnailCacheManager _thumbanilCacheManager;

    public VideoThumbnailImageCacheMaintenance(ThumbnailCacheManager thumbanilCacheManager)
    {
        _thumbanilCacheManager = thumbanilCacheManager;
    }
    public void Maitenance()
    {
        _thumbanilCacheManager.Maitenance(TimeSpan.FromDays(7), 1000);
    }
}
