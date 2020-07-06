using System.Collections.Generic;
using System.Linq;

namespace Hohoema.Models.Repository.Niconico.NicoVideo.Series
{
    public class SeriesDetails
    {
        private Mntone.Nico2.Videos.Series.SeriesDetails _res;

        public SeriesDetails(Mntone.Nico2.Videos.Series.SeriesDetails res)
        {
            _res = res;
        }

        Series _Series;
        public Series Series => _Series ??= new Series(_res.Series);

        public string DescriptionHTML => _res.DescriptionHTML;

        SeriesOwnerInfo _Owner;
        public SeriesOwnerInfo Owner => new SeriesOwnerInfo(_res.Owner);

        IReadOnlyList<SeriresVideo> _videos;
        public IReadOnlyList<SeriresVideo> Videos => _videos ??= _res.Videos?.Select(seriesVideo => new SeriresVideo(seriesVideo)).ToList();

        IReadOnlyList<SeriesSimple> _OwnerSeries;
        public IReadOnlyList<SeriesSimple> OwnerSeries => _OwnerSeries ??= _res.OwnerSeries?.Select(series => new SeriesSimple(series)).ToList();
    }
}
