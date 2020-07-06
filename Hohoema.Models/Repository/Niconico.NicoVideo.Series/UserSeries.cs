namespace Hohoema.Models.Repository.Niconico.NicoVideo.Series
{
    public class UserSeries
    {
        private Mntone.Nico2.Users.Series.UserSeries _series;

        public UserSeries(Mntone.Nico2.Users.Series.UserSeries series)
        {
            _series = series;
        }

        public int Id => _series.Id;

        private SeriesOwner _owner;
        public SeriesOwner Owner => _owner ??= new SeriesOwner(_series.Owner);

        public string Title => _series.Title;

        public bool IsListed => _series.IsListed;

        public string Description => _series.Description;

        public string ThumbnailUrl => _series.ThumbnailUrl;

        public int ItemsCount => _series.ItemsCount;
    }
}
