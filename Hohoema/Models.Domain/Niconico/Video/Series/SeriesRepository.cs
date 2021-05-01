using Mntone.Nico2.Users.Series;
using Mntone.Nico2.Videos.Series;
using Hohoema.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.Video.Series
{
    using UserSeries = Mntone.Nico2.Users.Series.UserSeries;
    public sealed class SeriesRepository
    {
        private readonly NiconicoSession _niconicoSession;

        public SeriesRepository(NiconicoSession niconicoSession)
        {
            _niconicoSession = niconicoSession;
        }

        public async Task<IList<UserSeries>> GetUserSeriesAsync(string userId)
        {
            List<UserSeries> items = new List<Mntone.Nico2.Users.Series.UserSeries>();
            var res = await _niconicoSession.Context.User.GetUserSeiresAsync(userId, 0);
            items.AddRange(res.Data.Items);
            uint page = 1;
            while (items.Count < res.Data.TotalCount)
            {
                res = await _niconicoSession.Context.User.GetUserSeiresAsync(userId, page);
                items.AddRange(res.Data.Items);
            }

            return items;
        }

        public async Task<SeriesDetails> GetSeriesVideosAsync(string seriesId)
        {
            return await _niconicoSession.Context.Video.GetSeriesVideosAsync(seriesId);
        }
    }
}
