using Hohoema.Models.Niconico.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.Search
{
    public sealed class VideoSearchResult
    {
        public int ItemsCount { get; set; }
        public int TotalCount { get; set; }

        public List<NicoVideoTag> Tags { get; set; }

        public List<Database.NicoVideo> VideoItems { get; set; }


    }
}
