using LiteDB;
using Mntone.Nico2.Videos.Ranking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Database.Local
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
        public List<RankingGenreTag> Tags { get; set; }
    }

    public class RankingGenreTagsDb
    {
        public static List<RankingGenreTag> Get(RankingGenre genre)
        {
            if (genre == RankingGenre.All) { return new List<RankingGenreTag>(); }

            var db = HohoemaLiteDb.GetLocalLiteRepository();
            var genreCode = genre.ToString();
            return db.SingleOrDefault<RankingGenreTagsInfo>(x => x.GenreCode == genreCode)?.Tags ?? new List<RankingGenreTag>();
        }

        public static bool Upsert(RankingGenre genre, IEnumerable<RankingGenreTag> tags)
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            db.Upsert(new RankingGenreTagsInfo()
            {
                GenreCode = genre.ToString(),
                Tags = tags.ToList()
            });
            return true;
        }

        public static bool Delete(RankingGenre genre)
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            var genreCode = genre.ToString();
            return db.Delete< RankingGenreTagsInfo>(x => x.GenreCode == genreCode) > 0;
        }



        const string FavoriteId = "__Faovrite__";
        public static List<RankingGenreTag> GetFavoriteItems()
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            return db.SingleOrDefault<RankingGenreTagsInfo>(x => x.GenreCode == FavoriteId)?.Tags ?? new List<RankingGenreTag>();
        }

        public static void SetFavoriteItems(IEnumerable<RankingGenreTag> tags)
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            db.Upsert<RankingGenreTagsInfo>(new RankingGenreTagsInfo()
            {
                GenreCode = FavoriteId,
                Tags = tags.ToList()
            } );
        }
    }
}
