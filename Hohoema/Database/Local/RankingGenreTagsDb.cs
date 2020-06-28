using LiteDB;
using Mntone.Nico2.Videos.Ranking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Database.Local
{
    public class RankingGenreTag
    {
        public string DisplayName { get; set; }
        public string Tag { get; set; }
    }

    public class RankingGenreTagsInfo
    {
        [BsonId]
        public string GenreCode { get; set; }

        [BsonField]
        public DateTime UpdateAt { get; set; }

        [BsonField]
        public List<RankingGenreTag> Tags { get; set; }
    }


    

    public class RankingGenreTagsDb
    {
        public static RankingGenreTagsInfo Get(RankingGenre genre)
        {
            if (genre == RankingGenre.All) { return null; }

            var db = HohoemaLiteDb.GetLocalLiteRepository();
            var genreCode = genre.ToString();
            return db.SingleOrDefault<RankingGenreTagsInfo>(x => x.GenreCode == genreCode);
        }

        public static bool Upsert(RankingGenre genre, IEnumerable<RankingGenreTag> tags)
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            db.Upsert(new RankingGenreTagsInfo()
            {
                GenreCode = genre.ToString(),
                Tags = tags.ToList(),
                UpdateAt = DateTime.Now
            });
            return true;
        }

        public static bool Delete(RankingGenre genre)
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            var genreCode = genre.ToString();
            return db.Delete<RankingGenreTagsInfo>(x => x.GenreCode == genreCode) > 0;
        }
    }
}
