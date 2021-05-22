using NiconicoToolkit.Activity.VideoWatchHistory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.Activity
{
    public sealed class ActivityClient
    {
        private readonly NiconicoContext _context;

        public ActivityClient(NiconicoContext context)
        {
            _context = context;
            VideoWachHistory = new VideoWatchHisotrySubClient(context);
        }

        public VideoWatchHisotrySubClient VideoWachHistory { get; }
    }
}
