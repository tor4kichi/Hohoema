using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Database
{
    public class Bookmark
    {
        [LiteDB.BsonId(autoId: true)]
        public int Id { get; set; }

        public BookmarkType BookmarkType { get; set; }
        public string Content { get; set; }

        public string Label { get; set; }
    }

}
