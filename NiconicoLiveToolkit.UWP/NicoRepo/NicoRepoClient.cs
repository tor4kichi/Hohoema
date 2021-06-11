using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NiconicoToolkit.NicoRepo
{
    public sealed class NicoRepoClient
    {
        private readonly NiconicoContext _context;
        private readonly JsonSerializerOptions _options;

        internal NicoRepoClient(NiconicoContext context, JsonSerializerOptions options)
        {
            _context = context;
            _options = options;
        }

        internal static class Urls
        {
            public const string NicorepoTimelineApiUrl = $"{NiconicoUrls.PublicApiV1Url}timelines/nicorepo/last-1-month/my/pc/entries.json";
        }


        public Task<NicoRepoEntriesResponse> GetLoginUserNicoRepoEntriesAsync(NicoRepoType type, NicoRepoDisplayTarget target, string untilId = null)
        {
            NameValueCollection dict = new();
            if (type != NicoRepoType.All)
            {
                dict.Add("object[type]", type.GetDescription());

                dict.Add("type", type switch
                {
                    NicoRepoType.Video => "upload",
                    NicoRepoType.Program => "onair",
                    NicoRepoType.Image => "add",
                    NicoRepoType.ComicStory => "add",
                    NicoRepoType.Article => "add",
                    NicoRepoType.Game => "add",
                    _ => throw new NotSupportedException()
                });
            }

            if (target != NicoRepoDisplayTarget.All) { dict.AddEnumWithDescription("list", target); }

            dict.AddIfNotNull("untilId", untilId);

            var url = new StringBuilder(Urls.NicorepoTimelineApiUrl)
                .AppendQueryString(dict)
                .ToString();

            return _context.GetJsonAsAsync<NicoRepoEntriesResponse>(url, _options);
        }
    }

}
