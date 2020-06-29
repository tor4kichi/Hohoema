using Mntone.Nico2.Videos.Dmc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.NicoVideo
{
    public class VideoSeriesInfo
    {
        private Series _series;

        public VideoSeriesInfo(Series series)
        {
            _series = series;
        }

        public int Id { get; set; }

        public string Title { get; set; }

        public string ThumbnailUrl { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public SeriesVideo PrevVideo { get; set; }

        public SeriesVideo NextVideo { get; set; }
    }

    public class SeriesVideo
    {
        public string Type { get; set; }

        public string Id { get; set; }

        public string Title { get; set; }

        public DateTime RegisteredAt { get; set; }

        public SeriesVideoMetaCount MetaCount { get; set; }

        public SeriesVideoThumbnail Thumbnail { get; set; }

        public int Duration { get; set; }

        public string ShortDescription { get; set; }

        public string LatestCommentSummary { get; set; }

        public bool IsChannelVideo { get; set; }

        public bool IsPaymentRequired { get; set; }

        public SeriesVideoOwner owner { get; set; }

    }

    public class SeriesVideoThumbnail
    {
        public string Url { get; set; }

        public string MiddleUrl { get; set; }

        public string LargeUrl { get; set; }
    }

    public class SeriesVideoOwner
    {
        public string OwnerType { get; set; }

        public string Id { get; set; }

        public string Name { get; set; }

        public string IconUrl { get; set; }
    }

    public class SeriesVideoMetaCount
    {
        public int ViewCount { get; set; }

        public int CommentCount { get; set; }

        public int MylistCount { get; set; }
    }
}
