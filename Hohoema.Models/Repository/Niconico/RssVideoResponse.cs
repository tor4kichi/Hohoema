using Mntone.Nico2.Videos.Ranking;
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

        public string VideoId => _video.GetVideoId();

        public string Title => _video.GetRankTrimmingTitle();

        RankingVideoMoreData _MoreData;
        public RankingVideoMoreData MoreData => _MoreData ??= new RankingVideoMoreData(_video.GetMoreData());

        public string RawTitle => _video.RawTitle;

        public Uri WatchPageUrl => _video.WatchPageUrl;

        public DateTimeOffset PubDate => _video.PubDate;

        public string Description => _video.Description;
    }


    public class RankingVideoMoreData
    {
        private Mntone.Nico2.Videos.Ranking.RankingVideoMoreData _rankingVideoMoreData;

        public RankingVideoMoreData(Mntone.Nico2.Videos.Ranking.RankingVideoMoreData rankingVideoMoreData)
        {
            _rankingVideoMoreData = rankingVideoMoreData;
        }

        public string Title => _rankingVideoMoreData.Title;
        public TimeSpan Length => _rankingVideoMoreData.Length;
        public string ThumbnailUrl => _rankingVideoMoreData.ThumbnailUrl;
        public int WatchCount => _rankingVideoMoreData.WatchCount;
        public int CommentCount => _rankingVideoMoreData.CommentCount;
        public int MylistCount => _rankingVideoMoreData.MylistCount;
    }
}
