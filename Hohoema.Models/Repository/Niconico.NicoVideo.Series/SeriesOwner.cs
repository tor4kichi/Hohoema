using System;

namespace Hohoema.Models.Repository.Niconico.NicoVideo.Series
{
    public class SeriesOwner
    {
        private Mntone.Nico2.Users.Series.Owner _owner;

        public SeriesOwner(Mntone.Nico2.Users.Series.Owner owner)
        {
            _owner = owner;
        }

        public string Type { get; set; }

        public string Id { get; set; }


        public SeriesProviderType ProviderType => Type switch
        {
            "user" => SeriesProviderType.User,
            _ => throw new NotSupportedException(Type)
        };
    }
}
