using Hohoema.Models;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.Niconico;

namespace Hohoema.Models.Repository.Niconico.NicoVideo.Series
{
    public sealed class SeriesRepository : ProviderBase
    {
        public SeriesRepository(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }

        public async Task<SeriesDetails> GetSeriesVideosAsync(string seriesId)
        {
            var res = await ContextActionWithPageAccessWaitAsync(context =>
            {
                return context.Video.GetSeriesVideosAsync(seriesId);
            });

            return new SeriesDetails(res);
        }
    }
}
