using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.Search
{
    public sealed class SearchClient 
    {
        private readonly NiconicoContext _context;

        public SearchClient(NiconicoContext context)
        {
            _context = context;
            Video = new Video.VideoSearchSubClient(context);
        }

        public Video.VideoSearchSubClient Video { get; }
    }
}
