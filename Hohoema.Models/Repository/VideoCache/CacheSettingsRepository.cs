using Hohoema.Models.Niconico.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.VideoCache
{
    public sealed class CacheSettingsRepository : FlagsRepositoryBase
    {
        public CacheSettingsRepository()
        {
            _IsCacheEnabled = Read(true, nameof(IsCacheEnabled));
            _IsAcceptedCache = Read(false, nameof(IsAcceptedCache));
        }

        bool _IsCacheEnabled;
        public bool IsCacheEnabled
        {
            get => _IsCacheEnabled;
            set => SetProperty(ref _IsCacheEnabled, value);
        }

        bool _IsAcceptedCache;
        public bool IsAcceptedCache
        {
            get => _IsAcceptedCache;
            set => SetProperty(ref _IsAcceptedCache, value);
        }

        NicoVideoQuality _DefaultCacheQuality;
        public NicoVideoQuality DefaultCacheQuality
        {
            get => _DefaultCacheQuality;
            set => SetProperty(ref _DefaultCacheQuality, value);
        }
    }
}
