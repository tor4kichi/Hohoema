using Hohoema.Models.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Maintenance
{
    public interface IMaintenance
    {
        void Maitenance();
    }
    internal class VideoThumbnailImageCacheMaintenance : IMaintenance
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
}
