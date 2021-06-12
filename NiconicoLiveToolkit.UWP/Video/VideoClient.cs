using NiconicoToolkit.Ranking.Video;
using NiconicoToolkit.Mylist;
using NiconicoToolkit.Video.Watch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.Video
{
    public sealed class VideoClient
    {
        private readonly NiconicoContext _context;

        private readonly JsonSerializerOptions _option;

        public VideoRankinguSubClient Ranking { get; }
        public VideoWatchSubClient VideoWatch { get; }

        internal VideoClient(NiconicoContext context, JsonSerializerOptions defaultOptions)
        {
            _option = defaultOptions;
            _context = context;
            Ranking = new VideoRankinguSubClient(context);
            VideoWatch = new VideoWatchSubClient(context, _option);
        }
    }

}

