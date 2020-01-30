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

        public async Task<List<Series>> GetUserSeriesAsync(string userId)
        {
            var res = await _niconicoSession.Context.User.GetUserSeriesListAsync(userId);
            return res.Series;
        }
    }
}
