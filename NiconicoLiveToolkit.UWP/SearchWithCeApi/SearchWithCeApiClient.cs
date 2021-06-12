using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.SearchWithCeApi
{
    public sealed class SearchWithCeApiClient
    {
        public SearchWithCeApiClient(NiconicoContext context, System.Text.Json.JsonSerializerOptions defaultOptions)
        {
            Video = new Video.VideoSearchSubClient(context, defaultOptions);
        }

        public Video.VideoSearchSubClient Video { get; }
    }
}
