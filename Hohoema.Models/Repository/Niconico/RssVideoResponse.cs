using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico
{
    public sealed class RssVideoResponse
    {
        private Mntone.Nico2.RssVideoResponse _res;

        public RssVideoResponse(Mntone.Nico2.RssVideoResponse res)
        {
            _res = res;

            Items = res.Items?.Select(x => new RssVideoData(x)).ToList();
        }

        public IReadOnlyList<RssVideoData> Items { get; }
    }


    public sealed class RssVideoData
    {
        private readonly Mntone.Nico2.RssVideoData _video;

        public RssVideoData(Mntone.Nico2.RssVideoData video)
        {
            _video = video;
        }

        public string RawTitle => _video.RawTitle;

        public Uri WatchPageUrl => _video.WatchPageUrl;

        public DateTimeOffset PubDate => _video.PubDate;

        public string Description => _video.Description;
    }
}
