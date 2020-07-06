using System;

namespace Hohoema.Models.Repository.Niconico.NicoVideo.Series
{
    public class SeriresVideo
    {
        private Mntone.Nico2.Videos.Series.SeriresVideo _seriesVideo;

        public SeriresVideo(Mntone.Nico2.Videos.Series.SeriresVideo seriesVideo)
        {
            _seriesVideo = seriesVideo;
        }

        public Uri ThumbnailUrl => _seriesVideo.ThumbnailUrl;
        public string Id => _seriesVideo.Id;
        public string Title => _seriesVideo.Title;
        public TimeSpan Duration => _seriesVideo.Duration;
        public DateTime PostAt => _seriesVideo.PostAt;
        public int WatchCount => _seriesVideo.WatchCount;
        public int CommentCount => _seriesVideo.CommentCount;
        public int MylistCount => _seriesVideo.MylistCount;
    }
}
