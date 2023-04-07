using Hohoema.Infra;
using LiteDB;
using NiconicoToolkit.Ranking.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Video.Ranking
{

    public class RankingGenreTagEntry
    {
        public string DisplayName { get; set; }
        public string Tag { get; set; }
    }

    public class RankingTagsGenreGroupedEntry
    {
        [BsonId]
        public string GenreCode { get; set; }

        [BsonField]
        public DateTime UpdateAt { get; set; }

        [BsonField]
        public List<RankingGenreTagEntry> Tags { get; set; }

        
    }


    

    public class RankingGenreCache : LiteDBServiceBase<RankingTagsGenreGroupedEntry>
    {
        public RankingGenreCache(LiteDatabase liteDatabase) : base(liteDatabase)
        {
        }

        public RankingTagsGenreGroupedEntry Get(RankingGenre genre)
        {
            if (genre == RankingGenre.All) { return null; }

            var genreCode = genre.ToString();
            return _collection.FindOne(x => x.GenreCode == genreCode);
        }

        public bool Upsert(RankingGenre genre, IEnumerable<RankingGenreTagEntry> tags)
        {
            return _collection.Upsert(new RankingTagsGenreGroupedEntry()
            {
                GenreCode = genre.ToString(),
                Tags = tags.ToList(),
                UpdateAt = DateTime.Now
            });
        }

        public bool Delete(RankingGenre genre)
        {
            var genreCode = genre.ToString();
            return _collection.DeleteMany(x => x.GenreCode == genreCode) > 0;
        }
    }
}
