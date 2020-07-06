using System;

namespace Hohoema.Models.Repository.Niconico.NicoVideo.Series
{
    public class Series
    {
        private Mntone.Nico2.Videos.Series.Series _series;

        public Series(Mntone.Nico2.Videos.Series.Series series)
        {
            _series = series;
        }

        public string Id => _series.Id;
        public string Title => _series.Title;
        public int Count => _series.Count ?? 0;
        public Uri ThumbnailUrl => _series.ThumbnailUrl;
    }
}
