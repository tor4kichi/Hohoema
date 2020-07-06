namespace Hohoema.Models.Repository.Niconico.NicoVideo.Series
{
    public class SeriesOwnerInfo
    {
        private Mntone.Nico2.Videos.Series.SeriesOwner _owner;

        public SeriesOwnerInfo(Mntone.Nico2.Videos.Series.SeriesOwner owner)
        {
            _owner = owner;
        }

        public string Id => _owner.Id;
        public string Nickname => _owner.Nickname;
        public string IconUrl => _owner.IconUrl;
    }
}
