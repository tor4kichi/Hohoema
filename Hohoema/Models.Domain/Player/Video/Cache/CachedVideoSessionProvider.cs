using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Collections.Immutable;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Player;

namespace Hohoema.Models.Domain.Player.Video.Cache
{
    public class CachedVideoSessionProvider : INiconicoVideoSessionProvider
    {
        public string ContentId { get; }

        public ImmutableArray<NicoVideoQualityEntity> AvailableQualities { get; }

        private readonly VideoCacheManagerLegacy _videoCacheManager;
        private readonly Dictionary<NicoVideoQuality, NicoVideoCached> _cachedQualities;



        public CachedVideoSessionProvider(string contentId, VideoCacheManagerLegacy videoCacheManager, IEnumerable<NicoVideoCached> qualities)
        {
            ContentId = contentId;
            _videoCacheManager = videoCacheManager;
            _cachedQualities = qualities.ToDictionary(x => x.Quality);
            AvailableQualities = qualities.Select(x => new NicoVideoQualityEntity(true, x.Quality, x.Quality.ToString())).ToImmutableArray();
        }

        public bool CanPlayQuality(string qualityId)
        {
            if (!Enum.TryParse<NicoVideoQuality>(qualityId, out var quality))
            {
                return false;
            }

            return _cachedQualities.ContainsKey(quality);
        }

        public Task<IStreamingSession> CreateVideoSessionAsync(NicoVideoQuality quality)
        {
            if (_cachedQualities.ContainsKey(quality))
            {
                return _videoCacheManager.CreateStreamingSessionAsync(_cachedQualities[quality]);
            }
            else if (_cachedQualities.Any())
            {
                return _videoCacheManager.CreateStreamingSessionAsync(_cachedQualities.Last().Value);
            }
            else
            {
                throw new Exception();
            }
        }
    }








}
