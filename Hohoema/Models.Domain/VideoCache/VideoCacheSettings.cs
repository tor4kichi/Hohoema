using Hohoema.Models.Infrastructure;

namespace Hohoema.Models.Domain.VideoCache
{
    public sealed class VideoCacheSettings : FlagsRepositoryBase
    {
        public VideoCacheSettings()
        {
            _MaxVideoCacheStorageSize = Read(default(long?), nameof(MaxVideoCacheStorageSize));
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

        public bool IsNotifyOnDownloadWithMeteredNetwork
        {
            get => Read(true);
            set => Save(value);
        }

        public long CachedStorageSize
        {
            get => Read(0);
            set => Save(value);
        }

        public NicoVideoQuality DefaultCacheQuality
        {
            get => Read(NicoVideoQuality.High);
            set => Save(value);
        }

}
}
