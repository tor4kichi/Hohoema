using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.SearchWithPage
{
    public sealed class SearchWithPageClient 
    {
        private readonly NiconicoContext _context;

        public SearchWithPageClient(NiconicoContext context)
        {
            _context = context;
            Video = new Video.VideoSearchSubClient(context);
        }

        public Video.VideoSearchSubClient Video { get; }
    }
}
