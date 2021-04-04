using Hohoema.Models.Infrastructure;

namespace Hohoema.Models.Domain.VideoCache
{
    public sealed class VideoCacheSettings : FlagsRepositoryBase
    {
        public VideoCacheSettings()
        {
        }

        public long? MaxVideoCacheStorageSize
        {
            get => Read(default(long?));
            set => Save(value);
        }


        public bool IsAllowDownloadOnRestrictedNetwork
        {
            get => Read(true);
            set => Save(value);
        }

        public long CachedStorageSize
        {
            get => Read(0);
            set => Save(value);
        }
    }
}
