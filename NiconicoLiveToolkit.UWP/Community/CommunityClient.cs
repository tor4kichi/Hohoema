using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.Community
{
    public sealed class CommunityClient
    {
        private readonly NiconicoContext _context;

        internal CommunityClient(NiconicoContext context)
        {
            _context = context;
        }

        public static string MakeCommunityPageUrl(string communityId)
        {
            return $"http://com.nicovideo.jp/community/{communityId}";
        }
    }
}
