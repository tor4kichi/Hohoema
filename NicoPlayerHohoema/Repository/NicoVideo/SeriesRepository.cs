using Mntone.Nico2.Users.Series;
using Mntone.Nico2.Videos.Series;
using NicoPlayerHohoema.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Repository.NicoVideo
{
    public sealed class SeriesRepository
    {
        private readonly NiconicoSession _niconicoSession;

        public SeriesRepository(NiconicoSession niconicoSession)
        {
            _niconicoSession = niconicoSession;
        }

        public async Task<IList<UserSeries>> GetUserSeriesAsync(string userId)
        {
            var res = await _niconicoSession.Context.User.GetUserSeiresAsync(userId);
            return res.Serieses;
        }

        public async Task<SeriesDetails> GetSeriesVideosAsync(string seriesId)
        {
            return await _niconicoSession.Context.Video.GetSeriesVideosAsync(seriesId);
        }
    }
}
