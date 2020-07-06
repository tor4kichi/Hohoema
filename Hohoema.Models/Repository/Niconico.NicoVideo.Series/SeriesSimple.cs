namespace Hohoema.Models.Repository.Niconico.NicoVideo.Series
{
    public class SeriesSimple
    {
        private Mntone.Nico2.Videos.Series.SeriesSimple _series;

        public SeriesSimple(Mntone.Nico2.Videos.Series.SeriesSimple series)
        {
            _series = series;
        }

        public string Id => _series.Id;
        public string Title => _series.Title;
    }
}
